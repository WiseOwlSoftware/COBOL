// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE §13.18.60.4 GR1 EQUIVALENCE, over the WHOLE <see cref="Usage"/> enum (kb/Work PB495).
///
/// <para>GR1: <i>"If the USAGE clause is specified or implied at a group level, it applies only to each
/// elementary item in the group."</i> It does not say the group's clause INFLUENCES its leaves; it says the
/// clause APPLIES TO each of them. So for every usage the compiler admits and every picture a programmer can
/// write beside it, these two spellings describe THE SAME ITEM:</para>
/// <code>
///     01 G USAGE u.  05 A ⟨pic⟩.      ≡      01 G.  05 A ⟨pic⟩ USAGE u.
/// </code>
/// <para>— same verdict, same diagnostic code, same width. This test enumerates <see cref="Usage"/> and asserts
/// exactly that, so a usage added to the enum tomorrow is covered without anyone remembering to cover it.</para>
///
/// <para><b>Why an enumeration and not a list of cases.</b> Before PB495 the equivalence held for THREE usages.
/// The inheritance pass carried two hand-written sets — <c>ResolveIndexItems</c>' shed list and
/// <c>InheritUsageClauses</c>' <c>Binary or Packed or Comp5</c> — neither derived from anything, and every usage
/// in neither fell through SILENTLY: a picture-less leaf under a group-level BINARY-SHORT became a ZERO-LENGTH
/// item, a group-level FLOAT-SHORT emitted the group itself as a scalar <c>float</c>, and a PICTURE-bearing leaf
/// under a group-level BINARY-LONG kept its usage-display width with no diagnostic. Measured on 39479e19: of the
/// 138 (usage × picture × arm) cells this file's matrix covers, 108 DIFFERED between the two arms.</para>
///
/// <para><b>⛔ WHAT THIS FILE CANNOT CATCH.</b> The one hand-written table left is
/// <see cref="UsageFamilies.IsPictureless"/>, and it drives BOTH arms — which is the point of writing the set
/// down once, and which also means a WRONG ENTRY IN IT IS INVISIBLE HERE: both spellings move together and the
/// equivalence still holds. Measured while this file was written — dropping BINARY-SHORT from the set left every
/// test below green while `05 A PIC 9(4) USAGE BINARY-SHORT.` became legal on both arms. The independent oracle
/// is the standard's own sentence, and it lives in <c>PicturelessUsageSetDriftTests</c> (Unit), which re-reads
/// §13.16.3 SR8 out of <c>specs/ISO_COBOL.md</c>. Neither file is sufficient alone.</para>
/// </summary>
public sealed class UsageInheritanceDriftTests
{
    /// <summary>Every <see cref="Usage"/> member, spelled as a programmer would write it.</summary>
    public static TheoryData<Usage> AllUsages()
    {
        var d = new TheoryData<Usage>();
        foreach (Usage u in Enum.GetValues<Usage>()) d.Add(u);
        return d;
    }

    /// <summary>The pictures the §13.18.60.3 screens discriminate between: none, numeric, alphanumeric,
    /// national, boolean, numeric-edited. One representative per category the rules name.</summary>
    private static readonly string[] Pictures = ["", "PIC 9(4)", "PIC X(3)", "PIC N(2)", "PIC 1(3)", "PIC ZZ9"];

