// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public struct SpawnedObjectState : IEquatable<SpawnedObjectState>
    {
        public string Path => path;
        public string[] Parameters => parameters?.Select(s => s?.Value).ToArray();

        [SerializeField] private string path;
        [SerializeField] private NullableString[] parameters;

        public SpawnedObjectState (string path, string[] parameters)
        {
            this.path = path;
            this.parameters = parameters?.Select(s => (NullableString)s).ToArray();
        }

        public bool Equals (SpawnedObjectState other) => path == other.path;
        public override bool Equals (object obj) => obj is SpawnedObjectState other && Equals(other);
        public override int GetHashCode () => Path != null ? Path.GetHashCode() : 0;
        public static bool operator == (SpawnedObjectState left, SpawnedObjectState right) => left.Equals(right);
        public static bool operator != (SpawnedObjectState left, SpawnedObjectState right) => !left.Equals(right);
    }
}
