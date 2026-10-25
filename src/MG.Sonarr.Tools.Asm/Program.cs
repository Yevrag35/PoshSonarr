// Developer tool: force RyuJIT FullOpts compilation of arbitrary PoshSonarr methods so DOTNET_JitDisasm
// can capture their disassembly. Normally driven by run-asmdump.ps1 beside it, which owns the env
// vars (DOTNET_TieredCompilation=0, DOTNET_JitDisasm=<pattern>) and the stdout redirect, and lands
// the dump under <repo>\.temp.
//
// Usage:
//   MG.Sonarr.Tools.Asm.exe <TypeName> [MethodName] [--assembly <name>] [--targs T1,T2] [--margs T1,T2] [--inlines <file>]
//
//   TypeName    simple ("Wildcard"), namespace-qualified ("MG.Sonarr.Next.Strings.Wildcard"),
//               or nested ("Outer+Inner"). Generic types may omit the arity suffix ("RentedBuffer"
//               finds RentedBuffer`1, "PooledBuffer+Enumerator" finds PooledBuffer`1+Enumerator)
//               but then require --targs to close the type.
//   MethodName  omitted or "*" prepares every declared method and constructor; ".ctor" prepares just
//               the constructors; a property name falls back to its get_/set_ accessors.
//   --assembly  restrict the type search to one assembly, named in full ("MG.Sonarr.Next.Shell") or
//               without its "MG.Sonarr." prefix ("Next.Shell").
//   --targs     comma-separated generic type arguments closing the declaring type (int, string, ...).
//   --margs     comma-separated generic type arguments closing a generic method.
//   --inlines   also write the JIT's inlining decisions for the dumped methods to <file> and print the
//               calls it did not inline to stderr. The trace filters on DOTNET_JitDisasm's pattern, so it
//               covers the same methods as the dump. See InlineTrace.
//
// RuntimeHelpers.PrepareMethod compiles the exact method handle standalone - immune to the failure
// mode where a small method inlines into this harness's <Main>$ and never JITs on its own (0-byte
// dump). With tiering disabled that single compilation is FullOpts. Async/iterator methods also get
// their compiler-generated state machine's MoveNext prepared - that is where the real code lives.
//
// PrepareMethod quirk (measured 2026-10-25 on .NET 10.0.13 via DOTNET_JitDisasmSummary): on a class,
// PrepareMethod can return success without JIT-compiling a NEWSLOT virtual - one that takes a vtable
// slot of its own rather than overriding an inherited one. That covers every interface implementation,
// implicit or explicit (C# emits an implicit one as `virtual final newslot`), and every `virtual`
// member that is not an override. Measured standalone, it skipped every one, in a sealed class or not,
// through BOTH overloads, and for every instantiation, shared or not, so the instantiation-handle
// fallback below can't rescue one: it sits in a catch the silent no-op never reaches. In this harness
// it compiled some of an abstract cmdlet base class's newslot virtuals, and compiled more of them once
// the types deriving from it had loaded, so whether it compiles one depends on runtime state, and the
// harness never counts on it. Every override measured compiled normally, abstract declaring class
// included, and so did every virtual on a value type, struct or ref struct. A class's newslot
// virtuals are therefore compiled by actually ENTERING them, in two tiers:
//   1. INVOKE - reflection-invoke on a RuntimeHelpers.GetUninitializedObject instance with default
//      arguments. The JIT compiles the whole body before the first instruction executes, so the
//      exception that follows (fields are null/default) cannot affect the dump.
//   2. THUNK  - for a byref-like return or parameter type (ReadOnlySpan<T>, Span<T>, ...), which
//      reflection rejects during binder setup, before ever entering the body. An emitted thunk passes
//      default arguments, calls the target and pops the result, so nothing needs boxing. See
//      TryThunkCompile.
// A newslot virtual neither tier can enter (an abstract declaring class, an interface's default
// method) falls back to PrepareMethod and is tagged "virtual?" because its listing is not guaranteed:
// the dump holds one only when the fallback happened to compile it.
// Disassembly goes to stdout; every line this harness prints goes to stderr so the dump stays clean.
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MG.Sonarr.Tools.Asm;

const string ASSEMBLY_PREFIX = "MG.Sonarr";
const BindingFlags DECLARED_ALL =
	BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
const BindingFlags CTOR_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