    /// <summary>The compile verdict, reduced to what GR1 makes comparable: the diagnostic CODES (order-
    /// insensitive, de-duplicated) and — for a clean compile — the item's width as the program itself reports it.
    /// Positions and message text legitimately differ (each names its own entry); the VERDICT may not.</summary>
    private static string Verdict(string source)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        string text = ok ? stdout : detail;
        var codes = Regex.Matches(text, @"COBOL(?:NET)?\d{4}").Select(m => m.Value).Distinct().Order().ToList();
        if (!ok && codes.Count == 0) codes.Add("FAIL-WITHOUT-A-CODE:" + text.Split('\n')[0].Trim());
        return codes.Count > 0 ? string.Join(",", codes) : "OK " + stdout.Trim();
    }

    /// <summary>Is this verdict one where the compiler refuses the USAGE KEYWORD ITSELF, before the item can
    /// acquire a <see cref="PicInfo"/> model at all? Two codes say so and they are ONE situation, not two:
    /// COBOLNET0899 (recognized but STAGED — today USAGE FUNCTION-POINTER, waiting on the P13 prototype
    /// registry) and COBOLNET1943 (recognized and DECLINED — USAGE MESSAGE-TAG, Annex A.3 item 4, kb/Work
    /// PB487). ⛔ A PREDICATE, deliberately, not a check written inline twice: the situation is what the
    /// carve-outs below are about, and the next refused keyword joins it here in one place.</summary>
    private static bool KeywordRefused(string verdict) =>
        verdict.Contains("COBOLNET0899") || verdict.Contains("COBOLNET1943");

    private static string Program(string id, string ws) =>
        $"""
         IDENTIFICATION DIVISION.
         PROGRAM-ID. {id}.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         {ws}
         PROCEDURE DIVISION.
             DISPLAY "LEN=" FUNCTION BYTE-LENGTH(A).
             STOP RUN.
         """;

    [Theory]
    [MemberData(nameof(AllUsages))]
    public void InheritedUsage_IsTheSameItemAsAWrittenOne(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        foreach (string pic in Pictures)
        {
            // Unique PROGRAM-IDs per cell — .NET serves a stale same-named assembly otherwise.
            string cell = tag + Regex.Replace(pic, "[^A-Z0-9]", "");
            string direct = Program($"UD{cell}", $"01 G.\n    05 A {pic} USAGE {word}.");
            string inherited = Program($"UI{cell}", $"01 G USAGE {word}.\n    05 A {pic}.");
            string dv = Verdict(direct), iv = Verdict(inherited);
            string label = $"{word} / {(pic.Length == 0 ? "(no picture)" : pic)}";

            // A usage whose KEYWORD the compiler refuses — STAGED (COBOLNET0899, USAGE FUNCTION-POINTER, whose
            // representation waits on the P13 prototype registry) or DECLINED (COBOLNET1943, USAGE MESSAGE-TAG,
            // Annex A.3 item 4) — has no PicInfo model of its own, so the item it describes does not exist on
            // EITHER arm and the codes past the refusal are not comparable: §13.18.60.3 SR14's elementary arm
            // keys on the resolved class, which such a usage has none of, so it fires on the group spelling
            // (from the WRITTEN clause) and not on the elementary one. That asymmetry belongs to the refusal,
            // not to GR1 — it is registered as kb/Work PB819, whose fix shape is arm B keying on the written or
            // inherited PHRASE, and DataBinder.UsageDeclaration's Sr14PhraseOf documents it at the site.
            // Assert what GR1 does require here: both spellings are refused.
            if (KeywordRefused(dv) || KeywordRefused(iv))
            {
                Assert.True(dv.StartsWith("COBOL") && iv.StartsWith("COBOL"),
                    $"{label}: a usage whose keyword is refused must be refused on BOTH spellings — direct {dv}, "
                    + $"inherited {iv}");
                continue;
            }
            Assert.Equal($"{label} → {dv}", $"{label} → {iv}");
        }
    }

    /// <summary>The set against the REPRESENTATION MACHINERY — not against the standard (that is
    /// <c>PicturelessUsageSetDriftTests</c>, and this one cannot substitute for it: the SR8 screen is driven by
    /// the same predicate, so the first assertion below is circular by construction and is kept only to pin the
    /// screen's presence). The SECOND assertion is not circular and is the one that earns its keep: a usage the
    /// set calls picture-less must have a synthesized profile in <c>BindEntry</c>'s picture-less chain, or
    /// <c>05 A USAGE u.</c> binds an item with NO PicInfo — the zero-length cell whose MOVE receiver crashed the
    /// emitter (kb/Work PB495). The table and that chain are two different hand-written places, and this is what
    /// holds them together.
    ///
    /// <para>Some members are excluded because the compiler refuses the KEYWORD before any picture rule is
    /// reached, so neither spelling is evidence about SR8: FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 are
    /// documented processor-dependent non-support (COBOLNET1564, Annex A.3 items 17/19), FUNCTION-POINTER stages
    /// loud pending the P13 prototype registry, and MESSAGE-TAG is declined non-support (COBOLNET1943, Annex A.3
    /// item 4, kb/Work PB487). They are named by PREDICATE — <see cref="KeywordRefused"/> and the 1564 test —
    /// never by list, so a member that later lands is tested automatically.</para>
    ///
    /// <para>The second sentence's arm is asserted only in the picture-less direction (a picture-less item of a
    /// picture-less usage compiles). Its converse — a picture-REQUIRING usage with no picture — is kb/Work
    /// PB504's open mechanism (§13.16.3 SR9's VALUE-implied picture is synthesized nowhere), so it is asserted
    /// as the CURRENT state with the note named, not as the rule.</para></summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void Sr8Set_MatchesWhatTheCompilerActuallyDoes(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        string withPicture = Verdict(Program($"S8P{tag}", $"01 G.\n    05 A PIC 9(4) USAGE {word}."));
        string without = Verdict(Program($"S8N{tag}", $"01 G.\n    05 A USAGE {word}."));

        // The keyword itself refused (documented non-support COBOLNET1564, or staged / declined — see
        // KeywordRefused): neither spelling is SR8 evidence, because no picture rule is ever reached.
        if (withPicture.Contains("COBOLNET1564") || KeywordRefused(withPicture)) return;
        // §13.18.60.3 SR14 refuses the five pointer/object phrases at level 05 whatever the picture — the
        // placement rule fires first, so again neither spelling is SR8 evidence here.
        if (withPicture.Contains("COBOLNET1724") && without.Contains("COBOLNET1724")) return;

        if (UsageFamilies.IsPictureless(usage))
        {
            Assert.True(withPicture.StartsWith("COBOL"),
                $"USAGE {word} is in UsageFamilies.IsPictureless, so ISO §13.16.3 SR8 forbids a PICTURE beside "
                + $"it — but `05 A PIC 9(4) USAGE {word}.` compiled: {withPicture}");
            Assert.True(without.StartsWith("OK"),
                $"USAGE {word} is picture-less, so `05 A USAGE {word}.` describes a complete item — got {without}");
        }
        else
        {
            Assert.True(withPicture.StartsWith("OK") || withPicture.Contains("COBOLNET0881"),
                $"USAGE {word} is NOT in UsageFamilies.IsPictureless, so ISO §13.16.3 SR8 REQUIRES a PICTURE "
                + $"beside it — `05 A PIC 9(4) USAGE {word}.` must be accepted, or refused by a §13.18.60.3 "
                + $"picture-category rule (COBOLNET0881), not by an SR8 prohibition: {withPicture}");
        }
    }

    /// <summary>The headline REPRESENTATION half, end to end and in one program: a PICTURE-less elementary item
    /// under a group that wrote a picture-less usage gets THAT USAGE'S width — the width §13.18.60.4 GR12/GR13
    /// and the implementor's Annex A.1 determination fix — where before PB495 it got a ZERO-LENGTH cell and a
    /// MOVE into it crashed the compiler (NullReferenceException in <c>MoveEmitter.ConvertSource</c>).</summary>
    [Fact]
    public void PicturelessLeafUnderAGroupUsage_TakesTheUsagesWidth()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495WIDTHS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GC USAGE BINARY-CHAR.
                05 AC.
            01 GS USAGE BINARY-SHORT.
                05 AS-I.
            01 GL USAGE BINARY-LONG.
                05 AL.
            01 GD USAGE BINARY-DOUBLE.
                05 AD.
            01 GF USAGE FLOAT-SHORT.
                05 AF.
            01 GG USAGE FLOAT-LONG.
                05 AG.
            01 GI USAGE INDEX.
                05 AI.
            PROCEDURE DIVISION.
                MOVE 12 TO AS-I.
                DISPLAY "C=" FUNCTION BYTE-LENGTH(AC)
                        " S=" FUNCTION BYTE-LENGTH(AS-I)
                        " L=" FUNCTION BYTE-LENGTH(AL)
                        " D=" FUNCTION BYTE-LENGTH(AD)
                        " F=" FUNCTION BYTE-LENGTH(AF)
                        " G=" FUNCTION BYTE-LENGTH(AG)
                        " I=" FUNCTION BYTE-LENGTH(AI)
                        " GRP=" FUNCTION BYTE-LENGTH(GS).
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // 1/2/4/8 for binary-char/-short/-long/-double (§13.18.60.4 GR12's minimum ranges over the
        // implementor's two's-complement widths, Annex A.1); 4/8 for float-short/float-long (GR13 + A.1);
        // 8 for the index item (§13.18.60.4 GR10, implementor-defined — the R40 pin). The GROUP is the width
        // of its one leaf, never zero and never a scalar of the usage's own type.
        Assert.Equal("C=1 S=2 L=4 D=8 F=4 G=8 I=8 GRP=2", stdout);
    }

    /// <summary>§13.18.60.3 SR2 — a subordinate entry may write a USAGE clause beside its group's, but "the same
    /// usage shall be specified in both entries". The rule had NO site at all before PB495: the nearer clause
    /// simply won and the outer one was discarded silently, changing the item's representation and the group's
    /// width from what either written clause asked for.</summary>
    [Fact]
    public void Sr2_ContradictingUsages_AreDiagnosed_AndTheAgreeingSpellingCompiles()
    {
        const string bad = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495SR2BAD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G USAGE COMPUTATIONAL.
                05 A PIC 9(4).
                05 B PIC 9(4) USAGE DISPLAY.
            PROCEDURE DIVISION.
                DISPLAY "X".
                STOP RUN.
            """;
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(bad);
        Assert.False(ok, "a USAGE DISPLAY leaf inside a USAGE COMPUTATIONAL group violates ISO §13.18.60.3 SR2");
        Assert.Contains("COBOLNET1927", detail);
        Assert.Contains("§13.18.60.3 SR2", detail);

        // SR6 makes COMP the abbreviation for COMPUTATIONAL, and this implementation identifies BINARY with
        // both (§13.18.60.4 GR4/GR6 are each implementor-defined) — so "the same usage" is satisfied and all
        // three leaves are the 2-byte binary item, group width 6.
        const string ok3 = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495SR2OK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G USAGE COMPUTATIONAL.
                05 A PIC 9(4).
                05 B PIC 9(4) USAGE COMP.
                05 C PIC 9(4) USAGE BINARY.
            PROCEDURE DIVISION.
                DISPLAY "A=" FUNCTION BYTE-LENGTH(A) " B=" FUNCTION BYTE-LENGTH(B)
                        " C=" FUNCTION BYTE-LENGTH(C) " G=" FUNCTION BYTE-LENGTH(G).
                STOP RUN.
            """;
        var (ok2, stdout, detail2) = new CobolNetCompiler(2023).CompileAndRun(ok3);
        Assert.True(ok2, detail2);
        Assert.Equal("A=2 B=2 C=2 G=6", stdout);
    }
}
