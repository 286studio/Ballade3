// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate appearance records. Command should contains a parameter with <see cref="IDEActorAttribute"/>.
    /// Used by the IDE Metadata utility to provide autocomplete information to the IDE extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class IDEAppearanceAttribute : Attribute
    {
        public readonly int NamedIndex;

        /// <param name="namedIndex">When applied to named parameter, specify index of the associated value (0 is for name and 1 for value).</param>
        public IDEAppearanceAttribute (int namedIndex = -1)
        {
            NamedIndex = namedIndex;
        }
    }
}