string? typeQuery = null;
string? methodQuery = null;
string? assemblyFilter = null;
string[] typeArgNames = [];
string[] methodArgNames = [];
string? inlinesPath = null;

try
{
	for (int i = 0; i < args.Length; i++)
	{
		switch (args[i])
		{
			case "--assembly":
				assemblyFilter = NextArg(args, ref i);
				break;
			case "--targs":
				typeArgNames = SplitList(NextArg(args, ref i));
				break;
			case "--margs":
				methodArgNames = SplitList(NextArg(args, ref i));
				break;
			case "--inlines":
				inlinesPath = NextArg(args, ref i);
				break;
			default:
				if (typeQuery is null) typeQuery = args[i];
				else if (methodQuery is null) methodQuery = args[i];
				else return Fail(2, $"unexpected argument '{args[i]}'");
				break;
		}
	}
}
catch (ArgumentException ex)
{
	// NextArg found an option at the end of the command line, with no value after it.
	return Fail(2, ex.Message);
}

if (typeQuery is null)
	return Fail(2, "usage: MG.Sonarr.Tools.Asm.exe <TypeName> [MethodName] [--assembly <name>] [--targs T1,T2] [--margs T1,T2] [--inlines <file>]");
if (methodQuery == "*")
	methodQuery = null;

// Load every MG.Sonarr assembly sitting next to the harness so the type search sees the full closure.
// The harness's own MG.Sonarr.Tools.Asm.dll matches that glob too, and it holds nothing but the
// harness itself, such as the Program class the compiler synthesizes for these top-level statements,
// so skip it rather than let a bare "Program" query resolve here.
string harnessDll = $"{Assembly.GetEntryAssembly()?.GetName().Name}.dll";
List<Assembly> assemblies = [];
foreach (string dllPath in Directory.EnumerateFiles(AppContext.BaseDirectory, $"{ASSEMBLY_PREFIX}*.dll").Order())
{
	if (Path.GetFileName(dllPath).Equals(harnessDll, StringComparison.OrdinalIgnoreCase))
		continue;

	try
	{
		assemblies.Add(Assembly.Load(AssemblyName.GetAssemblyName(dllPath)));
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine($"warn: could not load {Path.GetFileName(dllPath)}: {ex.Message}");
	}
}

if (assemblyFilter is not null)
{
	string loaded = string.Join(", ", assemblies.Select(asm => asm.GetName().Name));
	assemblies.RemoveAll(asm =>
	{
		string name = asm.GetName().Name ?? string.Empty;
		return !name.Equals(assemblyFilter, StringComparison.OrdinalIgnoreCase)
			&& !name.Equals($"{ASSEMBLY_PREFIX}.{assemblyFilter}", StringComparison.OrdinalIgnoreCase);
	});
	if (assemblies.Count == 0)
		return Fail(2, $"--assembly '{assemblyFilter}' matched no loaded {ASSEMBLY_PREFIX} assembly. Loaded: {loaded}");
}

Dictionary<string, Type> aliases = new(StringComparer.OrdinalIgnoreCase)
{
	["int"] = typeof(int), ["uint"] = typeof(uint), ["long"] = typeof(long), ["ulong"] = typeof(ulong),
	["short"] = typeof(short), ["ushort"] = typeof(ushort), ["byte"] = typeof(byte), ["sbyte"] = typeof(sbyte),
	["bool"] = typeof(bool), ["char"] = typeof(char), ["float"] = typeof(float), ["double"] = typeof(double),
	["decimal"] = typeof(decimal), ["string"] = typeof(string), ["object"] = typeof(object),
	["nint"] = typeof(nint), ["nuint"] = typeof(nuint),
};

// Resolve the target type.
List<Type> matches = FindTypes(typeQuery);
if (matches.Count == 0)
{
	Console.Error.WriteLine($"error: no type named '{typeQuery}' in {assemblies.Count} {ASSEMBLY_PREFIX} assemblies");
	List<string> near = [.. assemblies
		.SelectMany(TypesOf)
		.Where(t => IsNearMiss(t.Name, typeQuery))
		.Select(t => $"  {t.FullName} ({t.Assembly.GetName().Name})")
		.Distinct()
		.Order()];
	if (near.Count > 0)
	{
		Console.Error.WriteLine("close matches:");
		foreach (string s in near.Take(12))
			Console.Error.WriteLine(s);
	}
	return 2;
}
if (matches.Count > 1)
{
	// Non-generic companion + generic type (RentedBuffer / RentedBuffer`1): the --targs count says
	// which one is meant. Only report ambiguity when arity can't settle it.
	List<Type> byArity = [.. matches.Where(t => t.GetGenericArguments().Length == typeArgNames.Length)];
	if (byArity.Count == 1)
	{
		matches = byArity;
	}
	else
	{
		Console.Error.WriteLine($"error: '{typeQuery}' is ambiguous - qualify the name or pass --assembly:");
		foreach (Type t in matches)
			Console.Error.WriteLine($"  {t.FullName} ({t.Assembly.GetName().Name})");
		return 2;
	}
}

