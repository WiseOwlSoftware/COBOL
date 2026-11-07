// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Editions;

/// <summary>The four mutually-exclusive options of a <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §2023:7.3.10.2).</summary>
public enum CobolWordsAction
{
    /// <summary>EQUATE literal-1 WITH literal-2 — literal-2 becomes a synonym for literal-1 (GR2).</summary>
    Equate,
    /// <summary>UNDEFINE literal-3 — literal-3 is no longer reserved/restricted (GR3).</summary>
    Undefine,
    /// <summary>SUBSTITUTE literal-4 BY literal-5 — literal-5 takes over literal-4's role; literal-4 becomes a
    /// user word (GR4).</summary>
    Substitute,
    /// <summary>RESERVE literal-6 — literal-6 shall not be used as a user-defined word (GR5).</summary>
    Reserve,
}

/// <summary>
/// One parsed <c>&gt;&gt;COBOL-WORDS</c> operation (ISO §2023:7.3.10). Both words are stored UPPER-CASE (SR2:
/// case-insensitive) with the enclosing literal quotes stripped. <see cref="Existing"/> is the
/// reserved/context-sensitive/intrinsic word SR3 governs (present for EQUATE/UNDEFINE/SUBSTITUTE);
/// <see cref="New"/> is the fresh user-defined word SR4 governs (present for EQUATE/SUBSTITUTE/RESERVE).
/// </summary>
/// <param name="Action">Which option.</param>
/// <param name="Existing">literal-1 (EQUATE) / literal-3 (UNDEFINE) / literal-4 (SUBSTITUTE); null for RESERVE.</param>
/// <param name="New">literal-2 (EQUATE) / literal-5 (SUBSTITUTE) / literal-6 (RESERVE); null for UNDEFINE.</param>
/// <param name="At">Where the directive is written, in the final preprocessed text: the line (1-based) and the
/// indicator's column (0-based) — the diagnostics anchor. SR3 and SR4's WORD-CATEGORY checks run in the conformance
/// pass, after the directive's text is gone, so the pass positions its sink from this (kb/Work PB1925); the frontend's
/// own SR1/SR2/SR5 diagnostics read the same place off the directive line directly. It is the operation's ONLY position
/// (the former 0-based <c>Line</c> was read nowhere and is deleted); a producer outside the directive stage passes
/// <c>default</c> (no cursor) knowingly.</param>
public sealed record CobolWordsOp(CobolWordsAction Action, string? Existing, string? New, DiagnosticCursor At)
{
    /// <summary>The directive as ISO §2023:7.3.10.2 writes it, its words upper-case and quoted
    /// (<c>&gt;&gt;COBOL-WORDS SUBSTITUTE "COMMA" BY "CMA"</c>): the ONE spelling of an operation for the map's own text
    /// (<see cref="CobolWordsMap.DirectiveText"/>) and for every diagnostic that names the user's directive.</summary>
    public string AsWritten => Action switch
    {
        CobolWordsAction.Equate => $">>COBOL-WORDS EQUATE \"{Existing}\" WITH \"{New}\"",
        CobolWordsAction.Undefine => $">>COBOL-WORDS UNDEFINE \"{Existing}\"",
        CobolWordsAction.Substitute => $">>COBOL-WORDS SUBSTITUTE \"{Existing}\" BY \"{New}\"",
        CobolWordsAction.Reserve => $">>COBOL-WORDS RESERVE \"{New}\"",
        _ => throw new InvalidOperationException($"CobolWordsAction {Action} has no directive spelling"),
    };

