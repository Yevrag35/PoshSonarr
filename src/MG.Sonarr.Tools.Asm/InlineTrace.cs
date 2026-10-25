// --inlines: records the JIT's inlining decisions for the methods a dump covers, so a call that stayed
// a call in the listing comes with the JIT's reason.
//
// The release JIT has no inline dump of its own - DOTNET_JitPrintInlinedMethods printed nothing when
// tried on 2026-10-25 with .NET 10.0.13 - but the runtime still raises every decision as an event on
// its own provider.
// Keyword JitTracing (0x1000) carries MethodJitInliningSucceeded and MethodJitInliningFailed[Ansi],
// the latter with the JIT's reason ("unprofitable inline", "target not direct", ...), and an
// in-process EventListener receives them over an EventPipe session. That is the verdict only: the
// score behind "unprofitable inline" never leaves the JIT, short of a Checked build's dump.
//
// The session delivers asynchronously, so two sentinel methods bracket the targets. The start
// sentinel's event proves the session is live before any target compiles; the end sentinel compiles
// last on the same thread, so its event arrives only after every target's. A start event that never
// comes almost always means tiering is on - tier-0 code never inlines, so no decision is ever made.
using System.Diagnostics.Tracing;
using System.IO.Enumeration;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MG.Sonarr.Tools.Asm;

internal sealed class InlineTrace : EventListener
{
	private const string RUNTIME_PROVIDER = "Microsoft-Windows-DotNETRuntime";
	private const long JIT_TRACING_KEYWORD = 0x1000;

	private static readonly TimeSpan s_startTimeout = TimeSpan.FromSeconds(10);
	private static readonly TimeSpan s_drainTimeout = TimeSpan.FromSeconds(30);
	private static readonly string s_sentinelOwner = typeof(InlineTrace).FullName!;

	// Field initializers run before the base constructor, which is what enables the provider, so these
	// exist by the time the first event can arrive. The fields the constructor body assigns may not.
	private readonly Lock _gate = new();
	private readonly List<Decision> _decisions = [];
	private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly string[]? _patterns;
	private readonly string _filter;
	private readonly string _path;

	private InlineTrace(string filter, string path)
	{
		_filter = filter;
		_path = path;
		_patterns = [.. filter.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(StripSignature)];
	}

	// Opens the trace and waits until the session delivers. Returns null - with the reason in `failure` -
	// when the start sentinel's event never arrives.
	public static InlineTrace? Start(string filter, string path, out string? failure)
	{
		InlineTrace trace = new(filter, path);
		PrepareSentinel(nameof(SentinelStart));
		if (trace._started.Task.Wait(s_startTimeout))
		{
			failure = null;
			return trace;
		}

		trace.Dispose();
		failure = $"no JIT inlining event arrived within {s_startTimeout.TotalSeconds:0} s - tier-0 code never inlines, so check DOTNET_TieredCompilation=0";
		return null;
	}

	// Waits for the targets' events to drain, writes every decision to the trace file, and prints each
	// call the JIT left as a call to stderr with its reason.
	public void Finish()
	{
		PrepareSentinel(nameof(SentinelEnd));
		bool drained = _drained.Task.Wait(s_drainTimeout);
		this.Dispose();

		Decision[] decisions;
		lock (_gate)
		{
			decisions = [.. _decisions];
		}

		StringBuilder file = new();
		file.AppendLine($"; JIT inlining decisions for methods matching '{_filter}'");
		file.AppendLine("; A FAIL line ends with the JIT's reason. [in X] names the inliner when it is not the method being compiled.");
		if (!drained)
		{
			file.AppendLine($"; INCOMPLETE: the end sentinel did not arrive within {s_drainTimeout.TotalSeconds:0} s, so later decisions may be missing.");
			Console.Error.WriteLine($"warn: inline trace incomplete - its end sentinel did not arrive within {s_drainTimeout.TotalSeconds:0} s");
		}

		// A root compiles start to finish on one thread, so its decisions arrive contiguously. GroupBy
		// keeps the roots in first-appearance order and each root's decisions in arrival order.
		foreach (IGrouping<string, Decision> root in decisions.GroupBy(d => d.Root))
		{
			int notInlined = root.Count(d => d.Reason is not null);
			string heading = $"{root.Key} - {root.Count() - notInlined} inlined, {notInlined} not inlined";
			file.AppendLine().AppendLine(heading);
			Console.Error.WriteLine($"inlines: {heading}");
			foreach (Decision decision in root)
			{
				string line = decision.Describe();
				file.Append("  ").AppendLine(line);
				if (decision.Reason is not null)
					Console.Error.WriteLine($"  {line}");
			}
		}

		if (decisions.Length == 0)
		{
			file.AppendLine().AppendLine("(no decisions - no matching method compiled a call the JIT considered)");
			Console.Error.WriteLine("inlines: no decisions recorded for the dumped methods");
		}

		File.WriteAllText(_path, file.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	protected override void OnEventSourceCreated(EventSource eventSource)
	{
		if (eventSource.Name == RUNTIME_PROVIDER)
			this.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)JIT_TRACING_KEYWORD);
	}

