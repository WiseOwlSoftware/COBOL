// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>A method local the Roslyn backend declares ITSELF — a hoisted identification, a leaf or sort loop counter, a
/// recreation's element cell — named where the backend writes its declaration. A model node built over it (a position
/// over the backend's own loop counter) carries it like any <see cref="MethodLocal"/>; the binder never makes one.</summary>
internal sealed record CSharpLocal(string Name) : MethodLocal
{
    /// <summary>The integer position the backend's own local <paramref name="name"/> holds (a loop counter).</summary>
    public static PositionLocal Position(string name) => new(new CSharpLocal(name));
}

/// <summary>
/// ⛔ THE ONE SPELLING OF A METHOD LOCAL IN THE GENERATED C# (kb/Work PB4321; DESIGN-backend-abstraction.md §1.3 L8). The
/// neutral tree names a local by its identity (<see cref="MethodLocal"/>); this is where the Roslyn backend turns that
/// identity into a C# identifier, so the declaration and every read of one local spell it alike. Every name is in the
/// <c>__</c> space no COBOL word can reach (§8.3.2.1: a hyphen or underscore is never the first character of a
/// user-defined word), and each role has its own stem, so no two of the binder's locals in a unit share a name.
/// </summary>
internal static class LocalNames
{
    /// <summary>The C# identifier of <paramref name="local"/>.</summary>
    public static string Of(MethodLocal local) => local switch
    {
        CSharpLocal c => c.Name,
        StatementLocal s => Stem(s.Role) + s.Ordinal.ToString(CultureInfo.InvariantCulture) + Suffix(s.Role),
        _ => throw new System.InvalidOperationException($"LocalNames has no arm for {local.GetType().Name}"),
    };

    private static string Stem(StatementLocalRole role) => role switch
    {
        StatementLocalRole.InitializeOccurrence => "__ini",
        StatementLocalRole.ElementMoveOccurrence => "__vlm",
        StatementLocalRole.CorrespondingSendingAnchor or StatementLocalRole.CorrespondingReceivingAnchor => "__corr",
        StatementLocalRole.TableAllIndices => "__all",
        StatementLocalRole.HeldTableAllIndices => "__hold",
        StatementLocalRole.ReportVaryingCounter => "__kv",
        _ => throw new System.InvalidOperationException($"LocalNames has no stem for {role}"),
    };

    /// <summary>The two anchors of one CORRESPONDING statement share its ordinal and differ by their suffix.</summary>
    private static string Suffix(StatementLocalRole role) => role switch
    {
        StatementLocalRole.CorrespondingSendingAnchor => "s",
        StatementLocalRole.CorrespondingReceivingAnchor => "t",
        _ => "",
    };
}
