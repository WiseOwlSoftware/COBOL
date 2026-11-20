// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>What <see cref="SymbolTable.Identify"/> found: the symbol a written name denotes, of the class the position
/// admitted. Only the table makes one (<c>NameTableOwnershipDriftTests</c> refuses one made anywhere else), so a symbol
/// reached from a written name has come through a recording point: <see cref="SymbolTable.Identify(ParserRuleContext, NameClass,
/// bool, NameReach, ProcedureSite?, ReportSite)"/>, or, for an object-class-name or interface-name, the compilation group tier's
/// <see cref="SymbolTable.ClassNames.Lookup"/>, whose answer is a <see cref="Compiler.Oo.OoNameResult"/>
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1).
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
    /// file-name, a <see cref="ConstantDef"/> for a constant-name, a <see cref="ParagraphDefinition"/> or a <see cref="Procedure.SectionInfo"/> for a procedure-name, the definition a
    /// configuration section declares for a configuration-section name (an <see cref="AlphabetDef"/>, a <see cref="SwitchStatusName"/>,
    /// an <see cref="Editions.ImplementorName"/> row for a mnemonic-name, ...), a <see cref="ReportModel"/> for a report-name, a <see cref="ProgramPrototype"/> for a program-prototype-name, a
    /// <see cref="FunctionPrototypeReference"/> for a function-prototype-name, a
    /// <see cref="ReportVaryingModel"/> for a VARYING counter, the <see cref="ReportCandidates{T}"/> of a report group, a sum counter or a
    /// report entry, ... (by
    /// <see cref="Class"/>). A procedure-name that several procedures carry identifies no one symbol: <see cref="Symbol"/> is then
    /// the <see cref="ProcedureCandidates"/>, which the reference reports (kb/Work PB466) and which records no occurrence.</summary>
    public object Symbol { get; }
}

/// <summary>Where a name is written when no parse rule holds it alone: <see cref="Token"/>, or the word of
/// <see cref="Length"/> characters at <see cref="Start"/> inside it (a constant-name in a PICTURE character-string's repetition,
/// §2023:13.10.3 SR2). <see cref="SymbolTable.Identify(WrittenWord, NameClass, bool, NameReach)"/> keys its <see cref="Text"/>,
/// and listing step 7b records the occurrence at the token's position plus <see cref="Start"/>.</summary>
internal readonly record struct WrittenWord(IToken Token, int Start, int Length)
{
    /// <summary>A whole token.</summary>
    public static WrittenWord Of(IToken token) => new(token, 0, token.Text.Length);

    /// <summary>The word as written.</summary>
    public string Text => Token.Text.Substring(Start, Length);
}

/// <summary>A qualified name written where no parse rule holds it with its qualifiers: <see cref="Head"/>, the name, and
/// <see cref="Qualifiers"/>, the words of its <c>OF</c>/<c>IN</c> phrases innermost first (§2023:8.4.2.2.3 SR4), each a token of a
/// subscript's token list or of a <c>dataReference</c>'s qualifications. <see cref="SymbolTable.IdentifyIndexName"/> keys their
/// texts, and listing step 7b records the occurrence at <see cref="Head"/>. Every word is read off its token once, when the name is
/// made (a token's text is a new string at each read), and an unqualified name allocates no list (section 9's budget: every
/// subscript name is read so).</summary>
internal readonly record struct WrittenQualifiedName
{
    public WrittenQualifiedName(IToken head, IReadOnlyList<IToken> qualifiers)
    {
        Head = head;
        Name = head.Text;
        Qualifiers = qualifiers;
        QualifierWords = qualifiers.Count == 0 ? [] : [.. qualifiers.Select(static q => q.Text)];
    }

    /// <summary>The name's token.</summary>
    public IToken Head { get; }

    /// <summary>The qualifiers' tokens, innermost first.</summary>
    public IReadOnlyList<IToken> Qualifiers { get; }

    /// <summary>The qualifiers' words as written, innermost first: made once with the name, so every reader shares one array.</summary>
    public IReadOnlyList<string> QualifierWords { get; }

    /// <summary>A <c>dataReference</c>'s name and the words of its qualifications (its subscripts and reference modifiers are not
    /// read), or null when the reference writes no name: a LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER register, whose
    /// <c>cobolWord</c> is its file-name or report-name QUALIFIER (§2023:8.4.2.2.2 Formats 7 and 8), and a recovery node with no word.
    /// The name is never the reference's glued text (kb/Work PB2040).</summary>
    public static WrittenQualifiedName? Of(Frontend.Generated.CobolParserCore.DataReferenceContext dref)
    {
        if (dref.LINAGE_COUNTER() is not null || dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null
            || dref.cobolWord() is not { } word)
            return null;
        List<IToken>? qualifiers = null;
        foreach (var suffix in dref.dataReferenceSuffix())
            if (suffix.qualification() is { } q) (qualifiers ??= []).Add(q.cobolWord().Start);
        return new(word.Start, qualifiers ?? (IReadOnlyList<IToken>)[]);
    }

    /// <summary>The name at <paramref name="at"/> of a token list and the qualifiers written after it (<c>OF</c> or <c>IN</c>, then a
    /// word <paramref name="isWord"/> admits), and the index of the last token read.</summary>
    public static WrittenQualifiedName Of(IReadOnlyList<IToken> tokens, int at, Func<IToken, bool> isWord, out int last)
    {
        List<IToken>? qualifiers = null;
        last = at;
        while (last + 2 < tokens.Count && tokens[last + 1].Type is Frontend.Generated.CobolParserCore.OF or Frontend.Generated.CobolParserCore.IN
               && isWord(tokens[last + 2]))
        {
            (qualifiers ??= []).Add(tokens[last + 2]);
            last += 2;
        }
        return new(tokens[at], qualifiers ?? (IReadOnlyList<IToken>)[]);
    }

    /// <summary>The name as written.</summary>
    public string Name { get; }
}

