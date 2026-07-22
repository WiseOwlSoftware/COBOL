// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;              // FlagState / PictureAnalyzer / EditionContext
using CobolNet.Binding.Model;        // Usage / PicCategory / PicInfo
using CobolNet.Binding.Passes;       // GroupBindContext
using CobolNet.Editions;             // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Generated;   // CobolParserCore / CobolParserCoreBaseVisitor
using CobolNet.Frontend.Preprocessor;// FlagDirective / FlagOption / FlagOptions / FlagDirectiveLine

namespace CobolNet.Validation;

/// <summary>
/// The migration-flagging pass (ISO §7.3.14 FLAG-02 / §7.3.15 FLAG-14) — a sibling to
/// <see cref="VersionConformancePass"/>, run right after it in <c>BinderDriver</c>. It emits a Warning for every
/// construct that an active <c>&gt;&gt;FLAG-02</c>/<c>&gt;&gt;FLAG-14</c> option flags (GR1: the implementor SHALL
/// provide the warning mechanism). It is a SEPARATE pass, not a bolt-on, because flagging is an orthogonal axis to
/// edition gating: it fires on the user's DIRECTIVE STATE (a <c>&gt;&gt;FLAG</c> ON at the construct's line),
/// regardless of <c>--std</c>, and is ALWAYS a Warning (never fails the compile).
///
/// It is a PARSE-TREE visitor (the source-line reason, design D2): the flag fold is line-sensitive (GR2 — a flag
/// applies to the text FOLLOWING its directive), and a bound statement carries no uniform source line, whereas
/// every parse node has <c>ctx.Start.Line</c> — anchored to the same final-text lines as the
/// <see cref="FlagEvent"/>s (<see cref="FlagDirectiveProcessor"/> collects on the FINAL text). It reuses the
/// generated ANTLR base visitor (the same traversal mechanism the <see cref="VersionConformancePass"/> parse arm uses).
/// Syntactic options decide from the parse node directly; options needing a resolved fact look it up by name in the
/// models reachable from <see cref="GroupBindContext"/>. Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
internal sealed class FlagConformancePass : CobolParserCoreBaseVisitor<object?>
{
    private readonly FlagState _flag;
    private readonly IDiagnosticSink _sink;
    // Resolved-model lookups keyed by SOURCE NAME (the flag pass runs before the file-connector renaming, so the
    // model still carries source names — see BinderDriver): the WRITE targets (record- and file-names) whose file
    // has a LINAGE clause (FLAG-14 m), and the report-names whose description carries a VARYING clause (FLAG-02 f).
    private readonly IReadOnlySet<string> _linageWriteTargets;
    private readonly IReadOnlySet<string> _varyingReports;
    // The compile-time directive states the state-coupled options read: the >>REF-MOD-ZERO-LENGTH tri-state (i) and
    // the >>TURN EC-checking model (i needs EC-BOUND-REF-MOD; e needs EC-RANGE-INDEX).
    private readonly RefModZeroLengthState _refModZl;
    private readonly TurnState _turn;
    // A discard EditionContext so the reused PictureAnalyzer (the ONE picture-category mechanism) can classify a
    // parse-tree PICTURE string WITHOUT re-emitting its bind-time diagnostics to the real sink. At 2023 (the
    // superset) so no legal symbol is spuriously rejected; the picture was already validated during binding.
    private readonly EditionContext _discard = new(2023);

    private FlagConformancePass(FlagState flag, IDiagnosticSink sink,
        IReadOnlySet<string> linageWriteTargets, IReadOnlySet<string> varyingReports,
        RefModZeroLengthState refModZl, TurnState turn)
    {
        _flag = flag;
        _sink = sink;
        _linageWriteTargets = linageWriteTargets;
        _varyingReports = varyingReports;
        _refModZl = refModZl;
        _turn = turn;
    }

    /// <summary>Flag every construct an active FLAG option covers. A no-op (no walk) when no FLAG directive is
    /// present — the zero-overhead invariant: a source with no <c>&gt;&gt;FLAG</c> line is byte-identical to a
    /// build without this pass.</summary>
    public static void Run(GroupBindContext group, FlagState flag, IDiagnosticSink sink)
    {
        if (!flag.Any) return;

        // Build the two source-name lookups the statement detectors need (m WRITE-END-OF-PAGE / f
        // TERMINATE-WITH-VARYING). Files/reports live in program units (not OO class data), so units suffice.
        var linage = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var varying = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in group.Units)
        {
            foreach (var file in unit.Data.Files)
                if (file.Linage is not null)
                {
                    linage.Add(file.CobolName);
                    foreach (var rec in file.Records)
                        if (rec.CobolName is { } rn) linage.Add(rn);
                }
            foreach (var report in unit.Data.Reports)
                if (report.Groups.Any(g => g.Lines.Any(l => l.Fields.Any(f => f.Varyings.Count > 0))))
                    varying.Add(report.Name);
        }