Type type = matches[0];
if (type.IsGenericTypeDefinition)
{
	Type[] genericParams = type.GetGenericArguments();
	if (typeArgNames.Length == 0)
		return Fail(2, $"{type.FullName} is generic over <{string.Join(", ", genericParams.Select(p => p.Name))}> - pass --targs");
	if (typeArgNames.Length != genericParams.Length)
		return Fail(2, $"{type.FullName} needs {genericParams.Length} type argument(s), got {typeArgNames.Length}");
	try
	{
		type = type.MakeGenericType([.. typeArgNames.Select(ResolveTypeArg)]);
	}
	catch (Exception ex)
	{
		return Fail(2, $"could not close {type.FullName}: {ex.Message}");
	}
}

Console.Error.WriteLine($"type: {type} ({type.Assembly.GetName().Name})");

// Select the methods to prepare.
List<MethodBase> targets = [];
if (methodQuery is null)
{
	targets.AddRange(type.GetMethods(DECLARED_ALL));
	targets.AddRange(type.GetConstructors(CTOR_FLAGS));
}
else if (methodQuery is ".ctor" or "ctor")
{
	targets.AddRange(type.GetConstructors(CTOR_FLAGS));
}
else
{
	targets.AddRange(type.GetMethods(DECLARED_ALL).Where(m => m.Name.Equals(methodQuery, StringComparison.Ordinal)));
	if (targets.Count == 0)
	{
		// Property convenience: fall back to the accessors.
		targets.AddRange(type.GetMethods(DECLARED_ALL)
			.Where(m => m.Name == "get_" + methodQuery || m.Name == "set_" + methodQuery));
	}
}

if (targets.Count == 0)
{
	Console.Error.WriteLine($"error: no method '{methodQuery}' declared on {type}. Declared methods:");
	foreach (string name in type.GetMethods(DECLARED_ALL).Select(m => m.Name).Distinct().Order())
		Console.Error.WriteLine($"  {name}");
	return 3;
}

// --inlines: bracket the prepare loop with the JIT inlining trace. Filtering on the dump's own
// DOTNET_JitDisasm pattern keeps the trace to exactly the methods the dump lists; run by hand without
// one, it falls back to the script's class-wide default.
InlineTrace? inlineTrace = null;
if (inlinesPath is not null)
{
	string filter = Environment.GetEnvironmentVariable("DOTNET_JitDisasm") is { Length: > 0 } disasm
		? disasm
		: $"*{TypePath(type).Replace("+", "*+", StringComparison.Ordinal)}*:*";
	inlineTrace = InlineTrace.Start(filter, inlinesPath, out string? traceFailure);
	if (inlineTrace is null)
		Console.Error.WriteLine($"warn: no inline trace - {traceFailure}");
}

int prepared = 0, skipped = 0;
foreach (MethodBase target in targets)
{
	MethodBase method = target;
	if (method.IsAbstract)
	{
		Console.Error.WriteLine($"skip [abstract]      {DescribeMethod(method)}");
		skipped++;
		continue;
	}
	if (method is MethodInfo { IsGenericMethodDefinition: true } open)
	{
		if (methodArgNames.Length == 0)
		{
			Console.Error.WriteLine($"skip [needs --margs] {DescribeMethod(method)}");
			skipped++;
			continue;
		}
		try
		{
			method = open.MakeGenericMethod([.. methodArgNames.Select(ResolveTypeArg)]);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"skip [margs]         {DescribeMethod(method)}: {ex.Message}");
			skipped++;
			continue;
		}
	}

	if (TryPrepare(method, out string how))
	{
		Console.Error.WriteLine($"ok   [{how}] {DescribeMethod(method)}");
		prepared++;
	}
	else
	{
		Console.Error.WriteLine($"skip [{how}] {DescribeMethod(method)}");
		skipped++;
		continue;
	}

	// Async/iterator methods: the real code lives in the state machine's MoveNext - prepare it too.
	if (method.GetCustomAttribute<StateMachineAttribute>() is { StateMachineType: { } smType })
	{
		if (smType.ContainsGenericParameters)
		{
			Console.Error.WriteLine($"note: state machine {smType.Name} is open generic - not prepared");
			continue;
		}
		MethodInfo? moveNext = smType.GetMethod("MoveNext", DECLARED_ALL);
		if (moveNext is not null && TryPrepare(moveNext, out string smHow))
		{
			Console.Error.WriteLine($"ok   [{smHow}] {DescribeMethod(moveNext)} (state machine of {method.Name})");
			prepared++;
		}
	}
}