public sealed partial class SymbolTable
{
    /// <summary>
    /// THE lookup from a written name to a symbol of an element's tables (docs/rearchitecture/DESIGN-listing.md section 6.7.1; an
    /// object-class-name or interface-name is the compilation group's, identified by its twin <see cref="ClassNames.Lookup"/>):
    /// <paramref name="written"/>
    /// is the name as the parser read it, <paramref name="wanted"/> the class its position admits. Null when the name denotes
    /// no symbol of that class here. <paramref name="report"/> says whether this is the reference itself (true) or a probe
    /// that classifies a word before the reference is resolved (false); listing step 7b records an occurrence for a reference
    /// and none for a probe, so a probe followed by its resolution is one occurrence. <paramref name="reach"/> narrows a
    /// file-name position that names only this element's own entries (<see cref="NameReach.Own"/>). <paramref name="site"/> is where a
    /// procedure-name is written (its method and its containing section), which scopes its resolution; every procedure-name
    /// reference states it, and no other class reads it. <paramref name="reportSite"/> is where a REPORT SECTION clause's name is
    /// written (its report and the VARYING counters in scope, <see cref="ReportSite"/>), read only by the sum-counter and VARYING
    /// classes. A report-section reference that may reach several entries (a report group, a sum counter, a report entry) answers
    /// every candidate (<see cref="ReportCandidates{T}"/>), and the class's own funnel words two or more.
    /// <para>A function-prototype-name (<see cref="NameClass.FunctionPrototype"/>) is keyed by the word the &gt;&gt;COBOL-WORDS
    /// directive made of its token (<see cref="FunctionWord"/>, §2023:7.3.10.4 GR2 to GR4), the name every function-name lookup
    /// reads, never the token's text a second time (kb/Work PB1372).</para>
    /// <para>A name is ONE word, and a COBOL word is one token (<c>cobolWord</c> in CobolWords.g4), so the key is that token's
    /// text. A context of several tokens is no name and denotes nothing: its <c>GetText()</c> glues the tokens, so
    /// <c>A - B</c> would read as the file-name <c>A-B</c> and <c>F OF G</c> as <c>FOFG</c> (kb/Work PB2040, PB5932 review
    /// round 1). The rule lives here, once, so no caller can hand the table a glued text. A REFERENCE RULE's context is the one
    /// exception, and the table reads its words from its children: a procedure-name (<c>procedureName</c>, or a USE FOR DEBUGGING
    /// operand) is a head word and its qualifier (<see cref="ProcedureWords"/>).</para>
    /// </summary>
    internal IdentifiedName? Identify(ParserRuleContext written, NameClass wanted, bool report, NameReach reach = NameReach.Visible,
        ProcedureSite? site = null, ReportSite reportSite = default)
    {
        object? symbol;
        if (wanted == NameClass.Procedure)
            symbol = ProcedureNamed(written,
                site ?? throw new ArgumentNullException(nameof(site), "a procedure-name resolves from where it is written"));
        else if (IsReportClass(wanted))
            symbol = ReportSymbolNamed(written, wanted, reach, reportSite);   // the occurrence `report` gates is listing step 7b's
        else if (wanted == NameClass.FunctionPrototype)
            symbol = written.Start is { } only && written.Stop is { } last && only.TokenIndex == last.TokenIndex
                ? FunctionPrototypeNamed(FunctionKey(WrittenWord.Of(only)))   // the occurrence `report` gates is listing step 7b's
                : null;
        else if (WordOf(written) is not { } key)
            symbol = null;
        else if (IsConfigurationClass(wanted))
        {
            // A configuration-section name's reference is a §2023:8.3.2.2 USE of the word in the element it is written in
            // (kb/Work PB3153); a probe uses nothing. (Listing step 7b records the occurrence under the same flag.)
            (symbol, var kind) = ConfigurationNamed(key, wanted);
            if (symbol is not null && report) _data.RecordConfigurationNameUse(written, key.Word, kind);
        }
        else
            symbol = SymbolNamed(key, wanted, reach);   // the occurrence `report` gates is listing step 7b's
        return symbol is null ? null : new IdentifiedName(wanted, symbol);
    }