        new FlagConformancePass(flag, sink, linage, varying,
            group.Session.RefModZeroLength, group.Session.Turn).Visit(group.Tree);
    }

    /// <summary>Emit the option's Warning if it is flagging at <paramref name="line"/>. The Code is per-directive;
    /// the ConstructId (suppress-key), Message, and Citation are per-option (spec-faithful — each names its own GR4
    /// sub-rule + Annex-E item).</summary>
    private void Flag(FlagOption option, int line, string where)
    {
        if (!_flag.IsOnAt(line, option)) return;
        var info = FlagOptions.Info(option);
        bool f02 = info.Directive == FlagDirective.Flag02;
        string code = (f02 ? DiagnosticCatalog.Flag02Warning : DiagnosticCatalog.Flag14Warning).Code;
        string constructId = $"flag-{(f02 ? "02" : "14")}-{info.Word.ToLowerInvariant()}";
        _sink.Report(new EditionDiagnostic(code, EditionSeverity.Warning, constructId,
            $"{info.Change} — flagged by >>{FlagDirectiveLine.DirectiveWord(info.Directive)} {info.Word}",
            where, info.Citation));
    }

    // ── FLAG-14 h READ-PREVIOUS (§7.3.15.4 GR4 h) — a READ … PREVIOUS (sequential or keyed; the parse rule is
    //    common). Purely syntactic. ──
    public override object? VisitReadStatement(CobolParserCore.ReadStatementContext ctx)
    {
        if (ctx.readDirection()?.PREVIOUS() is not null)
            Flag(FlagOption.Flag14ReadPrevious, ctx.Start.Line, "the READ PREVIOUS statement");
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 c I-O-STATUS-07 (§7.3.14.4 GR4 c) — a CLOSE specifying WITH NO REWIND or the UNIT phrase (NOT
    //    REEL — GR4 c names only UNIT and NO REWIND). Purely syntactic; one flag per CLOSE statement. ──
    public override object? VisitCloseStatement(CobolParserCore.CloseStatementContext ctx)
    {
        foreach (var phrase in ctx.closeFilePhrase())
        {
            var opt = phrase.closeOption();
            if (opt is null) continue;
            if (opt.UNIT() is not null || (opt.NO() is not null && opt.REWIND() is not null))
            {
                Flag(FlagOption.Flag02IoStatus07, ctx.Start.Line, "the CLOSE WITH NO REWIND / UNIT statement");
                break;   // GR4 c flags the CLOSE statement once, however many such phrases it carries
            }
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 i REF-MOD-ZERO-LENGTH (§7.3.15.4 GR4 i; E.2 item 23) — a reference modification flagged ONLY when
    //    the >>REF-MOD-ZERO-LENGTH directive is UNSPECIFIED (neither explicit ON nor OFF) at the site AND
    //    EC-BOUND-REF-MOD checking is on there (a zero-length result would then raise the exception). A ref-mod
    //    reaches the parser two ways: the default-mode `refModSpec`, and — for a data reference — a
    //    `subscriptOrRefMod` carrying a SUB_COLON (the grammar leaves subscript-vs-refmod to the binder). ──
    public override object? VisitRefModSpec(CobolParserCore.RefModSpecContext ctx)
    {
        FlagRefMod(ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitSubscriptOrRefMod(CobolParserCore.SubscriptOrRefModContext ctx)
    {
        // A top-level SUB_COLON among the sub-tokens ⇒ a reference modification, not a subscript list.
        if (ctx.subToken().Any(t => t.SUB_COLON() is not null)) FlagRefMod(ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    private void FlagRefMod(int line)
    {
        if (_refModZl.IsUnspecifiedAt(line) && _turn.Enabled("EC-BOUND-REF-MOD", null, line))
            Flag(FlagOption.Flag14RefModZeroLength, line,
                "the reference modification (>>REF-MOD-ZERO-LENGTH unspecified and EC-BOUND-REF-MOD checking on)");
    }

    // ── The VALUE-clause data options — anchored at the VALUE clause's source line (the flaggable syntax). k
    //    reaches ANY real data item; g/l/j reach numeric-edited items (a PICTURE-string property, §13.18.40, via the
    //    ONE PictureAnalyzer). FILLER-safe (no name lookup). ──
    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var (picture, value, usage) = Clauses(ctx);
        if (value is not null)
        {
            int line = value.Start.Line;
            var fig = FirstDescendant<CobolParserCore.FigurativeConstantContext>(value);

            // k VALUE-FIG-CON-LENGTH (§7.3.15.4 GR4 k; E.2 item 11) — a figurative constant VALUE on a data item
            // with NO SPECIFIED LENGTH: no PICTURE, no length-implying USAGE (DISPLAY/absent gives none without a
            // PICTURE; COMP-*/INDEX/POINTER/… imply one), and NOT a group (a group's figurative VALUE is filled to
            // the subordinates' length, §13.18.63 SR13). Applies to any real data item (levels 1-49, 77); a
            // level-88 condition-name reaches here via valueClause and is excluded.
            if (fig is not null && picture is null && UsageGivesNoLength(usage)
                && IsRealDataLevel(ctx) && !HasSubordinates(ctx))
                Flag(FlagOption.Flag14ValueFigConLength, line,
                    "a figurative constant in the VALUE clause of a data item with no specified length");

            // g/l/j — numeric-edited items only.
            if (picture is not null && IsNumericEditedPicture(picture))
            {
                if (fig is not null)
                {
                    // g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO — the figurative constant ZERO (ZERO/ZEROS/ZEROES, with
                    // or without ALL). One condition, two independently-toggled options.
                    if (fig.ZERO() is not null)
                    {
                        Flag(FlagOption.Flag14NumEdZeroFigconst, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
                        Flag(FlagOption.Flag14ValueZero, line, "the figurative constant ZERO in the VALUE clause of a numeric-edited item");
                    }
                }
                else if (LiteralHasNoEditingSymbols(value))
                {
                    // j VALUE-EDITING — the VALUE is a LITERAL (numeric or nonnumeric, NOT a figurative constant)
                    // carrying no editing symbols. §13.18.63 SR6/SR11 + E.2 item 29: at 2023 editing is auto-supplied
                    // for a numeric literal and compulsory for an alphanumeric/national literal (both changed 2014→2023).
                    Flag(FlagOption.Flag14ValueEditing, line, "a numeric-edited VALUE literal that contains no editing symbols");
                }
            }
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-14 m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m) — a WRITE that ALLOWS an END-OF-PAGE phrase (its file has a
    //    LINAGE clause, §14.9.51) but does not specify it. The "allows EOP" fact is the file's LINAGE, resolved by
    //    name from the model; anchored at the WRITE. ──
    public override object? VisitWriteStatement(CobolParserCore.WriteStatementContext ctx)
    {
        if (ctx.writeAtEndOfPage() is null)
        {
            // recordName (a dataReference — unqualified for a WRITE record in practice; a qualified record name is a
            // rare false-negative for this advisory flag) or the FILE fileName form.
            string? target = ctx.recordName()?.GetText() ?? ctx.fileName()?.GetText();
            if (target is not null && _linageWriteTargets.Contains(target))
                Flag(FlagOption.Flag14WriteEndOfPage, ctx.Start.Line,
                    "the WRITE without an END-OF-PAGE phrase (the file has a LINAGE clause)");
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-02 f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f) — a TERMINATE of a report whose description contains a
    //    VARYING clause (§13.18.64). Flagged once per TERMINATE when any named report carries a VARYING. ──
    public override object? VisitTerminateStatement(CobolParserCore.TerminateStatementContext ctx)
    {
        foreach (var report in ctx.reportName())
            if (_varyingReports.Contains(report.GetText()))
            {
                Flag(FlagOption.Flag02TerminateWithVarying, ctx.Start.Line,
                    "the TERMINATE of a report whose description contains a VARYING clause");
                break;
            }
        return base.VisitChildren(ctx);
    }

    /// <summary>The entry's PICTURE string, VALUE clause, and USAGE clause (each null when absent) — read once from
    /// the data-description clauses.</summary>
    private static (string? Picture, CobolParserCore.ValueClauseContext? Value, CobolParserCore.UsageClauseContext? Usage)
        Clauses(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var list = ctx.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
        string? pic = null;
        CobolParserCore.ValueClauseContext? value = null;
        CobolParserCore.UsageClauseContext? usage = null;
        if (list is not null)
            foreach (var c in list)
            {
                if (c.pictureClause()?.PIC_STRING() is { } ps) pic = ps.GetText();
                if (c.valueClause() is { } vc) value = vc;
                if (c.usageClause() is { } uc) usage = uc;
            }
        return (pic, value, usage);
    }

    /// <summary>Whether a PICTURE string classifies as <see cref="PicCategory.NumericEdited"/> via the ONE
    /// <see cref="PictureAnalyzer"/> (discard sink; §13.18.40). A custom CURRENCY SIGN symbol is not threaded — a
    /// numeric-edited picture using a non-default currency symbol is a rare false-negative, never a false-positive.</summary>
    private bool IsNumericEditedPicture(string picture)
        => PictureAnalyzer.Analyze(picture, Usage.Display, _discard, "a flagged VALUE clause").Category
            == PicCategory.NumericEdited;

    /// <summary>Whether an item with NO PICTURE has no length from its USAGE either: DISPLAY (explicit or absent —
    /// the default) has no length without a PICTURE, while every other usage (COMP-*, INDEX, POINTER family, the
    /// float/binary families, …) implies a fixed length. Detected from the usage keyword text.</summary>
    private static bool UsageGivesNoLength(CobolParserCore.UsageClauseContext? usage)
        => usage is null || usage.GetText().ToUpperInvariant().EndsWith("DISPLAY", StringComparison.Ordinal);

    /// <summary>A real data-item level (1–49 or the independent 77) — excludes 66 (RENAMES), 78 (CONSTANT), and
    /// 88 (condition-name), none of which is a length-bearing data item.</summary>
    private static bool IsRealDataLevel(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        int lvl = Level(ctx);
        return (lvl >= 1 && lvl <= 49) || lvl == 77;
    }

    private static int Level(CobolParserCore.DataDescriptionEntryContext ctx)
        => int.TryParse(ctx.levelNumber()?.GetText(), out int n) ? n : 0;

    /// <summary>Whether the entry is a GROUP item — the immediately-following sibling entry is a real subordinate
    /// data item (level 2–49, deeper than this entry). A following 66/88 entry is NOT a subordinate.</summary>
    private static bool HasSubordinates(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        if (NextEntry(ctx) is not { } next) return false;
        int nl = Level(next);
        return nl > Level(ctx) && nl is >= 2 and <= 49;
    }

    /// <summary>The next sibling <c>dataDescriptionEntry</c> in the same container (entries are a FLAT list — data
    /// nesting is by level number, not parse structure), or null.</summary>
    private static CobolParserCore.DataDescriptionEntryContext? NextEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        if (ctx.Parent is not { } parent) return null;
        bool found = false;
        for (int i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChild(i);
            if (found && child is CobolParserCore.DataDescriptionEntryContext next) return next;
            if (ReferenceEquals(child, ctx)) found = true;
        }
        return null;
    }

    /// <summary>Whether a numeric-edited VALUE clause is a plain LITERAL (numeric or nonnumeric — the caller has
    /// already excluded figurative constants) that contains NO editing symbols (j VALUE-EDITING). A numeric literal
    /// never carries editing symbols (flagged); a nonnumeric STRINGLIT/NATLIT is scanned for the unambiguous
    /// numeric-editing insertion characters. Only a '0'- or 'B'-only insertion (ambiguous with a digit / a letter)
    /// escapes the scan — a rare false-negative for this advisory flag. A concatenation / boolean / hex literal is
    /// not analyzed (not a numeric-editing value) and is not flagged.</summary>
    private static bool LiteralHasNoEditingSymbols(CobolParserCore.ValueClauseContext value)
    {
        if (FirstDescendant<CobolParserCore.NonNumericLiteralContext>(value) is { } nn)
        {
            string? text = nn.STRINGLIT()?.GetText() ?? nn.NATLIT()?.GetText();
            return text is not null && !ContainsEditingSymbol(StripLiteral(text));
        }
        return FirstDescendant<CobolParserCore.NumericLiteralContext>(value) is not null;
    }

    // The unambiguous numeric-editing INSERTION characters as they appear in an edited value literal (§13.18.40.3).
    // '0' (zero insertion) and 'B' (space insertion) are omitted — indistinguishable from a digit / a letter in the
    // literal text without re-deriving the picture mask.
    private static readonly char[] EditingChars = [' ', '/', ',', '.', '+', '-', '$', '*'];

    private static bool ContainsEditingSymbol(string content)
    {
        if (content.IndexOfAny(EditingChars) >= 0) return true;
        string trimmed = content.TrimEnd();   // the CR / DB trailing sign insertions
        return trimmed.EndsWith("CR", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("DB", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The content of a STRINGLIT / NATLIT token — a leading national <c>N</c> prefix and the surrounding
    /// quotes stripped — for the editing-symbol scan.</summary>
    private static string StripLiteral(string token)
    {
        string s = token.Length > 0 && token[0] is 'N' or 'n' ? token[1..] : token;
        return s.Replace("\"", "").Replace("'", "");
    }

    /// <summary>The first descendant of type <typeparamref name="T"/> in <paramref name="node"/>'s subtree (pre-order),
    /// or null. A small generic walk — the flag detectors reach into a construct's operand subtree without threading
    /// the exact (edition-varying) grammar path.</summary>
    private static T? FirstDescendant<T>(Antlr4.Runtime.Tree.IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T hit) return hit;
            if (FirstDescendant<T>(child) is { } deeper) return deeper;
        }
        return null;
    }
}
