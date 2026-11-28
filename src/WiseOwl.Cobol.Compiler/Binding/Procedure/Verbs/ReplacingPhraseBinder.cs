// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>INITIALIZE's REPLACING phrase (ISO §2023:14.9.20.2; kb/Work PB2344, docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3): each REPLACING item's category-name and sending operand, screened by §2023:14.9.20.3 SR3, SR4 (its SET half and
/// its MOVE half) and SR6, bound to the phrase node <see cref="InitializeReplacingItem"/> the statement carries. It also
/// decodes the VALUE phrase's category-name, the same grammar rule (<c>initializeCategory</c>) under §2023:5.2.6.4's
/// "only once" alone. <see cref="InitializeBinder"/> binds the statement and walks the receivers; the categories an
/// item belongs to are <see cref="InitializeCategories"/>' rows.</summary>
internal sealed class ReplacingPhraseBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind the REPLACING phrase: one <see cref="InitializeReplacingItem"/> per item whose category-name names a
    /// category not already named (an item whose every word repeats is reported and dropped).</summary>
    public IReadOnlyList<InitializeReplacingItem> Bind(Core.InitializeReplacingPhraseContext replacing)
    {
        var replacements = new List<InitializeReplacingItem>();
        // §2023:14.9.20.3 SR6 "the same category shall not be repeated in a REPLACING phrase" and §2023:5.2.6.4's "any
        // single alternative shall be specified only once" are ONE rule over ONE accumulator: the union of every
        // category already named in this phrase, whether by an earlier item or by an earlier word of THIS item.
        var namedSoFar = default(InitializeCategorySet);
        foreach (var item in replacing.initializeReplacingItem())
        {
            var cats = CategorySetOf(item.initializeCategory(), ref namedSoFar);
            if (cats.IsEmpty) continue;   // every word was a repeat — reported by the pure check
            // §2023:14.9.20.3 SR4 makes identifier-2 "the SENDING item" of a MOVE, so it binds as MOVE's sending
            // operand does, function-identifier included (§2023:8.4.3.1.2 Format 1; §2023:8.4.3.2.3 SR1 bars one only from
            // a RECEIVING operand; fix-queue PB10) — with ONE difference on the literal slot: §2023:8.4.3.10.3 SR1a/SR1b
            // lets NULL "be used only as a sending operand in an INITIALIZE … statement", and NULL is identifier-2
            // (§2023:8.4.3.1.2 Format 8) carried on that slot.
            var send = item.sendingOperand();
            var lit = send?.literal();
            BoundOperand value = lit is not null ? host.Expr.NullAdmittingOperand(lit)
                : send is not null ? host.Move.SendingOperand(send, "INITIALIZE REPLACING sending operand")
                : BoundOperandError.Refused(ctx.Edition, "INITIALIZE REPLACING sending operand");
            // ISO §2023:14.9.20.3 SR3 — "for each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE,
            // or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2
            // shall be specified". literal-1 is what the rule excludes, and it excludes it because GR4 makes
            // the implicit statement a SET for exactly these five categories and no literal is a SET sending
            // operand (§2023:14.9.39). Unreachable until the words became spellable (kb/Work PB415).
            string senderText = WrittenSource.Of(
                lit ?? send?.dataReference() ?? (Antlr4.Runtime.ParserRuleContext?)send?.functionCall(), "the sending operand");
            // NULL is identifier-2, not literal-1 (kb/Work PB1427): SR3 is satisfied, and SR4's SET is valid for
            // every SET-form category (CheckSetFormCategoryAgreement), so only a real literal takes SR3.
            if (lit is not null && value is not BoundPredefinedNull)
                ctx.Validation.CheckInitializeReplacingSetCategoryIdentifier(cats, lit.GetText());
            else
                CheckSetFormCategoryAgreement(cats, value, senderText);
            // ISO §2023:14.9.20.3 SR4's SECOND paragraph — the MOVE half, for every category-name that is not one
            // of GR4's five SET-form ones. It is asked HERE, over the phrase, because the rule's receiving
            // operand is "an item of the specified category" and not any item identifier-1 happens to
            // contain: the pair is legal or illegal before a single receiver has been walked (kb/Work PB416).
            CheckReplacingMoveValidity(cats, value, senderText);
            replacements.Add(new InitializeReplacingItem(cats, value));
        }
        return replacements;
    }

    /// <summary>ISO §2023:14.9.20.3 SR4 for the SET-form categories — "for each of the categories data-pointer,
    /// function-pointer, message-tag, object-reference, and program-pointer specified in the REPLACING phrase, a
    /// SET statement with identifier-2 as the sending operand and an item of the specified category as the
    /// receiving operand shall be valid". §2023:14.9.39's pointer and object-reference formats admit only a sending
    /// operand of the receiver's own category, so category agreement is the NECESSARY condition, checked here
    /// (the receiver-specific rules — a restricted pointer's type or signature, an object reference's class — are
    /// asked per RECEIVING OPERAND by <c>SetPointerBinder.ScreenImplicitSet</c>, at the store; kb/Work PB3817).
    /// <para>It is checked at BIND time rather than left to the backend because <see cref="InitializeSetFrom"/>
    /// renders the SET statement's own straight copy: a mismatched sender would otherwise reach Roslyn as a
    /// CS-level type error on emitted code, which names no COBOL rule.</para></summary>
    private void CheckSetFormCategoryAgreement(InitializeCategorySet cats, BoundOperand value, string senderText)
    {
        foreach (var cat in InitializeCategories.All)
        {
            if (!cats.Contains(cat) || !InitializeCategories.IsSetForm(cat)) continue;
            // The predefined NULL is a valid SET sending operand for each of the five categories: §2023:14.9.20.4 GR6's
            // table gives it as the default of every one (the address NULL of §2023:8.4.3.10.4 GR1–GR4, the null object
            // reference of §2023:8.4.3.7), so `SET <an item of the category> TO NULL` is valid, which is SR4's question.
            if (value is BoundPredefinedNull) continue;
            var senderCat = value is BoundFieldOperand { Place.Item: { } si } ? InitializeCategories.InitializeItemCategory(si) : null;
            if (senderCat != cat)
                ctx.Validation.CheckInitializeReplacingSetCategoryAgrees(cat, senderCat, senderText);
        }
    }

    /// <summary>⛔ ISO §2023:14.9.20.3 SR4's MOVE HALF — "For each of the other categories specified in the REPLACING
    /// phrase, a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the specified
    /// category as the receiving operand shall be valid" — the twin of
    /// <see cref="CheckSetFormCategoryAgreement"/>, which answers SR4's FIRST paragraph for GR4's five SET-form
    /// categories. §2023:14.9.20.4 GR4's "as though a series of implicit MOVE or SET statements" is what makes a rule
    /// about MOVE a rule about INITIALIZE.
    /// <para>The question is asked of <see cref="MoveTable16"/>, the SAME screen <c>MoveBinder</c> asks, so the
    /// implicit MOVE and the explicit one the programmer could write instead cannot answer differently. Before
    /// this, <c>MoveTable16</c> had four call sites and INITIALIZE was none of them while
    /// <c>InitializeEmitter</c> synthesised its <c>BoundMove</c> directly — so every cell an explicit MOVE
    /// refuses, <c>INITIALIZE … REPLACING</c> accepted and STORED: a <c>PIC A(4)</c> item ended up holding
    /// <c>0000</c>, which no conforming program can produce and a later class-ALPHABETIC test then reads as
    /// false (kb/Work PB416).</para>
    /// <para>ONE message per REPLACING item, naming the first offending category — the same posture
    /// §2023:14.9.20.3 SR3's check takes, since one item's category-name may be a SET (§2023:5.2.6.4) and a reader fixing
    /// the phrase fixes it once.</para>
    /// <para>⚠ SR4's ONE EDITION-DEPENDENT MOVE RULE IS NOT ASKED HERE: §2023:14.9.25.3 SR5's figurative→numeric
    /// prohibition has three EDITION rows, which are the post-bind <c>VersionConformancePass</c>'s. The written
    /// REPLACING phrase rides <see cref="BoundInitialize.Replacing"/> and <c>VersionConformancePass.GateInitialize</c>
    /// asks the same rows <c>GateMove</c> asks, per category (kb/Work PB879) — so <c>INITIALIZE g REPLACING NUMERIC
    /// DATA BY HIGH-VALUE</c> is COBOL0902 at 2023 exactly as the explicit MOVE is, without a second, edition-blind
    /// copy of the rule here. SR5's SPACE arm IS asked here, through the chain below: it holds at every edition, so it
    /// is <c>MoveValidity.FigurativeRefusal</c>'s flat refusal, not an edition row (kb/Work PB3605).</para></summary>
    private void CheckReplacingMoveValidity(InitializeCategorySet cats, BoundOperand value, string senderText)
    {
        // ⛔ SR4'S SECOND PARAGRAPH IS WHAT THIS METHOD IMPLEMENTS, AND IT SAYS "the OTHER categories"
        // (kb/Work PB423). The MOVE question is not asked at all for the five categories SR4's FIRST paragraph
        // routes to a SET — data-pointer, function-pointer, message-tag, object-reference and program-pointer —
        // so a REPLACING phrase naming ONLY those has no implicit MOVE to be valid. The per-category loop below
        // already honoured that (Table16Receiver answers null for them); the SR1 CLASS screen did not, because
        // it was hoisted above the loop as "asked once" back when the only class it could refuse was INDEX, and
        // INDEX is not one of INITIALIZE's thirteen category-names. The moment SR1's screen gained its pointer
        // and object arms, `INITIALIZE g REPLACING DATA-POINTER DATA BY PTR` — the exact shape SR4's first
        // paragraph and §2023:14.9.20.4 GR4 describe, and which conformance:cases/14.9.20-INITIALIZE/pb415_initialize_replacing_set_form
        // pins — was refused as an invalid MOVE. The hoist stays (one message per REPLACING item, §2023:5.2.6.4's
        // category-name is a SET), guarded by the paragraph that owns it.
        bool anyMoveFormCategory = false;
        foreach (var cat in InitializeCategories.All)
            if (cats.Contains(cat) && !InitializeCategories.IsSetForm(cat)) { anyMoveFormCategory = true; break; }
        if (anyMoveFormCategory && MoveTable16.SenderClassRefusal(value, ctx.Edition.Edition) is { } classRefusal)
        {
            ctx.Edition.Error(DiagnosticCatalog.InitializeReplacingMoveInvalid,
                $"INITIALIZE REPLACING BY {senderText}: {ctx.Edition.Edition.CiteRule("§1985:VI.6.17.3 SR2", "§2023:14.9.20.3 SR4")} requires the implicit MOVE to be "
                + $"valid, and {classRefusal}");
            return;
        }
        foreach (var cat in InitializeCategories.All)
        {
            if (!cats.Contains(cat) || InitializeCategories.Table16Receiver(cat) is not { } recvPos) continue;
            // ⛔ THE WHOLE CHAIN, NOT A HAND-PICKED SUBSET (kb/Work PB878). This asked SR8 and SR10 and never
            // SR9, so `INITIALIZE G REPLACING ALPHANUMERIC DATA BY VG` over a variable-length group VG compiled
            // clean and aborted the run unit. The receiving operand of SR4's MOVE is "an item of the specified
            // category" — an ELEMENTARY item with a Table-16 position and no data item, which SR9's reader
            // reads (null receiver) as "not a group", i.e. a violation when the sender is a variable-length group.
            if (MoveTable16.Validity(value, recvPos, receiverItem: null, MoveTable16.CitationsOf(ctx.Edition.Edition))?.Reason is not { } refusal) continue;
            ctx.Edition.Error(DiagnosticCatalog.InitializeReplacingMoveInvalid,
                $"INITIALIZE REPLACING {InitializeCategories.Spelling(cat)} DATA BY {senderText}: {ctx.Edition.Edition.CiteRule("§1985:VI.6.17.3 SR2", "§2023:14.9.20.3 SR4")} "
                + $"requires that `MOVE {senderText} TO <an item of category "
                + $"{InitializeCategories.Spelling(cat)}>` be valid, and {refusal}");
            return;   // one message per REPLACING item, naming the first offending category
        }
    }

    /// <summary>Decode ONE grammar <c>category-name</c> — the §2023:5.2.6.4 SET of category words, accumulated left to
    /// right. <paramref name="namedSoFar"/> is the running union that §2023:14.9.20.3 SR6 ("the same category shall not
    /// be repeated in a REPLACING phrase") and §2023:5.2.6.4 ("any single alternative shall be specified only once")
    /// BOTH test against — one accumulator for one rule, so a word repeated within a category-name and a category
    /// repeated across REPLACING items are diagnosed identically. A repeated word is dropped, never doubled.</summary>
    private InitializeCategorySet CategorySetOf(Core.InitializeCategoryContext cat, ref InitializeCategorySet namedSoFar)
    {
        var set = default(InitializeCategorySet);
        foreach (var name in cat.initializeCategoryName())
        {
            var one = InitializeCategoryOf(name);
            if (!ctx.Validation.CheckInitializeCategoryUnique(namedSoFar, one)) continue;
            set = set.With(one);
            namedSoFar = namedSoFar.With(one);
        }
        return set;
    }

    /// <summary>The VALUE phrase's category-name (ISO §2023:14.9.20.2). Its words are governed by §2023:5.2.6.4's "only
    /// once" too, but by nothing else — SR6 is written about the REPLACING phrase alone — so the accumulator is
    /// local to this one category-name.</summary>
    internal InitializeCategorySet CategorySetOf(Core.InitializeCategoryContext cat)
    {
        var namedSoFar = default(InitializeCategorySet);
        return CategorySetOf(cat, ref namedSoFar);
    }

    /// <summary>Decode ONE printed category-name word (ISO §2023:14.9.20.2 "where category-name is:"). Every one of
    /// the thirteen is a required word with one hyphenated spelling, so this is a total token switch — the
    /// two-token <c>ALPHANUMERIC EDITED</c> / <c>NUMERIC EDITED</c> forms the rule used to carry are in no
    /// edition of the standard and are gone with the EDITED lexer token (kb/Work PB415).</summary>
    private static InitializeCategory InitializeCategoryOf(Core.InitializeCategoryNameContext cat) =>
        cat.ALPHABETIC() is not null ? InitializeCategory.Alphabetic
        : cat.ALPHANUMERIC_EDITED() is not null ? InitializeCategory.AlphanumericEdited
        : cat.ALPHANUMERIC() is not null ? InitializeCategory.Alphanumeric
        : cat.BOOLEAN() is not null ? InitializeCategory.Boolean
        : cat.DATA_POINTER() is not null ? InitializeCategory.DataPointer
        : cat.FUNCTION_POINTER() is not null ? InitializeCategory.FunctionPointer
        : cat.MESSAGE_TAG() is not null ? InitializeCategory.MessageTag
        : cat.NATIONAL_EDITED() is not null ? InitializeCategory.NationalEdited
        : cat.NATIONAL() is not null ? InitializeCategory.National
        : cat.NUMERIC_EDITED() is not null ? InitializeCategory.NumericEdited
        : cat.NUMERIC() is not null ? InitializeCategory.Numeric
        : cat.OBJECT_REFERENCE() is not null ? InitializeCategory.ObjectReference
        : InitializeCategory.ProgramPointer;
}
