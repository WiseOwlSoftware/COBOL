// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Runtime registry for cross-program GLOBAL USE AFTER STANDARD ERROR/EXCEPTION declaratives
/// (ISO §14.9.49.4 GR4 / §8.4.6.2.2). A containing program that declares a USE GLOBAL declarative
/// registers a handler delegate here; when an I/O exception arises during a statement in a CONTAINED
/// program that has no applicable USE declarative of its own, the contained program dispatches the
/// containing program's GLOBAL handler through this registry.
///
/// All programs of one compilation share a single .NET assembly, so the registered delegate refers to
/// a static handler method on the containing program's type; invoking it runs the containing program's
/// declarative section against the containing program's (live, on-stack) ProgramState. The associated
/// file is a GLOBAL file connector shared by name, so <see cref="FileRuntime"/>'s name-keyed I/O status
/// is already visible to both programs.
///
/// Scope encoding mirrors FileIoLowerer/FileRuntime: -1 = file-name-scoped (USE … ON file-name); the
/// file name then selects the handler. 0/1/2/3 = open-mode-scoped (INPUT/OUTPUT/I-O/EXTEND).
/// </summary>
public static class GlobalUseDeclarativeRegistry
{
    private static readonly object _lock = new();

    // Open-mode-scoped GLOBAL handlers, keyed by scope (0/1/2/3). A program declares at most one USE
    // declarative per open mode (the most recent registration wins, matching the per-program model in
    // SemanticModel.RegisterUseDeclarativeForMode).
    private static readonly Dictionary<int, Action> _byMode = new();

    // File-name-scoped GLOBAL handlers (USE … ON file-name), keyed by GLOBAL file name.
    private static readonly Dictionary<string, Action> _byFileName =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a containing program's open-mode-scoped GLOBAL USE declarative handler.</summary>
    public static void RegisterForMode(int scope, Action handler)
    {
        lock (_lock) { _byMode[scope] = handler; }
    }

    /// <summary>Register a containing program's file-name-scoped GLOBAL USE declarative handler.</summary>
    public static void RegisterForFile(string fileName, Action handler)
    {
        lock (_lock) { _byFileName[fileName] = handler; }
    }

    /// <summary>
    /// Dispatch a containing program's GLOBAL USE declarative after an I/O operation in a contained
    /// program, when that operation raised an exception condition the contained statement did not service
    /// (the same gate <see cref="FileRuntime.ShouldRunUseDeclarative"/> applies to a local declarative).
    /// A file-name-scoped GLOBAL handler for <paramref name="fileName"/> takes precedence; otherwise the
    /// open-mode-scoped handler for the file's actual open mode is invoked. Returns true if a handler ran.
    /// </summary>
    public static bool Dispatch(string fileName, int scope, bool excludeAtEnd, bool excludeInvalidKey)
    {
        if (!FileRuntime.ShouldRunUseDeclarative(fileName, scope, excludeAtEnd, excludeInvalidKey))
            return false;

        Action? handler;
        lock (_lock)
        {
            if (scope < 0)
                _byFileName.TryGetValue(fileName, out handler);
            else
                _byMode.TryGetValue(scope, out handler);
        }
        if (handler == null) return false;
        handler();
        return true;
    }

    /// <summary>Clear all registered handlers (called at run-unit start).</summary>
    public static void Clear()
    {
        lock (_lock) { _byMode.Clear(); _byFileName.Clear(); }
    }
}