Console.Error.WriteLine($"done: prepared {prepared}, skipped {skipped} (of {targets.Count} selected)");
inlineTrace?.Finish();
return prepared > 0 ? 0 : 4;

static string NextArg(string[] args, ref int i)
{
	if (i + 1 >= args.Length)
		throw new ArgumentException($"missing value after {args[i]}");
	return args[++i];
}

static string[] SplitList(string value) =>
	value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

static int Fail(int code, string message)
{
	Console.Error.WriteLine($"error: {message}");
	return code;
}

static IEnumerable<Type> TypesOf(Assembly asm)
{
	try
	{
		return asm.GetTypes();
	}
	catch (ReflectionTypeLoadException ex)
	{
		return ex.Types.OfType<Type>();
	}
}

// Matches exact simple name, arity-suffixed simple name (query "RentedBuffer" -> RentedBuffer`1),
// exact full name, or a "."/"+"-bounded tail of the full name, with or without its arity suffixes
// (query "PooledBuffer+Enumerator" -> PooledBuffer`1+Enumerator).
static bool TypeMatches(Type t, string query)
{
	if (query.IndexOfAny(['.', '+']) < 0)
		return SegmentMatches(t.Name, query);
	string? full = t.FullName;
	if (full is null)
		return false;
	foreach (string candidate in (ReadOnlySpan<string>)[full, StripArity(full)])
	{
		if (candidate.Equals(query, StringComparison.Ordinal))
			return true;
		if (candidate.Length > query.Length
			&& candidate.EndsWith(query, StringComparison.Ordinal)
			&& candidate[candidate.Length - query.Length - 1] is '.' or '+')
		{
			return true;
		}
	}
	return false;
}

// Suggestion heuristic for typos: substring either way, or a shared prefix of >= 6 chars
// (catches transpositions/omissions like "MetadataResovler" -> MetadataResolver).
static bool IsNearMiss(string name, string query)
{
	if (name.Contains(query, StringComparison.OrdinalIgnoreCase)
		|| query.Contains(name, StringComparison.OrdinalIgnoreCase))
	{
		return true;
	}
	int shared = 0;
	int max = Math.Min(name.Length, query.Length);
	while (shared < max && char.ToLowerInvariant(name[shared]) == char.ToLowerInvariant(query[shared]))
		shared++;
	return shared >= Math.Min(6, query.Length);
}

static bool SegmentMatches(string name, string query) =>
	name.Equals(query, StringComparison.Ordinal)
	|| (name.Length > query.Length && name[query.Length] == '`' && name.StartsWith(query, StringComparison.Ordinal));

// Drops every arity suffix, not just the last: a nested type's full name carries its outer type's too.
static string StripArity(string name) =>
	name.Contains('`') ? Regex.Replace(name, "`[0-9]+", string.Empty) : name;

// "PooledBuffer+Enumerator": the type's own name and the names of the types it is nested in, the form
// a type query takes, without arity suffixes or namespace.
static string TypePath(Type t) =>
	t is { IsNested: true, IsGenericParameter: false, DeclaringType: { } outer }
		? $"{TypePath(outer)}+{StripArity(t.Name)}"
		: StripArity(t.Name);

static string TypeShort(Type t) => t.IsConstructedGenericType
	? $"{TypePath(t)}<{string.Join(", ", t.GetGenericArguments().Select(TypeShort))}>"
	: t.Name;

