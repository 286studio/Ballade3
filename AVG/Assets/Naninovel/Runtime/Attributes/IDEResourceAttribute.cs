// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate resources with a specific path prefix.
    /// Used by the IDE Metadata utility to provide autocomplete information to the IDE extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class IDEResourceAttribute : Attribute
    {
        public readonly string PathPrefix;
        public readonly int NamedIndex;

        /// <param name="pathPrefix">Resource path prefix to associate with the parameter.</param>
        /// <param name="namedIndex">When applied to named parameter, specify index of the associated value (0 is for name and 1 for value).</param>
        public IDEResourceAttribute (string pathPrefix, int namedIndex = -1)
        {
            PathPrefix = pathPrefix;
            NamedIndex = namedIndex;
        }
    }
}
