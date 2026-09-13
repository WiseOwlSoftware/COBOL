// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO INVARIANTS THAT KEEP THE INITIALIZE EXPANSION AUTOMATIC (kb/Work PB418, CLAUDE.md rule 5).
/// <para>The INITIALIZE lane expands ISO §14.9.20.4 GR4's "series of implicit MOVE or SET statements" at BIND
/// time, so two things about it can rot silently, and both did.</para>
/// <para><b>1. The VALUE carrier.</b> A receiver's sending-operand under the VALUE phrase (GR6a3) and its
/// qualification under GR5c1b/GR5c1c are the SAME question — "what does this item's VALUE clause give THIS
/// occurrence" — and <c>DataItem.ValueAt</c> is the one reader that answers it across both carriers (Format 1's
/// <c>RawValue</c> and Format 2's <c>TableValuePlan</c>). The binder used to read <c>RawValue</c> directly, so
/// the Format-2 carrier was invisible: <c>ALL TO VALUE</c> emitted no store for a table element and
/// <c>ALL TO VALUE THEN TO DEFAULT</c> wrote SPACES over the values the statement exists to restore. A THIRD
/// carrier would repeat that exactly, and nothing would fail.</para>
/// <para><b>2. The action vocabulary.</b> <c>InitializeEmitter.EmitAction</c> switches over
/// <see cref="T:CobolNet.Binding.Bound.InitializeAction"/> with NO default arm — an action the binder composes
/// and the emitter does not know is dropped WITHOUT a diagnostic, and the store the standard requires simply
/// never happens. The vocabulary grew (PB393's loop, PB418's per-occurrence select) and will grow again.</para>
/// </summary>
public sealed class InitializeLaneDriftTests
{
    private static readonly string BinderPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "InitializeBinder.cs");

    private static readonly string BoundPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "BoundInitialize.cs");

    private static readonly string EmitterPath =
        TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "InitializeEmitter.cs");

    /// <summary>The file's code with every comment removed — a doc comment naming a symbol is documentation, not
    /// a read of it, and this file's subjects are named in several of them.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    /// <summary>ISO §14.9.20.4 GR5c1b/GR5c1c and GR6a3 are answered for BOTH VALUE carriers by
    /// <c>DataItem.ValueAt</c>, so the INITIALIZE binder must never reach past it to a carrier property. Reading
    /// one directly is how the Format-2 carrier went unseen (kb/Work PB418 / PB499); it is also how a third one
    /// would.</summary>
    [Fact]
    public void InitializeBinder_ReadsValueOnlyThroughTheOneCarrierAgnosticReader()
    {
        string code = CodeOf(BinderPath);

        Assert.True(code.Contains(".ValueAt(", StringComparison.Ordinal),
            "InitializeBinder no longer calls DataItem.ValueAt — §14.9.20.4 GR6a3's sending operand has to come "
            + "from the one carrier-agnostic reader, or a VALUE carrier it does not know about is silently lost.");

        // TableValuePlan is the Format-2 carrier the per-occurrence arm legitimately walks (GR5c1c needs the
        // occurrence tuples themselves, which no scalar reader can give it). RawValue and TableValues are not:
        // they are the raw carriers ValueAt exists to hide.
        foreach (string carrier in new[] { ".RawValue", ".TableValues" })
            Assert.False(code.Contains(carrier, StringComparison.Ordinal),
                $"InitializeBinder reads DataItem{carrier} directly. Ask DataItem.ValueAt(subscripts) instead — "
                + "it is the ONE reader of \"what initializes this item at this occurrence\" and the reason the "
                + "declaration lane and the INITIALIZE lane cannot disagree (§14.9.20.4 GR6a3).");
    }

    /// <summary>Every <c>InitializeAction</c> the bound tree declares has an arm in the emitter. The emitter's
    /// switch has no default, so a missing arm is a store that the standard requires and the program never
    /// performs — with a green build and a green gate.</summary>
    [Fact]
    public void EveryInitializeAction_HasAnEmitterArm()
    {
        string emitter = CodeOf(EmitterPath);
        var missing = new List<string>();
        foreach (string action in DeclaredActions())
            if (!Regex.IsMatch(emitter, $@"case\s+{Regex.Escape(action)}\b"))
                missing.Add(action);

        Assert.True(missing.Count == 0,
            "InitializeEmitter.EmitAction has no arm for: " + string.Join(", ", missing)
            + ". Its switch carries no default, so the action is dropped silently and the implicit MOVE/SET "
            + "§14.9.20.4 GR4 requires never happens. Add the arm, and the matching visit in "
            + "UsageCollectionPass.InitAct and BoundStores.InitStores.");
    }

    /// <summary>The action records declared in <c>BoundInitialize.cs</c> — read from the source rather than by
    /// reflection so the test names the file a contributor has to edit, and so a record added but not yet
    /// referenced anywhere still counts.</summary>
    private static IEnumerable<string> DeclaredActions() =>
        Regex.Matches(CodeOf(BoundPath), @"record\s+(?<name>\w+)\s*\([^)]*\)\s*:\s*InitializeAction\b")
            .Select(m => m.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>The scan above is only evidence if it can see anything at all — a regex that matches nothing
    /// makes the previous test a silent green (feedback_green_gates_arent_evidence).</summary>
    [Fact]
    public void TheActionScan_FindsTheActionsThatExist()
    {
        var actions = DeclaredActions().ToList();
        Assert.True(actions.Count >= 5,
            "the InitializeAction scan found only " + actions.Count + " record(s) in BoundInitialize.cs ("
            + string.Join(", ", actions) + ") — the declaration shape changed and EveryInitializeAction_"
            + "HasAnEmitterArm is no longer looking at anything.");
        foreach (string expected in new[]
                 { "InitializeStore", "InitializeLoop", "InitializeSetNull", "InitializeErrorAction",
                   "InitializeOccurrenceSelect", "InitializeSetFrom" })
            Assert.Contains(expected, actions);
    }

    // ── 3. THE CATEGORY-NAME SET (kb/Work PB415) ──────────────────────────────────────────────────────────────
    //    ISO §14.9.20.2's figure lists THIRTEEN category names and §5.2.6.4 makes a category-name one or more of
    //    them. The grammar carried FIVE for years behind a comment claiming the rest "require lexer tokens not
    //    yet defined" — stale the day three of those tokens landed for other reasons, and invisible because
    //    nothing compared the two lists. These three facts make the FOURTEENTH word automatic instead: the
    //    grammar rule, the bound enum and the per-word edition band all have to agree, with reserved-words.json
    //    (which the §8.9 funnel already owns) as the edition authority.

    private static readonly string GrammarPath =
        TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolData.g4");

    private static readonly string PassPath =
        TestRepo.Src("Cobol.Net.Compiler", "Validation", "VersionConformancePass.cs");

    /// <summary>The grammar's <c>initializeCategoryName</c> alternatives, as COBOL words.</summary>
    private static List<string> GrammarCategoryWords()
    {
        string g4 = File.ReadAllText(GrammarPath);
        var m = Regex.Match(g4, @"^initializeCategoryName\s*\r?\n(?<body>.*?)^\s*;\s*$",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "no initializeCategoryName rule in CobolData.g4 — ISO §14.9.20.2's category-name "
            + "list moved and this whole section is measuring nothing.");
        return Regex.Matches(m.Groups["body"].Value, @"^\s*[:|]\s*(?<tok>[A-Z][A-Z0-9_]*)\s*$",
                RegexOptions.Multiline)
            .Select(x => x.Groups["tok"].Value.Replace('_', '-'))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>ISO §14.9.20.2 prints THIRTEEN category names, the grammar shall spell all thirteen and nothing
    /// else, and <c>InitializeCategory</c> shall carry one member per word. A word the grammar cannot spell is
    /// legal source the compiler refuses; a member the grammar has no word for is a dead decoder arm.</summary>
    [Fact]
    public void TheThirteenCategoryNames_AreSpelledByTheGrammarAndCarriedByTheEnum()
    {
        // The printed list, transcribed from the rendered figure (licensed PDF p667 / folio 637) — the ONE
        // hand-written thing here, and the thing the other two artifacts are measured against.
        string[] printed =
        [
            "ALPHABETIC", "ALPHANUMERIC", "ALPHANUMERIC-EDITED", "BOOLEAN", "DATA-POINTER", "FUNCTION-POINTER",
            "MESSAGE-TAG", "NATIONAL", "NATIONAL-EDITED", "NUMERIC", "NUMERIC-EDITED", "OBJECT-REFERENCE",
            "PROGRAM-POINTER",
        ];
        Assert.Equal(13, printed.Length);

        Assert.Equal(printed.OrderBy(x => x, StringComparer.Ordinal).ToList(), GrammarCategoryWords());

        var members = Enum.GetValues<CobolNet.Binding.Bound.InitializeCategory>();
        Assert.Equal(printed.Length, members.Length);
        Assert.Equal(
            printed.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            members.Select(CobolNet.Binding.Bound.InitializeCategories.Spelling)
                .OrderBy(x => x, StringComparer.Ordinal).ToList());

        // InitializeCategorySet is a bitmask over the ordinals; more than 31 members would silently drop one.
        Assert.True(members.Length < 32,
            "InitializeCategory has " + members.Length + " members and InitializeCategorySet packs them into an "
            + "int — widen the mask before adding the 32nd.");
    }

    /// <summary>Each post-85 category-name is gated at the edition ISO §8.9 reserves its WORD, and that edition
    /// is read from <c>reserved-words.json</c> — the same table the §8.9 funnel and <c>reservedHere()</c> read.
    /// The three <c>initialize-category-*</c> construct rows are the bands; this pins that no word is banded
    /// against a different edition from the one that made its word reserved, and that the five COBOL-85 words
    /// are gated by nothing.</summary>
    [Fact]
    public void EveryCategoryName_IsGatedAtItsOwnReservationEdition()
    {
        string pass = CodeOf(PassPath);
        var m = Regex.Match(pass, @"VisitInitializeCategoryName\b.*?return base\.VisitChildren",
            RegexOptions.Singleline);
        Assert.True(m.Success, "VersionConformancePass has no VisitInitializeCategoryName — the per-word edition "
            + "gate for ISO §14.9.20.2's category-name list is gone.");
        string arm = m.Value;

        var bandOf = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (marker, year) in new[]
                 { ("InitializeCategory2002", 2002), ("InitializeCategory2014", 2014),
                   ("InitializeCategory2023", 2023) })
        {
            int at = arm.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(at >= 0, $"VisitInitializeCategoryName no longer references Constructs.{marker}");
            // The arm is ONE ordered conditional expression, so every token tested before a band's construct id
            // and not already claimed by an earlier band belongs to that band.
            foreach (Match w in Regex.Matches(arm[..at], @"ctx\.(?<tok>[A-Z][A-Z0-9_]*)\(\)"))
                bandOf.TryAdd(w.Groups["tok"].Value.Replace('_', '-'), year);
        }

        var reserved = ReservedWordIntroductions();
        foreach (string word in GrammarCategoryWords())
        {
            int introduced = reserved[word];
            if (introduced <= 85)
            {
                Assert.False(bandOf.ContainsKey(word),
                    $"{word} is reserved at COBOL-85 (reserved-words.json r85=true) and is one of ISO "
                    + "§14.9.20.2's five classic category names — gating it rejects conforming COBOL-85 source.");
                continue;
            }
            Assert.True(bandOf.TryGetValue(word, out int banded),
                $"{word} is an ISO §14.9.20.2 category-name that §8.9 reserves only from COBOL-{introduced}, and "
                + "VisitInitializeCategoryName gates it at no edition — it would be accepted below its own "
                + "introduction, which is the per-edition hole the VERSION TEST MATRIX exists to close.");
            Assert.True(banded == introduced,
                $"{word} is gated at COBOL-{banded} but §8.9 reserves it from COBOL-{introduced} "
                + "(reserved-words.json). The word and its category entered together; band it with its word.");
        }
    }

    /// <summary>reserved-words.json's per-word introduction edition (85 when reserved since '85).</summary>
    private static Dictionary<string, int> ReservedWordIntroductions()
    {
        string path = Path.Combine(TestRepo.Root, "tests", "version-matrix", "reserved-words.json");
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var e in doc.RootElement.GetProperty("words").EnumerateArray())
        {
            string w = e.GetProperty("word").GetString()!;
            map[w] = e.GetProperty("r85").GetBoolean() ? 85
                : e.GetProperty("r2002").GetBoolean() ? 2002
                : e.GetProperty("r2014").GetBoolean() ? 2014 : 2023;
        }
        return map;
    }

    /// <summary>⛔ THE PROBE'S OWN LIVENESS (feedback_green_gates_arent_evidence). The band scan above is a regex
    /// over source; if it found no words at all every assertion in it would vacuously pass. Assert the shape it
    /// actually extracted, and that the reserved-words table answers for every printed category name.</summary>
    [Fact]
    public void TheCategoryScans_FindWhatTheyClaimToMeasure()
    {
        var words = GrammarCategoryWords();
        Assert.Equal(13, words.Count);
        var reserved = ReservedWordIntroductions();
        foreach (string w in words)
            Assert.True(reserved.ContainsKey(w),
                $"reserved-words.json has no row for the category-name {w}; the edition band cannot be derived "
                + "and EveryCategoryName_IsGatedAtItsOwnReservationEdition would skip it.");
        Assert.Equal(5, words.Count(w => reserved[w] <= 85));
    }

    // ── 4. THE FOUR §14.9.20.3 OPERAND SCREENS (kb/Work PB416) ────────────────────────────────────────────────
    //    SR1 (identifier-1's class), SR4 (the implied MOVE's validity), SR5 (no RENAMES on identifier-1) and SR7
    //    (identifier-1 IS the receiving operand) were ALL unasked at the one place identifier-1 and its REPLACING
    //    operands are resolved. Two of the four repairs are STRUCTURAL rather than local — SR7 is a routing
    //    through the ONE receiving chokepoint and SR4 is a call to the ONE move-validity screen — and the whole
    //    value of that shape is that the NEXT prohibition and the NEXT category word are automatic. These three
    //    facts are what keeps "automatic" true.

    /// <summary>⛔ ISO §14.9.20.3 SR7 IS A FUNNEL, NOT A CHECK. "The data item referenced by identifier-1 is the
    /// receiving operand", so identifier-1 is subject to every receiving-operand prohibition the standard states
    /// — a growing set, already six deep in <c>ExpressionBinder.ResolveReceiving</c> (LINE-COUNTER §8.4.3.15.3
    /// SR3, a constant-name §13.10.4 GR1, PAGE-COUNTER, an unimplemented receiver shape, the OCCURS DYNAMIC
    /// CAPACITY register §13.18.38 SR30–32, and a CONSTANT RECORD §13.18.15.3 SR2). INITIALIZE used to resolve
    /// identifier-1 with the plain reference resolver, which answers only "where does this live", so NONE of them
    /// was asked and a structured constant was destroyed at run time in silence. Routing is what makes the
    /// seventh prohibition free; a re-introduced direct resolve would make it a seventh copy.</summary>
    [Fact]
    public void InitializeBinder_ResolvesIdentifier1ThroughTheOneReceivingChokepoint()
    {
        string code = CodeOf(BinderPath);

        Assert.True(code.Contains("ResolveReceiving(", StringComparison.Ordinal),
            "InitializeBinder no longer calls ExpressionBinder.ResolveReceiving. ISO §14.9.20.3 SR7 makes "
            + "identifier-1 the RECEIVING operand, and the receiving-operand prohibitions live behind that one "
            + "chokepoint — resolving identifier-1 any other way silently opts out of all of them (kb/Work "
            + "PB416: a CONSTANT RECORD was overwritten at run time with no diagnostic).");

        Assert.False(Regex.IsMatch(code, @"Refs\.Resolve\(\s*dref\s*\)"),
            "InitializeBinder resolves identifier-1's own dataReference through ctx.Refs.Resolve again. That is "
            + "the sending-side resolver: it answers where the item lives and screens nothing. identifier-1 is a "
            + "receiving operand (ISO §14.9.20.3 SR7) and shall go through host.Expr.ResolveReceiving.");
    }

    /// <summary>⛔ ISO §14.9.20.3 SR4's MOVE half IS ASKED OF THE ONE TABLE-16 SCREEN. "a MOVE statement with
    /// identifier-2 or literal-1 as the sending item and an item of the specified category as the receiving
    /// operand shall be valid" is the same question <c>MoveBinder</c> asks of <c>MoveTable16</c>, and
    /// §14.9.20.4 GR4 ("a series of implicit MOVE or SET statements") is what makes it INITIALIZE's question
    /// too. A private re-implementation here is how the implicit MOVE and the explicit one come to disagree —
    /// which is exactly what happened while INITIALIZE was not among <c>MoveTable16.Refusal</c>'s call sites and
    /// <c>InitializeEmitter</c> synthesised its <c>BoundMove</c> past <c>MoveBinder</c> entirely.</summary>
    [Fact]
    public void InitializeBinder_AsksTheOneMoveValidityScreen()
    {
        string code = CodeOf(BinderPath);
        foreach (string member in new[] { "MoveTable16.SenderPosition", "MoveTable16.Refusal",
                                          "MoveTable16.ShapeRefusal", "MoveTable16.SenderClassRefusal" })
            Assert.True(code.Contains(member, StringComparison.Ordinal),
                $"InitializeBinder no longer calls {member}. ISO §14.9.20.3 SR4 makes the validity of the "
                + "implicit MOVE a condition on the REPLACING phrase, and MoveTable16 is the ONE place that "
                + "answers it — for MOVE, for the INVOKE argument crossing (§14.8.2.3.3 rule 2d) and for this.");
    }

    /// <summary>⛔ EVERY CATEGORY-NAME EITHER TAKES THE SET FORM OR HAS A TABLE-16 RECEIVING POSITION — never
    /// neither, never both. ISO §14.9.20.3 SR4 is written in two paragraphs over exactly that partition: the
    /// first governs "data-pointer, function-pointer, message-tag, object-reference, and program-pointer" and
    /// asks about a SET statement, the second governs "each of the other categories" and asks about a MOVE. A
    /// category-name with neither answer is a REPLACING pair no rule screens; one with both is two rules
    /// answering for one word. This is the fact that makes the fourteenth category name automatic: adding the
    /// enum member without deciding which paragraph governs it fails HERE.</summary>
    [Fact]
    public void EveryCategoryName_IsScreenedByExactlyOneHalfOfSyntaxRule4()
    {
        foreach (var cat in CobolNet.Binding.Bound.InitializeCategories.All)
        {
            bool set = CobolNet.Binding.Bound.InitializeCategories.IsSetForm(cat);
            bool move = CobolNet.Binding.Bound.InitializeCategories.Table16Receiver(cat) is not null;
            Assert.True(set ^ move,
                $"category-name {CobolNet.Binding.Bound.InitializeCategories.Spelling(cat)} is "
                + (set ? "BOTH a SET-form category and carries a Table-16 receiving position — two halves of "
                       + "ISO §14.9.20.3 SR4 would screen the same REPLACING pair"
                       : "screened by NEITHER half of ISO §14.9.20.3 SR4: it is not one of GR4's five SET-form "
                       + "categories and InitializeCategories.Table16Receiver gives it no receiving position, "
                       + "so `INITIALIZE … REPLACING <it> BY <anything>` is accepted unscreened."));
        }
    }
}
