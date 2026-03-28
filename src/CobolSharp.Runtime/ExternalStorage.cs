// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;

namespace CobolSharp.Runtime;

/// <summary>
/// Shared storage for EXTERNAL data items (§13.18.22).
/// All programs in the run unit that describe the same external name
/// share the same byte array. Thread-safe via ConcurrentDictionary.
/// </summary>
public static class ExternalStorage
{
    private static readonly ConcurrentDictionary<string, byte[]> _storage = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get or create the shared byte array for the given external name.
    /// First caller allocates (space-filled per COBOL default); subsequent callers get the same array.
    /// Size must match across all programs (COBOL spec requirement, not enforced at runtime).
    /// </summary>
    public static byte[] GetOrCreate(string externalName, int size)
    {
        return _storage.GetOrAdd(externalName, _ =>
        {
            var array = new byte[size];
            Array.Fill(array, (byte)' ');
            return array;
        });
    }

    /// <summary>
    /// Reset all external storage. Used by test harnesses to ensure clean state.
    /// </summary>
    public static void Reset()
    {
        _storage.Clear();
    }
}
