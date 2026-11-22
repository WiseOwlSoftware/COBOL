// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>⛔ ONE INDEX-NAME DECLARATION — the identity an index-name reference resolves to (kb/Work PB919).
/// §2023:14.9.39.4 GR1 associates an index-name with the table whose OCCURS clause's INDEXED BY phrase specifies it, and
/// §2023:8.4.2.2.3 SR6 qualifies it through that table: "The qualification of an index-name may include the name of the
/// table with which the index-name is associated, as well as any name by which that table may be qualified." So
/// the declaration is the pair (name, table), and it owns its OWN cell.
/// <para>⛔ THE SHAPE THIS REPLACED WAS KEYED BY SPELLING. The unit held <c>name → cell</c>, de-duplicated on
/// insert, so two tables that each declared <c>INDEXED BY IX</c> silently SHARED one C# cell, a written
/// <c>IX</c> compiled with no §2023:8.4.2.2.3 SR1 ambiguity diagnostic, and the SR6 form that disambiguates it
/// (<c>IX OF EA</c>) was "not defined". A TYPE referenced twice cloned one index-name onto two tables and was
/// refused outright (the former COBOL1531 stage) for the same reason.</para></summary>
/// <param name="Name">The index-name as declared (compared case-insensitively, §2023:8.1.3.2 GR3a).</param>
/// <param name="Table">The OCCURS entry whose INDEXED BY phrase declares it — the first qualifier SR6 admits.</param>
/// <param name="Cell">The C# <c>long</c> member holding the occurrence number (DESIGN §3.5).</param>
public sealed record IndexDeclaration(string Name, DataItem Table, string Cell)
{
    /// <summary>Every INDEX-NAME declaration under <paramref name="root"/> (its subtree's <c>INDEXED BY</c> declarations, each
    /// with its own cell — kb/Work PB919). The ONE subtree walk every cell router, bridge and emitter reads; a model query, so
    /// a backend asks it without the binder (kb/Work PB2361).</summary>
    public static IEnumerable<IndexDeclaration> Under(DataItem root)
    {
        foreach (var d in root.Indexes) yield return d;
        foreach (var c in root.Children) foreach (var d in Under(c)) yield return d;
    }
}
