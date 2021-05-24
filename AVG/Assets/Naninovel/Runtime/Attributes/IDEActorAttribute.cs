// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate actor records with a specific path prefix.
    /// Used by the IDE Metadata utility to provide autocomplete information to the IDE extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class IDEActorAttribute : Attribute
    {
        public readonly string PathPrefix;
        public readonly int NamedIndex;

        /// <param name="pathPrefix">Actor path prefix to associate with the parameter. When *, will associate with all the available actors.</param>
        /// <param name="namedIndex">When applied to named parameter, specify index of the associated value (0 is for name and 1 for value).</param>
        public IDEActorAttribute (string pathPrefix = "*", int namedIndex = -1)
        {
            PathPrefix = pathPrefix;
            NamedIndex = namedIndex;
        }
    }
}