    /// <summary>⛔ THE ONE SENTENCE for a word an UNDEFINE or a SUBSTITUTE withdrew (<see cref="CobolWordsMap.RemovedBy"/>):
    /// the directive as written, the word it withdrew as the source writes it (<paramref name="word"/>) and the rule.
    /// UNDEFINE: the word "shall no longer be reserved or restricted in any way" (§2023:7.3.10.4 GR3). SUBSTITUTE: literal-5
    /// "shall be used in any syntax where" literal-4 "is documented as required or optional", and literal-4 "shall no
    /// longer be a reserved word, a context-sensitive word, nor an intrinsic function name" (GR4). Every diagnostic that
    /// names a withdrawn word (the syntax-error note, the clause binders) builds it here, so the cause reads alike
    /// (kb/Work PB4573).</summary>
    public string Withdrew(string word) => Action == CobolWordsAction.Undefine
        ? $"{AsWritten} withdrew {word} (ISO §2023:7.3.10.4 GR3)"
        : $"{AsWritten} withdrew {word}, and {New} now stands for it (ISO §2023:7.3.10.4 GR4)";

    /// <summary>What the withdrawal leaves of <paramref name="syntax"/>, a phrase of the standard's general formats that
    /// requires <paramref name="word"/>: under UNDEFINE it "shall not be available for use in this compilation group"
    /// (§2023:7.3.10.4 GR3); under SUBSTITUTE literal-5 "shall be used in any syntax where" the word "is documented as required"
    /// (GR4), so the phrase is written with literal-5 and is available.</summary>
    public string WithdrawnSyntax(string syntax, string word) => Action == CobolWordsAction.Undefine
        ? $"{syntax} requires the word {word}, so it is not available in this compilation group"
        : $"{syntax} requires the word {word}, so it is written with {New} in this compilation group";
}

