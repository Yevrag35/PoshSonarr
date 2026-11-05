namespace MG.Sonarr.Next.Shell.Context;

/// <summary>
/// Represents a cmdlet type that keeps a static flag for whether its scope check has run.
/// </summary>
/// <remarks>
/// <para>
/// The interface constrains the cmdlet types that
/// <see cref="CmdletContextExtensions.CreateScope{T}(T)"/> and the two <c>UnsetContext</c> overloads of
/// <see cref="CmdletContextExtensions"/> accept. Each implementing base class keeps the flag in a static field of its
/// own, so every cmdlet derived from that class shares it, across runspaces too.
/// </para>
/// <para>
/// TODO: Clarify what the flag records. No code calls <see cref="HasChecked"/> or <see cref="SetChecked(bool)"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The implementing cmdlet type.</typeparam>
internal interface IScopeCmdlet<T>
{
	/// <summary>
	/// When implemented in a derived class, gets a value that indicates whether the cmdlet type's scope check has run.
	/// </summary>
	/// <returns><see langword="true"/> if the check has run; otherwise, <see langword="false"/>.</returns>
	static abstract bool HasChecked();
	/// <summary>
	/// When implemented in a derived class, sets the flag that records whether the cmdlet type's scope check has run.
	/// </summary>
	/// <param name="toggle"><see langword="true"/> to record that the check has run; otherwise,
	/// <see langword="false"/>.</param>
	static abstract void SetChecked(bool toggle);
}

/// <summary>
/// Represents a cmdlet type that can tell whether one of its instances is running as a command.
/// </summary>
/// <remarks>
/// <para>
/// The interface constrains the cmdlet types that <see cref="CmdletContextExtensions.CreateScope{T}(T)"/> and
/// <see cref="CmdletContextExtensions.SetContext{T}(T, System.Reflection.Assembly, Action{IServiceCollection})"/> accept.
/// </para>
/// <para>
/// TODO: Clarify the intended check. No code calls <see cref="IsRunningCommand(T)"/>; the call in
/// <see cref="CmdletContextExtensions.CreateScope{T}(T)"/> that would refuse a cmdlet that isn't running is commented
/// out.
/// </para>
/// </remarks>
/// <typeparam name="T">The implementing cmdlet type.</typeparam>
internal interface IIsRunning<T> where T : PSCmdlet
{
	/// <summary>
	/// When overridden in a derived class, determines whether the specified cmdlet is running as a command, rather than
	/// being constructed or called directly.
	/// </summary>
	/// <remarks>
	/// The default implementation always returns <see langword="true"/>, and the disconnecting cmdlet's base class
	/// returns <see langword="false"/>.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet to check.</param>
	/// <returns><see langword="true"/> if <paramref name="cmdlet"/> is running as a command; otherwise,
	/// <see langword="false"/>.</returns>
	static virtual bool IsRunningCommand(T cmdlet)
	{
		return true;
		//return cmdlet.SessionState is not null && cmdlet.InvokeProvider is not null;
	}
}