static string DescribeMethod(MethodBase m)
{
	Type? dt = m.DeclaringType;
	string owner = dt is null
		? "?"
		: dt.IsConstructedGenericType ? $"{dt.Namespace}.{TypeShort(dt)}" : dt.FullName ?? dt.Name;
	return $"{owner}:{m.Name}({string.Join(", ", m.GetParameters().Select(p => TypeShort(p.ParameterType)))})";
}

// Force-compile at FullOpts. A class's newslot virtuals are entered, because PrepareMethod can
// silently skip them (see header). Everything else, overrides and every virtual on a value type
// included, takes PrepareMethod: fully closed non-shared methods the single-arg overload, methods
// whose closed form lands on shared (__Canon) generic code the explicit instantiation handles.
static bool TryPrepare(MethodBase method, out string how)
{
	bool unpreparableVirtual = false;
	if (method is MethodInfo { IsVirtual: true, IsAbstract: false } virtualMethod && IsSkippedByPrepare(virtualMethod))
	{
		if (TryInvokeCompile(virtualMethod, out how) || TryThunkCompile(virtualMethod, out how))
			return true;
		unpreparableVirtual = true;
	}
	try
	{
		RuntimeHelpers.PrepareMethod(method.MethodHandle);
		how = unpreparableVirtual ? "virtual?    " : "direct      ";
		return true;
	}
	catch (Exception first)
	{
		RuntimeTypeHandle[] instantiation = [.. GenericArgsOf(method).Select(t => t.TypeHandle)];
		if (instantiation.Length > 0)
		{
			try
			{
				RuntimeHelpers.PrepareMethod(method.MethodHandle, instantiation);
				how = "instantiated";
				return true;
			}
			catch (Exception second)
			{
				how = $"failed: {first.Message} / {second.Message}";
				return false;
			}
		}
		how = $"failed: {first.Message}";
		return false;
	}
}

