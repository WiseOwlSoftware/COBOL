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

    /// <summary>
    /// Programs that were canceled and must be returned to their initial state on the next CALL
    /// (ISO §14.9.5 GR3). The flag is consumed (cleared) by the program's Entry method when it
    /// next runs, via <see cref="ConsumeReinitFlag"/>.
    /// </summary>
    private static readonly HashSet<string> _needsReinit =
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

    /// <summary>
    /// CANCEL a program (ISO §14.9.5): it ceases its logical relationship to the run unit, and the
    /// next CALL must find it in its initial state. Canceling a program that was never called or is
    /// already canceled has no effect (GR7). The actual re-initialization happens in the program's
    /// Entry method, which calls <see cref="ConsumeReinitFlag"/> on its next activation.
    /// </summary>
    public static void Cancel(string programId)
    {
        _registry.Remove(programId);
        _needsReinit.Add(programId);
    }

    /// <summary>
    /// Called by a program's Entry method: returns true (and clears the flag) when the program must
    /// be returned to its initial state because it was canceled since its last activation
    /// (ISO §14.9.5 GR3). Returns false for a program that has not been canceled, so a normal
    /// CALL leaves WORKING-STORAGE in its last-used state (§14.6.2.3.2 — static items persist).
    /// </summary>
    public static bool ConsumeReinitFlag(string programId)
    {
        return _needsReinit.Remove(programId);
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
