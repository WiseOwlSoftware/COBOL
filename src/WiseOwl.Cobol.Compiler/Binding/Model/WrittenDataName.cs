// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// A <i>data-name-n</i> operand of a data division clause AS WRITTEN: <see cref="Name"/>, the data-name, and
/// <see cref="Qualifiers"/>, the words of its <c>OF</c>/<c>IN</c> phrases innermost first (§2023:8.4.2.2.2 Format 1,
/// §2023:8.4.2.2.3 SR4). It is the model's form of the parser's <c>WrittenQualifiedName</c>: a clause operand is resolved after
/// the whole data division is declared, and the model holds no parse tree (a published entry read back from a repository has
/// none), so the operand keeps its written words, and only <see cref="SymbolTable"/> resolves them
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, remainder slice 2"; kb/Work PB5932). It replaces the
/// <c>(name, qualifiers)</c> pair every such clause kept, so no text pair reaches a lookup.
/// <para>Made by the one capture of a data-name-n operand (<c>DataBinder.ClauseDataName</c>, <c>ClauseDataNameSubscripted</c>)
/// and by a published entry's decoding. An operand that capture REFUSED keeps its whole written text as <see cref="Name"/> with
/// no qualifier: a text no data-name can spell (it holds a space or a parenthesis), registered so its resolution stays silent
/// (kb/Work PB481).</para>
/// </summary>
public readonly record struct WrittenDataName(string Name, IReadOnlyList<string> Qualifiers)
{
    /// <summary>An unqualified data-name.</summary>
    public WrittenDataName(string name) : this(name, []) { }

    /// <summary>The operand as a diagnostic quotes it: <c>K OF G OF R</c>.</summary>
    public string Written => Qualifiers.Count == 0 ? Name : Name + " OF " + string.Join(" OF ", Qualifiers);

    /// <summary>The data-name and its qualifiers, as written.</summary>
    public override string ToString() => Written;
}