	protected override void OnEventWritten(EventWrittenEventArgs eventData)
	{
		string? name = eventData.EventName;
		bool failed = name?.StartsWith("MethodJitInliningFailed", StringComparison.Ordinal) == true;
		if (!failed && name != "MethodJitInliningSucceeded")
			return;

		string rootType = Payload(eventData, "MethodBeingCompiledNamespace");
		string rootMethod = Payload(eventData, "MethodBeingCompiledName");
		if (rootType == s_sentinelOwner)
		{
			if (rootMethod == nameof(SentinelStart))
				_started.TrySetResult();
			else if (rootMethod == nameof(SentinelEnd))
				_drained.TrySetResult();
			return;
		}

		// Null until the constructor body runs - nothing a dump covers compiles that early.
		if (_patterns is null || !Matches(_patterns, rootType, rootMethod))
			return;

		Decision decision = new(
			MethodName(eventData, "MethodBeingCompiled"),
			MethodName(eventData, "Inliner"),
			MethodName(eventData, "Inlinee"),
			failed ? Payload(eventData, "FailReason") : null);
		lock (_gate)
		{
			_decisions.Add(decision);
		}
	}

	// DOTNET_JitDisasm's syntax: space-separated globs, each "Class:Method" or a bare "Method", with '*'
	// as the wildcard. The class side matches the runtime's namespace-qualified type name, as it does
	// for the JIT. A "(signature)" tail is dropped, since the event's names carry no comparable one.
	private static bool Matches(string[] patterns, string type, string method)
	{
		foreach (string pattern in patterns)
		{
			string subject = pattern.Contains(':') ? $"{type}:{method}" : method;
			if (FileSystemName.MatchesSimpleExpression(pattern, subject, ignoreCase: false))
				return true;
		}
		return false;
	}

	private static string StripSignature(string pattern)
	{
		int paren = pattern.IndexOf('(');
		return paren < 0 ? pattern : pattern[..paren];
	}

	// "Namespace.Type:Method(parameters)" for one of the event's three methods, named by its payload
	// field prefix. The runtime formats signatures IL-style - "instance bool  (value class X.Point)" -
	// so only the parameter list is kept, without its class/value class keywords. An indirect call has
	// no target method, and the runtime sends the literal "<null>" for each of its names.
	private static string MethodName(EventWrittenEventArgs eventData, string prefix)
	{
		string name = Payload(eventData, prefix + "Name");
		if (name is "<null>" or "")
			return "(unknown target)";

		string signature = Payload(eventData, prefix + "NameSignature");
		int open = signature.IndexOf("  (", StringComparison.Ordinal);
		string parameters = open < 0
			? string.Empty
			: signature[(open + 2)..]
				.Replace("value class ", string.Empty, StringComparison.Ordinal)
				.Replace("class ", string.Empty, StringComparison.Ordinal);
		return $"{Payload(eventData, prefix + "Namespace")}:{name}{parameters}";
	}

	private static string Payload(EventWrittenEventArgs eventData, string field)
	{
		int index = eventData.PayloadNames?.IndexOf(field) ?? -1;
		return index >= 0 && eventData.Payload?[index] is { } value ? value.ToString() ?? string.Empty : string.Empty;
	}

	private static void PrepareSentinel(string name)
	{
		MethodInfo sentinel = typeof(InlineTrace).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
		RuntimeHelpers.PrepareMethod(sentinel.MethodHandle);
	}

	// Compiled only through PrepareSentinel, never called. Inlining Twice makes each one raise an event
	// that names it as the method being compiled.
	private static int SentinelStart(int value)
	{
		return Twice(value);
	}

	private static int SentinelEnd(int value)
	{
		return Twice(value);
	}

	private static int Twice(int value)
	{
		return value * 2;
	}

	// One decision. Root is the method being compiled and Inliner the method whose call site was judged -
	// they differ for a call inside an inlinee. Each name carries its parameter list, so overloads stay
	// apart. Reason is null for a call the JIT inlined.
	private readonly record struct Decision(string Root, string Inliner, string Inlinee, string? Reason)
	{
		public string Describe()
		{
			string where = this.Inliner == this.Root ? string.Empty : $" [in {this.Inliner}]";
			return this.Reason is null ? $"ok    {this.Inlinee}{where}" : $"FAIL  {this.Inlinee}{where} - {this.Reason}";
		}
	}
}