// The virtuals PrepareMethod can return from without compiling: on a reference type, one with a
// vtable slot of its own (newslot) rather than an override of an inherited one.
static bool IsSkippedByPrepare(MethodInfo method) =>
	method.DeclaringType is { IsValueType: false }
	&& (method.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot;

// Compile a virtual method by entering it for real: reflection-invoke on an uninitialized instance
// with default arguments. Compilation completes before the body's first instruction runs, so the
// exception the null/default state provokes is expected and harmless. Returns false without side
// effects when reflection can't bind the call (byref-like signature, abstract owner) - the
// caller falls back to the thunk, then to PrepareMethod.
static bool TryInvokeCompile(MethodInfo method, out string how)
{
	how = string.Empty;
	Type? owner = method.DeclaringType;
	if (owner is null || owner.IsAbstract || owner.ContainsGenericParameters || method.ContainsGenericParameters)
		return false;
	// Ordinary out/ref parameters bind fine (the boxed args slot is written back), so only byref-LIKE
	// types are unbindable. Unwrap the ByRef first: `ref Span<int>` is a ByRef whose own IsByRefLike is
	// false - the ref struct only shows up on the element type.
	if (method.ReturnType.IsByRefLike
		|| method.GetParameters().Any(p => (p.ParameterType.GetElementType() ?? p.ParameterType).IsByRefLike))
	{
		return false;
	}
	try
	{
		object instance = RuntimeHelpers.GetUninitializedObject(owner);
		method.Invoke(instance, new object?[method.GetParameters().Length]);
		how = "invoked     ";
		return true;
	}
	catch (TargetInvocationException)
	{
		// The body threw (expected - the instance is uninitialized), so it was entered = compiled.
		how = "invoked     ";
		return true;
	}
	catch
	{
		// Never entered the method (binder refusal, uninstantiable type, ...) - not compiled.
		return false;
	}
}

// Last resort for a newslot virtual with a byref-like return or parameter type (ReadOnlySpan<T>,
// Span<T>, ...), which defeats both strategies above: PrepareMethod can no-op on it (both overloads -
// the instantiation-handle one included), and reflection Invoke rejects a ByRefLike return or
// parameter during binder setup, BEFORE entering the body, so nothing gets compiled either. Emit a
// thunk that pushes a default value for every argument, calls the target and pops the result,
// sidestepping the boxing reflection needs. The call goes through the method's function pointer on
// purpose: a direct `call` lets the JIT inline a small target into the thunk, which would once again
// leave no standalone listing to match.
static bool TryThunkCompile(MethodInfo method, out string how)
{
	how = string.Empty;
	Type? owner = method.DeclaringType;
	if (owner is null || owner.IsAbstract || owner.ContainsGenericParameters || method.ContainsGenericParameters)
		return false;
	// Shared generic code entered through a raw function pointer takes its instantiation from `this`.
	// A static method would instead need a hidden generic-context argument the thunk cannot supply.
	if (method.IsStatic)
		return false;

	Action<object> run;
	try
	{
		Type[] signature = [.. method.GetParameters().Select(p => p.ParameterType)];
		DynamicMethod thunk = new(
			$"asmdump_{method.Name}", typeof(void), [typeof(object)], owner.Module, skipVisibility: true);

		ILGenerator il = thunk.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Castclass, owner);
		foreach (Type parameter in signature)
		{
			// ref/out take the address of a fresh zeroed slot; everything else takes a default value,
			// which for a byref-like parameter is the zeroed slot itself (an empty span).
			Type slotType = parameter.IsByRef ? parameter.GetElementType()! : parameter;
			if (!parameter.IsByRef && !slotType.IsValueType && !slotType.IsGenericParameter)
			{
				il.Emit(OpCodes.Ldnull);
				continue;
			}

			LocalBuilder slot = il.DeclareLocal(slotType);
			il.Emit(OpCodes.Ldloca_S, slot);
			il.Emit(OpCodes.Initobj, slotType);
			if (!parameter.IsByRef)
			{
				il.Emit(OpCodes.Ldloc_S, slot);
			}
			else
			{
				il.Emit(OpCodes.Ldloca_S, slot);
			}
		}

		il.Emit(OpCodes.Ldc_I8, (long)method.MethodHandle.GetFunctionPointer());
		il.Emit(OpCodes.Conv_I);
		il.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, method.ReturnType, signature, null);
		if (method.ReturnType != typeof(void))
			il.Emit(OpCodes.Pop);
		il.Emit(OpCodes.Ret);

		run = thunk.CreateDelegate<Action<object>>();
	}
	catch
	{
		// Never got as far as a callable thunk - nothing was compiled.
		return false;
	}

	// Created before the call, so a type that can't be instantiated counts as never entered rather than
	// as a body that threw.
	object instance;
	try
	{
		instance = RuntimeHelpers.GetUninitializedObject(owner);
	}
	catch
	{
		return false;
	}

	try
	{
		run(instance);
	}
	catch (Exception ex) when (ex is InvalidProgramException or BadImageFormatException)
	{
		// The thunk itself was malformed, so the target was never entered.
		Console.Error.WriteLine($"note: thunk for {method.Name} was rejected: {ex.Message}");
		return false;
	}
	catch
	{
		// The body threw (expected - the instance is uninitialized), so it was entered = compiled.
	}

	how = "thunked     ";
	return true;
}

static IEnumerable<Type> GenericArgsOf(MethodBase m)
{
	if (m.DeclaringType is { IsConstructedGenericType: true } dt)
	{
		foreach (Type t in dt.GetGenericArguments())
			yield return t;
	}
	if (m is MethodInfo { IsGenericMethod: true } mi)
	{
		foreach (Type t in mi.GetGenericArguments())
			yield return t;
	}
}

List<Type> FindTypes(string query) =>
	[.. assemblies.SelectMany(TypesOf).Where(t => TypeMatches(t, query)).Distinct()];

Type ResolveTypeArg(string name)
{
	// C# nullable shorthand ("int?", "WildcardMatchType?"): the element type resolves by every rule
	// below, then a value type is wrapped in Nullable<T>. On a reference type, ? is only an annotation,
	// so "string?" is string, as it is at runtime.
	if (name.Length > 1 && name.EndsWith('?'))
	{
		Type element = ResolveTypeArg(name[..^1]);
		return element.IsValueType ? typeof(Nullable<>).MakeGenericType(element) : element;
	}
	if (aliases.TryGetValue(name, out Type? alias))
		return alias;
	if (Type.GetType(name, throwOnError: false) is { } bcl)
		return bcl;
	List<Type> found = FindTypes(name);
	return found.Count switch
	{
		1 => found[0],
		0 => throw new ArgumentException($"type argument '{name}' not found (aliases, BCL names, and {ASSEMBLY_PREFIX} assemblies searched)"),
		_ => throw new ArgumentException($"type argument '{name}' is ambiguous: {string.Join(", ", found.Select(t => t.FullName))}"),
	};
}
