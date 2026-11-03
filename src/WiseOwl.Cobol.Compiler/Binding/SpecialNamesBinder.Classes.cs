// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>The CLASS and SYMBOLIC CHARACTERS clauses (ISO §12.3.7.2; §12.3.7.3 SR16, SR17; §12.3.7.4 GR11, GR12).
/// A partial of <see cref="SpecialNamesBinder"/>.</summary>
public sealed partial class SpecialNamesBinder
{
    /// <summary>SYMBOLIC CHARACTERS figurative constants (ISO §12.3.7.4 GR11 a — "Symbolic-character-1 defines a
    /// figurative constant"; kb/Work PB110): name → the ONE-character value (GR11 b/c — the character at ordinal
    /// integer-1 of the native or IN-alphabet coded character set) and its class. Substituted wherever a figurative
    /// constant may stand — the reference seams consult <see cref="SymbolicOf"/> exactly as they consult the
    /// §13.10 constant table, and the value fills like every figurative (§8.3.3.6.4 GR2 / GR10).</summary>
    internal ConfigurationNames<(string Value, bool National)> SymbolicCharacters =>
        field ??= new(UserWordKind.SymbolicCharacter, _host.RecordConfigurationNameUse);

    /// <summary>The symbolic character a BARE (unqualified, unsubscripted) data reference names, or null — the
    /// twin of <c>ConstantOf</c> (one shape for "a word that stands for a literal"). A resolution: the word is
    /// recorded as used as a symbolic-character (§8.3.2.2; kb/Work PB3153).</summary>
    internal (string Value, bool National)? SymbolicOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w ? SymbolicOf(dref, w.GetText()) : null;