/// <summary>
/// The per-compilation-group <c>&gt;&gt;COBOL-WORDS</c> override layer (ISO §2023:7.3.10; Annex D.12; Annex E.3.3
/// item 12) — the ONE runtime carrier the owner's recorded direction names: built in the Frontend
/// (<c>CobolWordsDirectiveProcessor</c>) from the directive text, then consulted by the lexer-applied
/// <c>CobolWordsRewriter</c> (token retyping), the map-aware lexer data-name gate, the composed
/// <see cref="ReservedWordSet"/> (RESERVE/UNDEFINE/SUBSTITUTE), and the binder's intrinsic-function-name
/// resolution. A pure-string data carrier so it crosses the frontend→compiler boundary like
/// <c>FlagState</c>/<c>RefModZeroLengthState</c>. <see cref="Empty"/> ⇒ every consumer is a no-op (the
/// zero-overhead invariant). Design SSOT: <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsMap
{
    /// <summary>The no-directive map — every consumer short-circuits on <see cref="IsEmpty"/>.</summary>
    public static readonly CobolWordsMap Empty = new([]);

    /// <summary>The edition whose word populations §2023:7.3.10.3 speaks of: the one that INTRODUCED the directive
    /// (<c>cobol-words-directive-2023</c>; §2023:7.3.10, Annex E.3.3 item 12), the only edition at which any rule of the
    /// directive is ever asked. SR3/SR4 (§2023:8.9 reserved ∪ §2023:8.10 context-sensitive ∪ §2023:8.11 intrinsic) and SR4's
    /// §2023:8.3.2.1 word limits are both read at it, so no caller restates the number.</summary>
    public static readonly int DirectiveEdition = ConstructRegistry.Find(Constructs.CobolWordsDirective2023)!.IntroducedIn;

    private readonly Dictionary<string, string> _synonyms;
    private readonly Dictionary<string, CobolWordsOp> _synonymOps;
    private readonly Dictionary<string, CobolWordsOp> _removedBy;
    private readonly HashSet<string> _deReserved;
    private readonly HashSet<string> _reserved;
    private readonly Dictionary<string, string> _reservedSynonyms;
    private readonly Dictionary<string, CobolWordsOp> _directingSynonyms;

    /// <summary>The words that begin a compiler directing statement (ISO §2023:7.2.2.2: "The compiler directing statements are
    /// the COPY statement and the REPLACE statement."), the ONE owner of the pair. §2023:7.3.10.4 GR6 puts them outside the
    /// directive: "A COBOL-WORDS directive does not affect any Compiler directing statements or Compiler directives", and
    /// the stage order of §2023:7.2.1 agrees (COPY is found in Step 1, REPLACE applies in Step 3, the directive's actions come
    /// last, in Step 4), so a synonym of one of these words begins no statement (kb/Work PB4488).</summary>
    public static readonly IReadOnlyList<string> CompilerDirectingStatementWords = ["COPY", "REPLACE"];

    public CobolWordsMap(IReadOnlyList<CobolWordsOp> ops)
    {
        Ops = ops;
        _synonyms = new(CobolNames.Comparer);       // keyed by the FRESH user word (literal-2 / literal-5): Annex C folds it
        _synonymOps = new(CobolNames.Comparer);     // the EQUATE or SUBSTITUTE that wrote each synonym
        _removedBy = new(CobolKeywords.Comparer);   // keyed by the EXISTING keyword (literal-3 / literal-4): basic letters alone
        _reserved = new(CobolNames.Comparer);       // RESERVE's literal-6 is a fresh user word
        _reservedSynonyms = new(CobolNames.Comparer);
        _directingSynonyms = new(CobolNames.Comparer);
        foreach (var op in ops)
        {
            switch (op.Action)
            {
                case CobolWordsAction.Equate when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word acts as the existing word
                    NoteSynonym(n, e, op);
                    break;
                case CobolWordsAction.Substitute when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word takes over the existing word's role
                    NoteSynonym(n, e, op);
                    _removedBy.TryAdd(e, op); // and the existing word becomes a user word
                    break;
                case CobolWordsAction.Undefine when op.Existing is { } e:
                    _removedBy.TryAdd(e, op);
                    break;
                case CobolWordsAction.Reserve when op.New is { } n:
                    _reserved.Add(n);
                    break;
            }
        }
        _deReserved = new(_removedBy.Keys, CobolKeywords.Comparer);
    }

    /// <summary>What a synonym of <paramref name="original"/> is besides a synonym: reserved, when §2023:8.9 reserves the
    /// original (GR2, GR4: it "shall be treated as a synonym for" the word, so §2023:8.3.2.1 rule 1 keeps it from being a
    /// user-defined word), and a word that begins no statement, when the original begins a compiler directing statement
    /// (GR6).</summary>
    private void NoteSynonym(string synonym, string original, CobolWordsOp op)
    {
        _synonymOps[synonym] = op;
        if (ReservedWords.Find(original) is { R2023: true }) _reservedSynonyms[synonym] = original;
        if (CompilerDirectingStatementWords.Any(w => CobolKeywords.Same(original, w))) _directingSynonyms[synonym] = op;
    }

    /// <summary>The parsed operations, in source order (SR3/SR4 validation + the token rewriter walk these).</summary>
    public IReadOnlyList<CobolWordsOp> Ops { get; }

    /// <summary>The map that <paramref name="text"/> (<see cref="DirectiveText"/>'s lines) states: the operations back, in order, with no
    /// source position. Null when a line is not one <see cref="DirectiveText"/> writes — a damaged record, which the caller reports.</summary>
    public static CobolWordsMap? FromDirectiveText(string text)
    {
        var ops = new List<CobolWordsOp>();
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var quoted = line.Split('"');   // ">>COBOL-WORDS EQUATE ", existing, " WITH ", new, ""
            const string Head = ">>COBOL-WORDS ";
            if (!line.StartsWith(Head, StringComparison.Ordinal)) return null;
            string verb = line[Head.Length..].Split(' ')[0];
            CobolWordsOp? op = (verb, quoted.Length) switch
            {
                ("EQUATE", 5) => new CobolWordsOp(CobolWordsAction.Equate, quoted[1], quoted[3], default),
                ("SUBSTITUTE", 5) => new CobolWordsOp(CobolWordsAction.Substitute, quoted[1], quoted[3], default),
                ("UNDEFINE", 3) => new CobolWordsOp(CobolWordsAction.Undefine, quoted[1], null, default),
                ("RESERVE", 3) => new CobolWordsOp(CobolWordsAction.Reserve, null, quoted[1], default),
                _ => null,
            };
            if (op is null) return null;
            ops.Add(op);
        }
        return ops.Count == 0 ? Empty : new CobolWordsMap(ops);
    }

    /// <summary>The map as the <c>&gt;&gt;COBOL-WORDS</c> directive lines that state it (ISO §2023:7.3.10.2), one per operation in source
    /// order, each ending in a line feed; empty for <see cref="Empty"/>. A skeleton publishes it (<c>CompileState.CobolWordsMap</c>,
    /// DESIGN-external-repository §6.1) and a compilation that expands the skeleton elsewhere reads it with the one directive
    /// processor, so the words mean the same there.</summary>
    public string DirectiveText => string.Concat(Ops.Select(op => op.AsWritten + "\n"));

    /// <summary>True when no <c>&gt;&gt;COBOL-WORDS</c> directive was present — every consumer short-circuits.</summary>
    public bool IsEmpty => Ops.Count == 0;

    /// <summary>A fresh user word (EQUATE literal-2 / SUBSTITUTE literal-5) → the existing word it stands in for
    /// (literal-1 / literal-4). The identifier→keyword rewriter and the intrinsic-synonym binder read this.</summary>
    public IReadOnlyDictionary<string, string> Synonyms => _synonyms;

    /// <summary>The existing word <paramref name="written"/> is a synonym for (EQUATE literal-2 → literal-1, SUBSTITUTE
    /// literal-5 → literal-4), or null when it is not a synonym: the ONE question "did the user write the word itself or its
    /// synonym" that a diagnostic asks to name the word as written (ISO §2023:7.3.10.4 GR2, GR4; kb/Work PB4573).
    /// <paramref name="written"/> is the word as the SOURCE writes it, never a retyped token's text, which is already
    /// the original's.</summary>
    public string? SynonymOf(string written) => _synonyms.GetValueOrDefault(written);

    /// <summary>The directive that made <paramref name="written"/> a synonym (the EQUATE that wrote it as literal-2 or the
    /// SUBSTITUTE that wrote it as literal-5), or null when it is not one: <see cref="SynonymOf"/>, asked for the directive,
    /// because the two differ in what becomes of the original word (EQUATE keeps it, GR2; SUBSTITUTE frees it, GR4) and a
    /// diagnostic that names the cause names the right rule (kb/Work PB4239).</summary>
    public CobolWordsOp? SynonymBy(string written) => _synonymOps.GetValueOrDefault(written);

    /// <summary>Words that LOSE their reserved/context/intrinsic status for this group (UNDEFINE literal-3 +
    /// SUBSTITUTE literal-4): the keyword→identifier rewriter, the <see cref="ReservedWordSet"/> suppress set,
    /// and the map-aware lexer data-name gate read this.</summary>
    public IReadOnlySet<string> DeReserved => _deReserved;

    /// <summary>The directive that DE-RESERVED <paramref name="written"/> (the UNDEFINE or SUBSTITUTE naming it as
    /// literal-3 / literal-4), or null when no directive removed it: the question <see cref="Resolve"/> answers with a
    /// null, asked for the directive itself, so a diagnostic names the cause and cites its
    /// <see cref="CobolWordsOp.At"/> (ISO §2023:7.3.10.4 GR3/GR4; kb/Work PB4309). SR5 (§2023:7.3.10.3) forbids the same COBOL
    /// word in more than one directive of a compilation group, so there is at most one.</summary>
    public CobolWordsOp? RemovedBy(string written) => _removedBy.GetValueOrDefault(written);

    /// <summary>Words newly reserved for this group (RESERVE literal-6): the <see cref="ReservedWordSet"/> reserve
    /// overlay reads this so a use as a user-defined word is COBOL0901-rejected.</summary>
    public IReadOnlySet<string> Reserved => _reserved;

    /// <summary>⛔ A SYNONYM OF A RESERVED WORD IS RESERVED (ISO §2023:7.3.10.4 GR2, GR4; kb/Work PB4488): a fresh user word
    /// (EQUATE literal-2 / SUBSTITUTE literal-5) → the §2023:8.9 reserved word it stands for, for every synonym whose original
    /// word §2023:8.9 reserves at <see cref="DirectiveEdition"/>. It is "treated as a synonym for" that word, and "Reserved words
    /// shall not be used as user-defined words or system-names" (§2023:8.3.2.1 rule 1). The lexer's retype
    /// (<c>CobolWordsRewriter.Plan</c>) makes a synonym its original's keyword TOKEN only where the original HAS a token, so
    /// a synonym of COPY, REPLACE, LOCALE, NESTED, ANYCASE, SYSTEM-DEFAULT, USER-DEFAULT, EXCEPTION-OBJECT, COMMA or MINUS
    /// stays an <c>IDENTIFIER</c> and only this set keeps it from being a name. A synonym of a context-sensitive word (§2023:8.10)
    /// or of an intrinsic function name stays what it was and is not here. The <see cref="ReservedWordSet"/> overlay reads
    /// it, beside <see cref="Reserved"/>.</summary>
    public IReadOnlyDictionary<string, string> ReservedSynonyms => _reservedSynonyms;

    /// <summary>The directive that made <paramref name="written"/> a synonym of a word that begins a compiler directing
    /// statement (<see cref="CompilerDirectingStatementWords"/>), or null. ISO §2023:7.3.10.4 GR6 puts those statements outside
    /// the directive, so such a synonym begins no COPY or REPLACE statement (kb/Work PB4488); a diagnostic that finds it at
    /// a syntax error names the directive and the rule.</summary>
    public CobolWordsOp? DirectingStatementSynonymOf(string written) => _directingSynonyms.GetValueOrDefault(written);

    /// <summary>
    /// The canonical COBOL word that a word WRITTEN in the source denotes under this compilation group's
    /// directives (ISO §2023:7.3.10.4 GR2/GR3/GR4) — <b>the ONE resolution</b> every consumer that classifies a word
    /// BY NAME calls. The lexer's <c>CobolWordsRewriter.Plan</c> retype can only reach words the lexer makes a keyword
    /// TOKEN; a word the binder classifies from its TEXT (the §15 phrase words ANYCASE/LOCALE/HEX/NAT/…, the
    /// SET-statement LC_ categories, the ALPHABET coded-set names) is reached ONLY here, so a site that compares
    /// raw text without calling this is inert to the directive in both directions (kb/Work PB250).
    /// <list type="bullet">
    /// <item><see langword="null"/> when <paramref name="written"/> was DE-RESERVED — UNDEFINE literal-3 (GR3:
    /// the word "shall no longer be reserved or restricted in any way … and any syntax requiring the use of the
    /// COBOL word that is the content of literal-3 shall not be available for use in this compilation group") or
    /// SUBSTITUTE literal-4 (GR4: it "shall no longer be a reserved word, a context-sensitive word, nor an
    /// intrinsic function name within this compilation group"). The caller must then treat the word as the
    /// user-defined word it now is — never as the keyword it spells.</item>
    /// <item>the CANONICAL word when <paramref name="written"/> is a synonym — EQUATE literal-2 (GR2: "may be
    /// used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function
    /// name that is the content of literal-1") or SUBSTITUTE literal-5 (GR4: "shall be used in any syntax where
    /// the COBOL word that is the content of literal-4 is documented as required or optional").</item>
    /// <item><paramref name="written"/> itself otherwise — including every word when <see cref="IsEmpty"/>, the
    /// zero-overhead no-directive path.</item>
    /// </list>
    /// SR3 (§2023:7.3.10.3) restricts literal-1/3/4 to a reserved word, a context-sensitive word or an
    /// intrinsic-function name, and SR5 forbids the same COBOL word in more than one directive of a compilation
    /// group, so the de-reserved set and the synonym keys are disjoint: the order of the two tests below is not a
    /// precedence choice. It matches the order <c>IntrinsicBinder.BindIntrinsicCore</c> has always used
    /// (removal tested against the ORIGINAL written name, then the synonym applied).
    /// </summary>
    /// <param name="written">The word as written in the source. A de-reserved keyword is found on its basic letters
    /// alone (<see cref="CobolKeywords.Comparer"/>; ISO §2023:8.1.3.2 3) a), SR2/GR1 — case-insensitive), a synonym by the
    /// Annex C fold of the fresh user word that stands for it (<see cref="CobolNames.Comparer"/>; §2023:8.1.3.2 4) b)).</param>
    /// <returns>The canonical word, or <paramref name="written"/> in its KEYWORD SPELLING
    /// (<see cref="CobolKeywords.Spelling"/>: the basic letters raised, every other character as written) when the
    /// directives leave it alone, so a caller that compares the result with an upper-case keyword is handed the word
    /// ready for it, and a word written with an extended letter (<c>DİSPLAY</c>, U+0130) is never a keyword
    /// (kb/Work PB4240, PB4420). A caller does not <see cref="CobolNames.UpperFold"/> the word first: that is the fold of a
    /// user-defined word, and a keyword is asked on the word as written.</returns>
    public string? Resolve(string written)
    {
        if (Ops.Count == 0) return CobolKeywords.Spelling(written);
        string? denoted = Denoted(written, out bool left);
        return left ? CobolKeywords.Spelling(written) : denoted;
    }

    /// <summary>The directives' reading of <paramref name="written"/>, the ONE place GR2-GR4 are applied, before any
    /// spelling: null when removed, the canonical word of a synonym, and <paramref name="written"/> itself with
    /// <paramref name="left"/> set when they leave it alone. <see cref="Is"/> compares the result on the basic letters
    /// and so needs no spelled copy (it is allocation-free).</summary>
    private string? Denoted(string written, out bool left)
    {
        left = false;
        if (_deReserved.Contains(written)) return null;
        if (_synonyms.TryGetValue(written, out string? canonical)) return canonical;
        left = true;
        return written;
    }

    /// <summary>
    /// True when the word WRITTEN in the source denotes the keyword <paramref name="keyword"/> under this
    /// group's directives — <b>the comparison every by-name classifier makes</b>, so <see cref="Resolve"/>'s
    /// GR2/GR3/GR4 reading is applied once and no caller re-implements it. Used by the parser's text predicates
    /// (<c>CobolParserCoreBase.Word</c>) and by every binder site that recognizes a §2023:8.9/§2023:8.10 word the lexer
    /// does not tokenize — the SET-statement locale categories, the ALPHABET coded-set names, CALL … AS NESTED.
    /// <para>The comparison with the keyword is on the BASIC LETTERS ALONE (<see cref="CobolKeywords.Same"/>; ISO
    /// §2023:8.1.3.2 3) a), 4): the Annex C fold governs user-defined words only), so a word written with an extended letter is
    /// not a keyword (<c>CLASSİFİCATİON</c>, U+0130, is no CLASSIFICATION; kb/Work PB4240, PB4420).
    /// Allocation-free (it never takes <see cref="Resolve"/>'s spelled copy): these run inside ANTLR's speculative prediction.</para>
    /// </summary>
    /// <remarks>⛔ Give this a word AS WRITTEN, and only for a word the LEXER does not tokenize (the
    /// §2023:8.9/§2023:8.10 words that arrive as bare IDENTIFIERs). For a word that may arrive as a keyword TOKEN use
    /// <c>CobolWordsRewriter.TokenIs</c> instead: the lexer's retype already resolved those, and resolving
    /// them again loses the synonym the user wrote.</remarks>
    public bool Is(string? written, string keyword)
        => written is not null
           && (Ops.Count == 0
               ? CobolKeywords.Same(written, keyword)
               : Denoted(written, out _) is { } w && CobolKeywords.Same(w, keyword));
}
