// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Naninovel
{
    public static class ManagedTextUtils
    {
        public const string RecordIdLiteral = ": ";
        public const string RecordCommentLiteral = ";";
        public const BindingFlags ManagedFieldBindings = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// Parses provided managed text document text into <see cref="ManagedTextRecord"/> collection.
        /// </summary>
        /// <param name="text">Text to parse; see remarks for the expected format.</param>
        /// <param name="category">Category of the contained records (document name).</param>
        /// <remarks>
        /// Expected managed text document format:
        /// RecordKey: Record Value
        /// ; Comment line (optional)
        /// RecordKey: Record Value
        /// </remarks>
        public static HashSet<ManagedTextRecord> ParseDocument (string text, string category)
        {
            var records = new HashSet<ManagedTextRecord>();
            var lines = text.SplitByNewLine(StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWithFast(RecordCommentLiteral)) continue;
                var id = line.GetBefore(RecordIdLiteral);
                if (string.IsNullOrEmpty(id)) continue;
                var value = line.Substring(id.Length + RecordIdLiteral.Length);
                // When actual value is not set in the document, set ID instead to make it clear which field is missing.
                if (string.IsNullOrEmpty(value)) value = id;
                var record = new ManagedTextRecord(id, value, category);
                records.Add(record);
            }

            return records;
        }

        /// <summary>
        /// Applies provided record values to the static 
        /// <see cref="ManagedTextAttribute"/> <see cref="string"/> fields in the application domain.
        /// </summary>
        public static void ApplyRecords (IEnumerable<ManagedTextRecord> records)
        {
            var map = new Dictionary<string, ManagedTextRecord>(StringComparer.Ordinal);
            foreach (var r in records)
                map[r.Key.GetBeforeLast(".") ?? r.Key] = r;
            foreach (var type in Engine.Types)
            {
                if (!map.TryGetValue(type.FullName, out var record)) continue;
                var fieldName = record.Key.GetAfter(".") ?? record.Key;
                var fieldInfo = type.GetField(fieldName, ManagedFieldBindings);
                if (fieldInfo is null) Debug.LogWarning($"Failed to apply managed text record value to '{type.FullName}.{fieldName}' field.");
                else fieldInfo.SetValue(null, record.Value);
            }
        }
    }
}
