// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The static facade over the run unit's <see cref="ModuleStack"/> (the emitted surface for FUNCTION
/// MODULE-NAME; kept name-stable pre-G8). Every member forwards to <c>RunUnit.Current.Modules</c>.
/// </summary>
public static class CobolModule
{
    /// <inheritdoc cref="ModuleStack.PushMain"/>
    public static void PushMain(string name) => RunUnit.Current.Modules.PushMain(name);

    /// <inheritdoc cref="ModuleStack.Push"/>
    public static void Push(string name, string outermost, bool isNested)
        => RunUnit.Current.Modules.Push(name, outermost, isNested);

    /// <inheritdoc cref="ModuleStack.Pop"/>
    public static void Pop() => RunUnit.Current.Modules.Pop();

    /// <inheritdoc cref="ModuleStack.Reset"/>
    public static void Reset() => RunUnit.Current.Modules.Reset();

    /// <inheritdoc cref="ModuleStack.Name"/>
    public static string Name(int kind) => RunUnit.Current.Modules.Name(kind);
}
