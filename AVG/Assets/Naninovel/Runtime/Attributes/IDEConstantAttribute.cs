// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate specified constant value range.
    /// Used by the IDE Metadata utility to provide autocomplete information to the IDE extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class IDEConstantAttribute : Attribute
    {
        public const string Transition = nameof(Transition);
        public const string Easing = nameof(Easing);
        public const string Expression = nameof(Expression);
        
        public readonly string ConstantName;
        public readonly int NamedIndex;

        /// <param name="constantName">Name of the constant to associate.</param>
        /// <param name="namedIndex">When applied to named parameter, specify index of the associated value (0 is for name and 1 for value).</param>
        public IDEConstantAttribute (string constantName, int namedIndex = -1)
        {
            ConstantName = constantName;
            NamedIndex = namedIndex;
        }
    }
}
