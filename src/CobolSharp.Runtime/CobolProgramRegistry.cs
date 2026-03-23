// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;

namespace CobolSharp.Runtime;

/// <summary>
/// Delegate signature for COBOL program entry points.
/// Returns 0 for normal completion, non-zero for exceptional conditions.
/// </summary>
public delegate int CobolProgramEntry(CobolDataPointer[] args);

/// <summary>
/// Runtime registry for inter-program CALL resolution.
/// Maps COBOL program names to their Entry method delegates.
/// Programs register themselves at startup; dynamic CALL resolves at runtime.
/// </summary>
public static class CobolProgramRegistry
{
    private static readonly Dictionary<string, CobolProgramEntry> _registry =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a program's entry point by PROGRAM-ID.</summary>
    public static void Register(string programId, CobolProgramEntry entry)
    {
        _registry[programId] = entry;
    }

    /// <summary>
    /// Resolve a program name to its entry point.
    /// First checks the registry, then attempts to load from assemblies
    /// in the application directory.
    /// </summary>
    public static CobolProgramEntry? Resolve(string programId)
    {
        if (_registry.TryGetValue(programId, out var entry))
            return entry;

        // Auto-discovery: search loaded assemblies for a type matching the program name
        entry = DiscoverProgram(programId);
        if (entry != null)
            _registry[programId] = entry;

        return entry;
    }

    /// <summary>Remove a program from the registry (CANCEL statement).</summary>
    public static void Cancel(string programId)
    {
        _registry.Remove(programId);
    }

    /// <summary>
    /// Search loaded assemblies and the application directory for a type
    /// with a static Entry(CobolDataPointer[]) method matching the program name.
    /// </summary>
    private static CobolProgramEntry? DiscoverProgram(string programId)
    {
        // Search already-loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var entry = FindEntryInAssembly(asm, programId);
            if (entry != null) return entry;
        }

        // Try loading from application directory
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string dllPath = Path.Combine(appDir, programId + ".dll");
        if (File.Exists(dllPath))
        {
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                return FindEntryInAssembly(asm, programId);
            }
            catch (Exception)
            {
                // Assembly load failed — return null (triggers ON EXCEPTION)
            }
        }

        return null;
    }

    private static CobolProgramEntry? FindEntryInAssembly(Assembly asm, string programId)
    {
        // Look for a type whose name matches the program-id (case-insensitive)
        foreach (var type in asm.GetExportedTypes())
        {
            if (string.Equals(type.Name, programId, StringComparison.OrdinalIgnoreCase))
            {
                var method = type.GetMethod("Entry",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(CobolDataPointer[])],
                    null);
                if (method != null && method.ReturnType == typeof(int))
                {
                    return (CobolProgramEntry)Delegate.CreateDelegate(
                        typeof(CobolProgramEntry), method);
                }
            }
        }
        return null;
    }
}
