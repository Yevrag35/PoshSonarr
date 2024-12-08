using System;

namespace MG.Sonarr.Next.Reflection
{
    /// <summary>
    /// A delegate that acts on a <see cref="Referencer"/>.
    /// </summary>
    /// <param name="referencer">The referencer used to load assemblies at application startup.</param>
    public delegate void ActOnReferencer(in Referencer referencer);

    /// <summary>
    /// A struct used at application startup to force loading of assemblies by referencing a type
    /// from each assembly.
    /// </summary>
    public readonly ref struct Referencer
    {
        /// <summary>
        /// References the specified type. No action is taken.
        /// </summary>
        /// <typeparam name="T">The type within an assembly to load.</typeparam>
        /// <returns>
        ///     The same <see cref="Referencer"/> for chaining.
        /// </returns>
        public readonly Referencer Reference<T>()
        {
            return this;
        }
        /// <summary>
        /// References the specified type. No action is taken.
        /// </summary>
        /// <param name="type">The type within an assembly to load.</param>
        /// <returns>
        ///     The same <see cref="Referencer"/> for chaining.
        /// </returns>
        public readonly Referencer Reference(Type type)
        {
            return this;
        }

        /// <summary>
        /// Force loads referenced assemblies by executing the specified action.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void LoadAll(ActOnReferencer action)
        {
            Referencer referencer = default;
            action(in referencer);
        }
    }
}