    /// <summary>Does a BARE data reference spell a symbolic character visible here? The predicate twin of
    /// <see cref="SymbolicOf(Core.DataReferenceContext)"/>, recording no use.</summary>
    internal bool NamesSymbolic(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w && NamesSymbolic(w.GetText());

    /// <summary>Does the bare word <paramref name="word"/> name a symbolic character visible here (one
    /// <see cref="HidesSymbolic"/> does not hide)? The predicate twin of
    /// <see cref="SymbolicOf(Antlr4.Runtime.RuleContext, string)"/>, recording no use.</summary>
    internal bool NamesSymbolic(string word) => SymbolicCharacters.Declares(word) && !HidesSymbolic(word);

    /// <summary>The symbolic character the word <paramref name="word"/> written at <paramref name="site"/> names, or
    /// null (the FigurativeOperand ALL-form lookup, a concatenation operand). A resolution, recorded as a use
    /// (§8.3.2.2; kb/Work PB3153) — only when the symbolic-character is visible: one a nearer declaration of the
    /// spelling hides (<see cref="HidesSymbolic"/>) is not what the word references, so it is neither resolved nor
    /// recorded.</summary>
    internal (string Value, bool National)? SymbolicOf(Antlr4.Runtime.RuleContext site, string word) =>
        !HidesSymbolic(word) && SymbolicCharacters.TryUse(site, word, out var def) ? def : null;

    /// <summary>Whether a nearer declaration of <paramref name="word"/> hides the symbolic-character of that spelling
    /// (§8.4.6.1, §8.4.6.2.1 3); kb/Work PB3154 review R1-1): an inherited one a nearer source element's data-,
    /// condition- or index-name hides (<see cref="DataBinder.DropShadowedConstants"/>), or any one the method being bound
    /// declares a name of (<see cref="OoDataBinder.ActiveMethodDeclares"/>). Asked by every symbolic lookup, the resolving and the
    /// predicate alike, so they cannot disagree.</summary>
    private bool HidesSymbolic(string word) => _shadowedSymbolics.Contains(word) || _host.Oo.ActiveMethodDeclares(word);

    /// <summary>The inherited symbolic-characters a nearer declaration hides (<see cref="DataBinder.DropShadowedConstants"/>).
    /// Hidden by <see cref="HidesSymbolic"/>, never removed from <see cref="SymbolicCharacters"/>: a contained
    /// unit inherits the whole table from this one, and this unit's local name hides nothing from it.</summary>
    private readonly HashSet<string> _shadowedSymbolics = new(CobolNames.Comparer);

    /// <summary>Hide every inherited symbolic-character a nearer declaration of its spelling hides (§8.4.6.2.1 3);
    /// the symbolic-character half of <see cref="DataBinder.DropShadowedConstants"/>, which owns the question
    /// (<see cref="DataBinder.DeclaredNearerThan"/>) and asks it of each inherited name at its containment distance.</summary>
    internal void HideShadowedSymbolics()
    {
        foreach (var (name, depth) in SymbolicCharacters.InheritedDepths)
            if (_host.DeclaredNearerThan(name, depth)) _shadowedSymbolics.Add(name);
    }

    /// <summary>One <c>CLASS class-name-1 [FOR {ALPHANUMERIC | NATIONAL}] IS {literal-5 [THROUGH literal-6]}…
    /// [IN alphabet-name-4]</c> clause (ISO §12.3.7.2): expand each value item to its member characters — a
    /// multi-character literal lists each character; a THRU pair contributes the contiguous native-collating range
    /// between the two single characters, ASCENDING OR DESCENDING (§12.3.7.4 GR12 — NC174A's <c>"D" THROUGH "A"</c>
    /// equals <c>"A" THRU "D"</c>).
    /// <para>⛔ THE FOR PHRASE IS READ ONCE, HERE, AND EVERY §12.3.7.3 SR17 RULE KEYS ON THAT ONE READING (kb/Work
    /// PB976). It used to be parsed and IGNORED, so the whole class-dependent half of SR17 — b1/c1 the IN alphabet's
    /// class, b3/c3 each noninteger literal's class, b4/c4 one character under THROUGH — went unenforced:
    /// <c>CLASS C IS N"0" THRU N"9"</c> and <c>CLASS C FOR NATIONAL IS 1 THRU 5 IN an-alphanumeric-alphabet</c>
    /// compiled clean. SR17 a — "<i>When neither the ALPHANUMERIC phrase nor the NATIONAL phrase is specified, the
    /// ALPHANUMERIC phrase is implied</i>" — is the default below. The operand rules are the ONE decoder the ALPHABET
    /// clause uses (<see cref="LiteralPhraseOperand"/>), under the CLASS clause's own rule numbers
    /// (<see cref="LiteralPhraseRules.Class"/>).</para></summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        // CLASS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is VersionConformancePass
        // ParseArm.VisitClassDefinitionClause (14g.4, recognition).
        using var _ = Edition.At(cd);
        string name = cd.cobolWord(0).GetText();
        _host.DeclareUserWord(name, UserWordKind.ClassName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        bool national = ForPhraseIsNational(cd.specialNamesForPhrase(), cd.misplacedSpecialNamesForPhrase());
        // The IN phrase (ISO §12.3.7.4 GR12 a; kb/Work PB110): a NUMERIC literal is the ordinal of a character
        // within the character set referenced by alphabet-name-4 — not the native set. SR17 d (a LOCALE alphabet)
        // and an undeclared name refuse through the ONE resolver; the clause then binds no class (its references
        // stay loud) rather than silently reverting to native ordinals.
        CodedCharacterSet? inSet = null;
        if (cd.cobolWord().Length > 1)
        {
            string inName = cd.cobolWord(1).GetText();
            inSet = CodedCharacterSetOf(cd.cobolWord(1), inName, $"CLASS {name} IN {inName}",
                "ISO §12.3.7.3 SR17 d — alphabet-name-4 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
            // SR17 b1/c1 — "When the IN phrase is specified, alphabet-name-4 shall reference an alphabet that defines
            // an alphanumeric / a national character set". The SYMBOLIC CHARACTERS clause's SR16 e1/f1 twin is the
            // same test in SwitchBindSymbolic.
            if (inSet.National != national)
            {
                Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"CLASS {name}{(national ? " FOR NATIONAL" : "")} "
                    + $"IN {inName}: alphabet-name-4 shall reference an alphabet that defines "
                    + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} character set — this alphabet is "
                    + $"{(inSet.National ? "FOR NATIONAL" : "alphanumeric")} (ISO §12.3.7.3 SR17 {(national ? "c1" : "b1")})");
                return;
            }
        }
        var rules = LiteralPhraseRules.Class(name, national, inSet);
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            // literal-5 and literal-6, each written as a literal or as a constant-name (§13.10.3 SR2; kb/Work PB1942).
            var lits = item.specialNamesLiteral()
                .Select(o => (Antlr4.Runtime.Tree.IParseTree)o.literal() ?? o.cobolWord()).ToArray();
            if (LiteralPhraseOperand(lits[0], rules) is not { } lo) continue;
            if (lits.Length < 2) { members.Append(lo.Characters); continue; }
            // SR17 b4/c4 — "Each alphanumeric / national literal, when a THROUGH phrase is specified, shall be one
            // character in length." A multi-character operand used to be taken as a plain literal-5 with literal-6
            // silently dropped (`"AB" THRU "C"` was the class {A, B}).
            if (LiteralPhraseOperand(lits[1], rules) is not { } hi
                || !OneCharacterOperands([lo, hi], rules, through: true)) continue;
            // GR12: "the contiguous characters in the NATIVE character set beginning with … literal-5, and ending
            // with … literal-6" — native, even under IN (GR12 a/b place each END in alphabet-name-4's set).
            char a = lo.Characters[0], b = hi.Characters[0];
            if (a > b) (a, b) = (b, a);
            for (char c = a; ; c++) { members.Append(c); if (c == b) break; }
        }
        string set = members.ToString();
        // SR17 b5/c5 — "The number of characters specified shall not exceed the number of characters in the native
        // … character set or, when the IN phrase is specified, the number of characters in the character set
        // referenced by alphabet-name-4." Without IN it cannot fire (distinct members of a 65,536-character
        // repertoire cannot outnumber it); under IN a smaller set (STANDARD-1's 128) can be exceeded.
        if (inSet is not null)
        {
            int distinct = set.Distinct().Count();
            if (distinct > inSet.OrdinalCount)
                Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"{rules.What} IN {cd.cobolWord(1).GetText()}: "
                    + $"{distinct} characters are specified — more than the {inSet.OrdinalCount} characters of the "
                    + $"character set referenced by alphabet-name-4 ({inSet.Phrase}) (ISO §12.3.7.3 {rules.Rule('5')})");
        }
        UserClasses.TryAdd(name, new UserClassDef(set, national));
    }

    /// <summary>⛔ THE ONE READING OF A SPECIAL-NAMES FOR PHRASE, for all three clauses that print it (ALPHABET,
    /// CLASS, SYMBOLIC CHARACTERS — ISO §12.3.7.2): NATIONAL when the phrase says so, ALPHANUMERIC otherwise
    /// (§12.3.7.3 SR13, SR16 d, SR17 a — "the ALPHANUMERIC phrase is implied"). The phrase belongs between the
    /// clause's name and IS; one written after the definition (<paramref name="misplaced"/>) has already been
    /// refused by name in ClosedFormatPass (kb/Work PB977), and is read here only so the rest of the clause binds
    /// in the class the user evidently meant instead of cascading class-rule diagnostics.</summary>
    private static bool ForPhraseIsNational(Core.SpecialNamesForPhraseContext? iso,
        Core.MisplacedSpecialNamesForPhraseContext? misplaced)
        => (iso ?? misplaced?.specialNamesForPhrase())?.NATIONAL() is not null;

    /// <summary>Bind one SYMBOLIC CHARACTERS clause (ISO §12.3.7.2; §12.3.7.3 SR16; §12.3.7.4 GR11 — kb/Work
    /// PB110: the clause was accepted-inert): each symbolic-character-1 defines a FIGURATIVE CONSTANT whose value
    /// is the character at ordinal integer-1 in the native character set of the clause's class (ALPHANUMERIC
    /// implied — SR16 d) or, under IN, in the coded character set of alphabet-name-3 (GR11 b/c; SR16 e1/f1 the
    /// class of that alphabet; SR16 g / the ONE resolver refuse a LOCALE alphabet). The pairing is positional
    /// across the WHOLE clause (SR16 b: "by position in the SYMBOLIC CHARACTERS clause"; kb/Work PB4108), the first name
    /// with the first integer, with a one-to-one correspondence (SR16 c); a name may be defined
    /// once across ALL the paragraph's SYMBOLIC CHARACTERS clauses (SR16 a).</summary>
    private void SwitchBindSymbolic(Core.SymbolicCharactersClauseContext sc)
    {
        using var _ = Edition.At(sc);
        bool national = ForPhraseIsNational(sc.specialNamesForPhrase(), sc.misplacedSpecialNamesForPhrase());
        CodedCharacterSet? inSet = null;
        if (sc.cobolWord() is { } inWord && inWord is not null)
        {
            inSet = CodedCharacterSetOf(inWord, inWord.GetText(), $"SYMBOLIC CHARACTERS IN {inWord.GetText()}",
                "ISO §12.3.7.3 SR16 g — alphabet-name-3 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
            if (inSet.National != national)
            {
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS{(national ? " FOR NATIONAL" : "")} "
                    + $"IN {inWord.GetText()}: alphabet-name-3 shall reference an alphabet that defines "
                    + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} character set — this alphabet is "
                    + $"{(inSet.National ? "FOR NATIONAL" : "alphanumeric")} (ISO §12.3.7.3 SR16 {(national ? "f1" : "e1")})");
                return;
            }
        }
        var (names, ords, groupsWellFormed) = SymbolicClauseOperands(sc);
        if (!groupsWellFormed || names.Count != ords.Count)
        {
            if (!groupsWellFormed)
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, "SYMBOLIC CHARACTERS: the clause repeats the "
                    + "group {symbolic-character-1}... [IS|ARE] {integer-1}..., so each repetition names at least one "
                    + "symbolic-character-1 and then at least one integer-1: the clause must open with a "
                    + "symbolic-character-1 and close with an integer-1 (ISO §12.3.7.2 general format)");
            else
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: {names.Count} "
                    + $"symbolic-character name(s) against {ords.Count} integer(s) — there shall be a one-to-one "
                    + "correspondence between the occurrences of symbolic-character-1 and of integer-1 in the clause, "
                    + "paired by position (ISO §12.3.7.3 SR16 b/c)");
            // RECOVERY, so the one refusal does not cascade into a false "'SC1' is not defined" at each use: the names
            // the clause wrote are declared with a placeholder value (never observed: the clause failed).
            foreach (var name in names)
            {
                _host.DeclareUserWord(name.GetText(), UserWordKind.SymbolicCharacter);
                SymbolicCharacters.TryAdd(name.GetText(), ("\0", national));
            }
            return;
        }
        for (int i = 0; i < names.Count; i++)
        {
            string symName = names[i].GetText();
            _host.DeclareUserWord(symName, UserWordKind.SymbolicCharacter);   // §8.3.2.2 (kb/Work PB1083)
            // integer-1 is an INTEGERLIT — unsigned digits by the grammar (§5.5 1)) — or an integer constant-name
            // (§13.10.3 SR2, read by the same reader: kb/Work PB1948) — so only its magnitude can be wrong. ⛔ It used to be read with `int.TryParse` and a failure `continue`d: an ordinal too long
            // for an int bound NOTHING and drew NO diagnostic, and the name then surfaced as "not defined" at
            // its first use (kb/Work PB1557's sibling). It is an integer-n, so it is read by THE ONE integer-n
            // reader — the very one the ALPHABET and CLASS literal phrases read their ordinals through
            // (LiteralPhraseOperand; kb/Work PB1091) — which saturates at int.MaxValue: a saturated value is out
            // of range of every character set, so the SR16 check below reports it.
            var (ordText, ordValue) = ords[i];
            int ordinal = ordValue.GetValueOrDefault();
            string? value = ordValue is null ? null
                : inSet is not null ? inSet.CharAt(ordinal)
                : ordinal is >= 1 and <= CollatingTable.Repertoire ? ((char)(ordinal - 1)).ToString() : null;
            if (value is null)
            {
                // A null ordinal is a constant-name the integer-n reader REFUSED and reported; only a value that
                // read is out of range here.
                if (ordValue is not null)
                    Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS {symName} IS "
                        + $"{ordText}: the ordinal position does not exist in the "
                        + $"{(inSet is not null ? $"character set referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} characters)" : $"native character set ({CollatingTable.Repertoire} characters)")}"
                        + $" — ISO §12.3.7.3 SR16 {(national ? "f" : "e")}{(inSet is not null ? "1" : "2")}");
                // RECOVERY, so one bad ordinal does not cascade: the name is DECLARED (DeclareUserWord above), and a
                // statement that uses it must find it defined rather than draw a second, false diagnostic ("'BIG' is
                // not defined") for a name the program did write. The placeholder value is never observed — the
                // clause already failed, so the compilation produces no program (the posture LiteralPhraseOperand's
                // class-rule recovery takes for an operand of the wrong class). kb/Work PB1091.
                SymbolicCharacters.TryAdd(symName, ("\0", national));
                continue;
            }
            if (!SymbolicCharacters.TryAdd(symName, (value, national)))
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: '{symName}' is "
                    + "already defined — a given symbolic-character-1 may be specified only once within the SYMBOLIC "
                    + "CHARACTER clauses of this SPECIAL-NAMES paragraph (ISO §12.3.7.3 SR16 a)");
        }
    }

    /// <summary>⛔ THE NAMES AND THE ORDINALS OF ONE SYMBOLIC CHARACTERS CLAUSE, each in source order across ALL the clause's
    /// entries (ISO §12.3.7.2: <c>{symbolic-character-1}… [IS|ARE] {integer-1}…</c>, the braced group repeating;
    /// §12.3.7.3 SR16 b: the pairing is "by position in the SYMBOLIC CHARACTERS clause", the first symbolic-character-1
    /// with the first integer-1; SR16 c: one to one between their occurrences — kb/Work PB4108). An ordinal is an
    /// <c>integer-n</c>, so an integer CONSTANT-NAME may stand for it (§13.10.3 SR2; kb/Work PB1948) and every ordinal is
    /// read through THE one integer-n reader.
    /// <para>The names and the ordinals are written as WORDS side by side, and a constant-name is the same kind of token
    /// as the next symbolic-character-1, so the parser cannot tell them apart (<c>symbolicCharacterEntry</c> parses an
    /// entry as literals, as <c>IS|ARE</c> then operands, or as words only, and its first alternative ends at the last
    /// literal, so a constant-name ordinal after a literal one opens a NEW entry). The entry is therefore NOT a unit of
    /// the rule: this walk reads the entries' tokens as one stream in SOURCE ORDER whatever the parse called them and
    /// decides each WORD by the one fact only the binder has — whether the compilation unit defines it as a constant
    /// (<see cref="DataBinder.FindConstant"/>): a literal is an ordinal; a word that names a constant is an ordinal; the first word
    /// after IS/ARE is an ordinal whatever it names (so an undefined one is reported by the reader as "not a defined
    /// constant-name"); any other word is a symbolic-character-1. An ordinal whose value is null was refused by the
    /// reader and is already reported.</para></summary>
    private (List<Core.CobolWordContext> Names, List<(string Text, int? Value)> Ordinals, bool GroupsWellFormed)
        SymbolicClauseOperands(Core.SymbolicCharactersClauseContext sc)
    {
        const string where = "SYMBOLIC CHARACTERS clause";
        var names = new List<Core.CobolWordContext>();
        var ordinals = new List<(string Text, int? Value)>();
        // The repeated group is `{name}... [IS|ARE] {integer}...`: names then ordinals, each at least once, so the stream
        // of operand kinds is (N+ O+)+ — it opens with a name and closes with an ordinal, and nothing more is needed
        // (any such stream is a concatenation of N+ O+ groups). The entry is not the unit of the rule (above), so the
        // grammar cannot enforce this and the count comparison in SwitchBindSymbolic cannot see it (kb/Work PB4108, R1-1).
        bool? firstIsName = null;
        bool lastIsName = false;
        foreach (var entry in sc.symbolicCharacterEntry())
        {
            bool afterIs = false;
            foreach (var child in entry.children)
            {
                if (child is Antlr4.Runtime.Tree.ITerminalNode)   // the optional IS / ARE
                {
                    afterIs = true;
                    continue;
                }
                Core.CobolWordContext? word = child switch
                {
                    Core.CobolWordContext w => w,
                    Core.IntegerOperandContext io when io.integerLiteral() is null => io.cobolWord(),
                    _ => null,
                };
                if (word is not null && !afterIs && _host.FindConstant(word.GetText()) is null && !_host.IsFailedConstant(word.GetText()))
                {
                    names.Add(word);
                    firstIsName ??= true;
                    lastIsName = true;
                    continue;
                }
                afterIs = false;
                firstIsName ??= false;
                lastIsName = false;
                ordinals.Add(child switch
                {
                    Core.IntegerLiteralContext il => (il.GetText(),
                        (int?)WiseOwl.Cobol.Validation.IntegerOperandRules.HostValue(il)),
                    Core.IntegerOperandContext io => (io.GetText(), _host.IntegerOperandValue(io, where)),
                    _ => (word!.GetText(), _host.ConstantIntegerText(word, word.GetText(), where) is { } digits
                        ? WiseOwl.Cobol.Validation.IntegerOperandRules.HostValue(digits) : null),
                });
            }
        }
        return (names, ordinals, firstIsName == true && !lastIsName);
    }
}