    /// <summary>
    /// The same lookup for a name no parse rule holds alone (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice
    /// 3"): a whole token the binder reads from a token list (a constant-name subscript of a LENGTH OF operand, §2023:13.10.3
    /// SR3), or a word INSIDE one token (an integer constant-name in a PICTURE character-string's repetition, §2023:13.10.3 SR2,
    /// which the scanner reads as part of the PICTURE string). <see cref="WrittenWord"/> carries the token and the word's span
    /// in it, so the position listing step 7b records is exact. A function-name the binder reads off a token (the reserved
    /// intrinsic-function words, a keyword-omitted reference's head) is identified so too. A procedure-name is a reference rule and
    /// is never written so.
    /// </summary>
    internal IdentifiedName? Identify(WrittenWord written, NameClass wanted, bool report, NameReach reach = NameReach.Visible)
    {
        _ = report;   // the occurrence it gates is listing step 7b's
        if (wanted == NameClass.Procedure)
            throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "a procedure-name is a reference rule: identify its context");
        if (IsConfigurationClass(wanted))
            throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "a configuration-section name is one word: identify its context");
        if (IsReportClass(wanted))
            throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "a report-section name is written in a context: identify it");
        object? symbol = wanted == NameClass.FunctionPrototype
            ? FunctionPrototypeNamed(FunctionKey(written))
            : SymbolNamed(Key(written.Text), wanted, reach);
        return symbol is null ? null : new IdentifiedName(wanted, symbol);
    }

    /// <summary>
    /// THE lookup of a written INDEX-NAME (§2023:8.4.2.2.2 Format 3), a qualified name no parse rule holds with its qualifiers (a
    /// token of a subscript's token list, or read off a <c>dataReference</c>): its §2023:8.4.2.2 candidate set, whose count is the
    /// caller's verdict (<c>NameResolution.ResolveIndexName</c>), or null when the spelling names no index-name visible here (the
    /// reference is then a data-name one). A qualifiable name's identification IS its candidate set, typed and unboxed: an
    /// index-name is read at every subscript name, several times per reference, so it answers no <see cref="IdentifiedName"/>
    /// around a boxed set (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, data-name lane, first slice"; the
    /// section 9 budget). <paramref name="report"/> is <see cref="Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>'s:
    /// listing step 7b records the occurrence here when it is true and the set has one member.
    /// </summary>
    internal NameCandidates<IndexDeclaration>? IdentifyIndexName(WrittenQualifiedName written, bool report)
    {
        _ = report;   // the occurrence it gates is listing step 7b's
        return IndexCandidates(written.Name, written.QualifierWords, _data.ActiveScope);
    }

    /// <summary>The symbol of class <paramref name="wanted"/> one word names here, or null.</summary>
    private object? SymbolNamed(CobolName key, NameClass wanted, NameReach reach) => wanted switch
    {
        NameClass.CommunicationDescription => _cds.GetValueOrDefault(key),
        NameClass.File => FileNamed(key, reach),
        NameClass.Constant => ConstantNamed(key),
        NameClass.ProgramPrototype => ProgramPrototypeNamed(key),
        NameClass.DebugRegister => DebugRegisterNamed(key),
        NameClass.TypeName => TypeDeclarationNamed(key, _data.ActiveScope),
        NameClass.CapacityRegister => CapacityRegisterNamed(key, _data.ActiveScope),
        _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not yet a class SymbolTable.Identify resolves"),
    };

    /// <summary>The name key of a written word: the table's one maker of a <see cref="CobolName"/>.</summary>
    private static CobolName Key(string word) => new(word);

    /// <summary>The key of a context that is exactly one token, or null for a context of several (or none): only a single
    /// word is a name. Internal for a pass that reads the parse tree before any element's table exists and keys its own
    /// facts by the written word (<c>VersionConformancePass</c>'s level-01 records): the key is the table's type, so the
    /// comparison is the table's one equality, never a comparer beside it.</summary>
    internal static CobolName? WordOf(ParserRuleContext written) =>
        written.Start is { } first && written.Stop is { } last && first.TokenIndex == last.TokenIndex ? Key(first.Text) : null;
}
