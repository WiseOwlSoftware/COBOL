// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>What <see cref="SymbolTable.Identify"/> found: the symbol a written name denotes, of the class the position
/// admitted. Only the table makes one (<c>NameTableOwnershipDriftTests</c> refuses one made anywhere else), so a symbol
/// reached from a written name has come through the one lookup (docs/rearchitecture/DESIGN-listing.md section 6.7.1).
/// Listing step 7b adds the occurrence it records.</summary>
internal sealed record IdentifiedName
{
    internal IdentifiedName(NameClass @class, object symbol)
    {
        Class = @class;
        Symbol = symbol;
    }

    /// <summary>The class of the symbol.</summary>
    public NameClass Class { get; }

    /// <summary>The symbol: a <see cref="Model.CommunicationDescription"/> for a cd-name, a <see cref="Model.FileModel"/> for a
    /// file-name, a <see cref="ParagraphDefinition"/> or a <see cref="Procedure.SectionInfo"/> for a procedure-name, ... (by
    /// <see cref="Class"/>). A procedure-name that several procedures carry identifies no one symbol: <see cref="Symbol"/> is then
    /// the <see cref="ProcedureCandidates"/>, which the reference reports (kb/Work PB466) and which records no occurrence.</summary>
    public object Symbol { get; }
}

public sealed partial class SymbolTable
{
    /// <summary>
    /// THE lookup from a written name to a symbol (docs/rearchitecture/DESIGN-listing.md section 6.7.1): <paramref name="written"/>
    /// is the name as the parser read it, <paramref name="wanted"/> the class its position admits. Null when the name denotes
    /// no symbol of that class here. <paramref name="report"/> says whether this is the reference itself (true) or a probe
    /// that classifies a word before the reference is resolved (false); listing step 7b records an occurrence for a reference
    /// and none for a probe, so a probe followed by its resolution is one occurrence. <paramref name="reach"/> narrows a
    /// file-name position that names only this element's own entries (<see cref="NameReach.Own"/>). <paramref name="site"/> is where a
    /// procedure-name is written (its method and its containing section), which scopes its resolution; every procedure-name
    /// reference states it, and no other class reads it.
    /// <para>A name is ONE word, and a COBOL word is one token (<c>cobolWord</c> in CobolWords.g4), so the key is that token's
    /// text. A context of several tokens is no name and denotes nothing: its <c>GetText()</c> glues the tokens, so
    /// <c>A - B</c> would read as the file-name <c>A-B</c> and <c>F OF G</c> as <c>FOFG</c> (kb/Work PB2040, PB5932 review
    /// round 1). The rule lives here, once, so no caller can hand the table a glued text. A REFERENCE RULE's context is the one
    /// exception, and the table reads its words from its children: a procedure-name (<c>procedureName</c>, or a USE FOR DEBUGGING
    /// operand) is a head word and its qualifier (<see cref="ProcedureWords"/>).</para>
    /// </summary>
    internal IdentifiedName? Identify(ParserRuleContext written, NameClass wanted, bool report, NameReach reach = NameReach.Visible,
        ProcedureSite? site = null)
    {
        _ = report;   // the occurrence it gates is listing step 7b's
        object? symbol = wanted switch
        {
            NameClass.Procedure => ProcedureNamed(written,
                site ?? throw new ArgumentNullException(nameof(site), "a procedure-name resolves from where it is written")),
            NameClass.CommunicationDescription => WordOf(written) is { } cd ? _cds.GetValueOrDefault(cd) : null,
            NameClass.File => WordOf(written) is { } file ? FileNamed(file, reach) : null,
            _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not yet a class SymbolTable.Identify resolves"),
        };
        return symbol is null ? null : new IdentifiedName(wanted, symbol);
    }

    /// <summary>The name key of a written word: the table's one maker of a <see cref="CobolName"/>.</summary>
    private static CobolName Key(string word) => new(word);

    /// <summary>The key of a context that is exactly one token, or null for a context of several (or none): only a single
    /// word is a name.</summary>
    private static CobolName? WordOf(ParserRuleContext written) =>
        written.Start is { } first && written.Stop is { } last && first.TokenIndex == last.TokenIndex ? Key(first.Text) : null;
}
