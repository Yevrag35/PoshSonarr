namespace MG.Sonarr.Next.Shell.Checkers;

[DebuggerStepThrough]
public sealed class StringEqualityChecker : EqualityChecker<string>
{
	protected override bool Equals([DisallowNull] string x, [DisallowNull] string y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}
	protected override int GetHashCodeCore([DisallowNull] object obj)
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
	}
}