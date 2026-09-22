// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Common;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <b>ISO §13.16.3 SR9 — the VALUE-implied PICTURE</b> (kb/Work PB504; the <c>ALL literal-1</c> length is
/// kb/Work PB831; the figurative admission is the kb/Work PB828 adjudication).
///
/// <para><b>Every program here REFERENCES the item and COMPILES FOR REAL.</b> That is not incidental: the two
/// tests that stood green over this construct before — <c>FlagDirectiveTests</c>' two
/// <c>VALUE-FIG-CON-LENGTH</c> facts — compile with <c>CheckOnly: true</c> over a program that never names the
/// item, so they are structurally incapable of seeing either failure this rule actually had (a picture-less item
/// reaching the emitter with no field at all, and the one-character recovery item storing a truncated value).
/// A green that never looked at what changed is not evidence (<c>feedback_green_gates_arent_evidence</c>), so
/// every positive below asserts the VALUE that comes back and its <c>FUNCTION LENGTH</c>.</para>
/// </summary>
public sealed class ImpliedPictureTests : CobolNetTestBase
{
    /// <summary>A WORKING-STORAGE program whose entries are <paramref name="entries"/> and whose PROCEDURE
    /// DIVISION is <paramref name="body"/>. Unique PROGRAM-ID per test (a repeated one serves a stale assembly).</summary>
    private static string Program(string id, string entries, string body) =>
        "       IDENTIFICATION DIVISION.\n"
        + $"       PROGRAM-ID. {id}.\n"
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + entries
        + "       PROCEDURE DIVISION.\n"
        + "       MAIN-PARA.\n"
        + body
        + "           STOP RUN.\n";

    // ── SR9's three arms, each measured through the VALUE that comes back and the item's length ──────────────

    /// <summary>SR9 a) — "if the literal is alphanumeric, 'PICTURE X(length)'", the length being the literal's
    /// own (§8.3.3.2). Before kb/Work PB504 this program was REJECTED by the §13.16.3 SR8 closing guard, and
    /// before kb/Work PB487 the reference leaked a raw Roslyn <c>CS0103</c> to the user.</summary>
    [Fact]
    public void Sr9a_AlphanumericLiteral_ImpliesPictureXOfTheLiteralLength()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9ALNU", "       01 A VALUE \"HELLO\".\n",
                "           DISPLAY \"[\" A \"] \" FUNCTION LENGTH(A)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[HELLO] 5", stdout);
    }

    /// <summary>§13.16.3 SR9 a) again, on the HEXADECIMAL form of an alphanumeric literal (§8.3.3.2) — SR9's length is
    /// "the length of the literal as specified in 8.3.3, Literals", and §8.3.3.2.3 r6 groups the digits TWO per
    /// alphanumeric character, so <c>X"414243"</c> is three characters and implies <c>X(3)</c>, not six.</summary>
    [Fact]
    public void Sr9a_HexadecimalLiteral_LengthIsItsDecodedCharacters()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9HEX", "       01 A VALUE X\"414243\".\n",
                "           DISPLAY \"[\" A \"] \" FUNCTION LENGTH(A)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[ABC] 3", stdout);
    }

    /// <summary>§13.16.3 SR9 b) — "if the literal is boolean, 'PICTURE 1(length)'", the length being the count of
    /// boolean characters the literal has (§8.3.3.4).</summary>
    [Fact]
    public void Sr9b_BooleanLiteral_ImpliesPicture1OfTheLiteralLength()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9BOOL", "       01 A VALUE B\"1011\".\n",
                "           DISPLAY \"[\" A \"] \" FUNCTION LENGTH(A)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[1011] 4", stdout);
    }

    /// <summary>SR9 c) — "if the literal is national, 'PICTURE N(length)'". The implied clause is a PICTURE
    /// character-string like any other, so §13.18.60.3 SR13 a) then applies to it in the standard's own words —
    /// "if the explicit or IMPLICIT picture character-string contains the symbol 'N', a USAGE NATIONAL clause is
    /// implied" — which is why writing USAGE NATIONAL on the entry is redundant rather than contradictory.</summary>
    [Fact]
    public void Sr9c_NationalLiteral_ImpliesPictureN_WithOrWithoutTheUsageClause()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9NAT",
                "       01 A VALUE N\"HELLO\".\n       01 B USAGE NATIONAL VALUE N\"HI\".\n",
                "           DISPLAY \"[\" A \"] \" FUNCTION LENGTH(A)\n"
                + "           DISPLAY \"[\" B \"] \" FUNCTION LENGTH(B)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[HELLO] 5\r\n[HI] 2", stdout);
    }

    // ── The figurative constant: inside SR9's grant, with §8.3.3.6.4's lengths ────────────────────────────────

    /// <summary>§8.3.3.6.4 GR3 b) — "When a figurative constant is other than ALL literal-1, the length of the
    /// string is one character" — for both the bare word and the Format-2 <c>ALL SPACES</c> spelling, in which
    /// ALL is the format's own OPTIONAL word rather than Format 6's required one.</summary>
    [Fact]
    public void Sr9_FigurativeConstant_IsOneCharacter()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9FIG",
                "       01 A VALUE SPACE.\n       01 B VALUE ALL SPACES.\n       01 C VALUE ZERO.\n",
                "           DISPLAY \"[\" A \"]\" FUNCTION LENGTH(A)\n"
                + "           DISPLAY \"[\" B \"]\" FUNCTION LENGTH(B)\n"
                + "           DISPLAY \"[\" C \"]\" FUNCTION LENGTH(C)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[ ]1\r\n[ ]1\r\n[0]1", stdout);
    }

    /// <summary>⛔ kb/Work PB831, the SILENT WRONG ANSWER this rule's absence produced. §8.3.3.6.4 GR3 c) — "The
    /// length of the string is the length of literal-1" — is the rule for a figurative constant whose CONTEXT
    /// does not specify a length, which is exactly a PICTURE-less item; GR2, which repeats the string to the
    /// receiver's size, governs a SIZED receiver and was applied here by mistake. <c>01 G VALUE ALL "AB".</c>
    /// therefore implies <c>X(2)</c> and holds "AB"; it used to bind as the one-character recovery item and
    /// print "A", with no diagnostic of any kind.</summary>
    [Fact]
    public void Sr9_AllLiteral_TakesLiteral1sLength()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9ALL",
                "       01 G VALUE ALL \"AB\".\n       01 J VALUE ALL \"A\".\n       01 H VALUE ALL N\"AB\".\n",
                "           DISPLAY \"[\" G \"]\" FUNCTION LENGTH(G)\n"
                + "           DISPLAY \"[\" J \"]\" FUNCTION LENGTH(J)\n"
                + "           DISPLAY \"[\" H \"]\" FUNCTION LENGTH(H)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[AB]2\r\n[A]1\r\n[AB]2", stdout);
    }

    /// <summary>§8.3.3.6.4 GR1 — "When a figurative constant is used in a context requiring national characters,
    /// the figurative constant represents a national character value" — and the only context a PICTURE-less
    /// entry offers is the usage that applies to it, which §13.18.60.4 GR1 lets a GROUP supply. So the same
    /// <c>VALUE SPACE</c> implies <c>N(1)</c> under a USAGE NATIONAL group and <c>X(1)</c> outside one; the
    /// witness is that the national one is accepted at all, since an implied <c>X(1)</c> under USAGE NATIONAL is
    /// §13.18.60.3 SR12's refusal.</summary>
    [Fact]
    public void Sr9_FigurativeInANationalContext_ImpliesANationalPicture()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9NCTX",
                "       01 G USAGE NATIONAL.\n          05 A VALUE SPACE.\n          05 B VALUE ALL N\"AB\".\n",
                "           DISPLAY \"A=\" FUNCTION LENGTH(A) \" B=[\" B \"]\" FUNCTION LENGTH(B)\n"),
            dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("A=1 B=[AB]2", stdout);
    }

    /// <summary>§8.3.3.6.4 GR4 — the ZERO format "represents the numeric value '0', one or more of the boolean
    /// character '0', or one or more of the character '0' ... depending on context" — and §13.18.60.3 SR5 makes
    /// usage bit a context whose picture "describes a boolean data item". It reaches the ZERO format only:
    /// §8.3.3.6.4 GR5–GR8 give SPACE / HIGH-VALUE / LOW-VALUE / QUOTE no boolean representation, so
    /// <c>USAGE BIT VALUE SPACE</c> implies <c>X(1)</c> and draws SR5's refusal — one diagnostic, from the
    /// picture screen, exactly as a written <c>PIC X(1) USAGE BIT</c> does.</summary>
    [Fact]
    public void Sr9_ZeroFigurativeInABooleanContext_ImpliesABooleanPicture()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9BCTX", "       01 A USAGE BIT VALUE ZERO.\n",
                "           DISPLAY \"[\" A \"]\" FUNCTION LENGTH(A)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[0]1", stdout);

        var refused = CompileAndRun(
            Program("SR9BCTXR", "       01 A USAGE BIT VALUE SPACE.\n", "           DISPLAY A\n"),
            dialectLevel: 2002);
        Assert.False(refused.ok);
        Assert.Contains("COBOLNET0881", refused.detail, StringComparison.Ordinal);
        Assert.Contains("SR5", refused.detail, StringComparison.Ordinal);
    }

    // ── The composed entry: a group member, a TYPEDEF template and its clone ─────────────────────────────────

    /// <summary>The implied clause is a PICTURE clause, so the member composes into its group's character image
    /// exactly as a written one does. Before the synthesis the picture-less member was reclassified as
    /// dynamic-length and <c>DISPLAY R</c> threw <c>NotImplementedCobolFeatureException</c> — "a variable-length
    /// group has no fixed record window" — on a legal FIXED-length group.</summary>
    [Fact]
    public void Sr9_ImpliedMember_ComposesIntoItsGroupImage()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9GRP",
                "       01 R.\n          05 A VALUE \"HELLO\".\n          05 B PIC X(3) VALUE \"XYZ\".\n",
                "           DISPLAY \"[\" R \"] \" FUNCTION LENGTH(R)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[HELLOXYZ] 8", stdout);
    }

    /// <summary>The rule's subject is the entry AS COMPOSED, so the synthesis runs over the COMPOSITION forest:
    /// a TYPEDEF TEMPLATE's own entry needs the implied picture (the §13.16.3 SR8 guard reports over it) and so
    /// does the <c>TYPE</c> CLONE (the emitter lays that one out). A pass over the written-entry forest alone
    /// would reject this program at the template.</summary>
    [Fact]
    public void Sr9_TypedefTemplateAndItsClone_BothTakeTheImpliedPicture()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9TDEF",
                "       01 T TYPEDEF.\n          05 A VALUE \"HELLO\".\n          05 B PIC X(3) VALUE \"XYZ\".\n"
                + "       01 R TYPE T.\n",
                "           DISPLAY \"[\" R \"] \" FUNCTION LENGTH(R)\n"), dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("[HELLOXYZ] 8", stdout);
    }

    // ── What SR9 does NOT grant: §13.16.3 SR8 still requires the PICTURE ─────────────────────────────────────

    /// <summary>SR9's own exclusion — "an alphanumeric, boolean, or national literal THAT IS NOT A ZERO-LENGTH
    /// LITERAL" — plus the three spellings outside its three categories: a numeric literal, the class-pointer
    /// figurative NULL, and an entry with no VALUE clause at all. Each falls to §13.16.3 SR8.
    /// <para>⛔ This is the boundary the former carve-out blurred: the SR8 guard used to skip ANY figurative
    /// VALUE, which silently admitted <c>ALL ""</c> as well.</para></summary>
    [Theory]
    [InlineData("SR9ZL", "       01 A VALUE \"\".\n")]
    [InlineData("SR9ZLA", "       01 A VALUE ALL \"\".\n")]
    [InlineData("SR9NUM", "       01 A VALUE 42.\n")]
    [InlineData("SR9NUL", "       01 A VALUE NULL.\n")]
    [InlineData("SR9NOV", "       01 A USAGE DISPLAY.\n")]
    public void Sr9_GrantsNothingHere_SoSr8StillRequiresThePicture(string id, string entry)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, entry, "           DISPLAY A\n"), dialectLevel: 2002);
        Assert.False(ok, "expected §13.16.3 SR8's rejection");
        Assert.Contains("COBOLNET0881", detail, StringComparison.Ordinal);
    }

    /// <summary>SR9 grants the omission only "in the DATA-ITEM FORMAT of the VALUE clause", which is
    /// §13.18.63.2 Format 1. The Format-2 (table) spelling is a different format and SR9 does not reach it, so a
    /// picture-less <c>OCCURS … VALUES ARE … FROM</c> entry stays §13.16.3 SR8's rejection.</summary>
    [Fact]
    public void Sr9_DoesNotReachTheFormat2TableValue()
    {
        var (ok, _, detail) = CompileAndRun(
            Program("SR9TBL", "       01 R.\n          05 A OCCURS 3 VALUES ARE \"AB\" FROM (1).\n",
                "           DISPLAY A(1)\n"), dialectLevel: 2002);
        Assert.False(ok, "expected §13.16.3 SR8's rejection — SR9 grants only the data-item format");
        Assert.Contains("COBOLNET0881", detail, StringComparison.Ordinal);
    }

    // ── The SIBLING format: §13.15.3 SR14, the report group description entry ────────────────────────────────

    /// <summary>⛔ THE SECOND ARM. §13.15.3 SR14 states §13.16.3 SR9 word for word for the REPORT GROUP
    /// description entry, and the report binder rejected it — <c>02 COLUMN 1 VALUE "HELLO".</c> drew
    /// "printable item at COLUMN 1 has no PICTURE clause" on legal source while its data-division twin compiled.
    /// The two arms now share one classifier and one edition gate. The printed line proves the implied
    /// <c>X(5)</c> is a real five-character item: HELLO at column 1, then the SOURCE item at column 10.</summary>
    [Fact]
    public void Sr14_ReportGroupEntry_TakesTheSameImpliedPicture()
    {
        string src =
            "       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. SR14RPT.\n"
            + "       ENVIRONMENT DIVISION.\n"
            + "       INPUT-OUTPUT SECTION.\n"
            + "       FILE-CONTROL.\n"
            + "           SELECT RPT ASSIGN TO \"sr14rpt.rpt\".\n"
            + "       DATA DIVISION.\n"
            + "       FILE SECTION.\n"
            + "       FD RPT REPORT IS R-1.\n"
            + "       WORKING-STORAGE SECTION.\n"
            + "       01 WS-SRC PIC 99 VALUE 7.\n"
            + "       REPORT SECTION.\n"
            + "       RD R-1\n"
            + "           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.\n"
            + "       01 DET-A TYPE DE LINE PLUS 1.\n"
            + "          02 COLUMN 1 VALUE \"HELLO\".\n"
            + "          02 COLUMN 10 PIC 99 SOURCE IS WS-SRC.\n"
            + "       PROCEDURE DIVISION.\n"
            + "       MAIN.\n"
            + "           OPEN OUTPUT RPT\n"
            + "           INITIATE R-1\n"
            + "           GENERATE DET-A\n"
            + "           TERMINATE R-1\n"
            + "           CLOSE RPT\n"
            + "           DISPLAY \"DONE\"\n"
            + "           STOP RUN.\n";
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("DONE", stdout);
        string[] printed = File.ReadAllLines(Path.Combine(TempDir, "sr14rpt.rpt"));
        Assert.Contains(printed, l => l.TrimEnd() == "HELLO    07");
    }

    // ── The edition boundary ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>COBOL-85 made no such grant, so below the introducing edition the compiler names the construct
    /// and the edition that introduces it (the <c>value-implied-picture-2002</c> registry row, COBOLNET0900
    /// band) rather than the generic SR8 rejection — the co-equal diagnostic obligation of every introduction
    /// gate. The identical source compiles at 2002.</summary>
    [Fact]
    public void Sr9_BelowItsIntroducingEdition_NamesTheEdition()
    {
        string src = Program("SR9E85", "       01 A VALUE \"HELLO\".\n", "           DISPLAY A\n");
        var below = CompileAndRun(src, dialectLevel: 85);
        Assert.False(below.ok, "§13.16.3 SR9 is a post-85 addition");
        Assert.Contains("COBOLNET0900", below.detail, StringComparison.Ordinal);
        Assert.Contains("COBOL-2002", below.detail, StringComparison.Ordinal);

        var at2002 = CompileAndRun(Program("SR9E02", "       01 A VALUE \"HELLO\".\n", "           DISPLAY A\n"),
            dialectLevel: 2002);
        Assert.True(at2002.ok, at2002.detail);
        Assert.Equal("HELLO", at2002.stdout);
    }

    // ── The DRIFT guard: the three arms, re-read from the standard on every run ──────────────────────────────

    /// <summary>⛔ §13.16.3 SR9's three arms AGAINST THEIR OWN SOURCE, re-read out of <c>specs/ISO_COBOL.md</c>
    /// — which PICTURE character-string each literal class implies. The classifier's class→symbol table is the
    /// one thing here a compiler-side probe cannot contradict (a wrong symbol moves the synthesis and every
    /// behavioural expectation together), so the standard's own sentences are the only independent oracle —
    /// the <c>PicturelessUsageSetDriftTests</c> discipline, applied to the sibling rule.</summary>
    [Fact]
    public void Sr9_ImpliedPictureText_MatchesTheStandardsThreeArms()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.16\.3\b"));
        Assert.True(start >= 0, "§13.16.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.16\.4\b"));
        Assert.True(end > start, "§13.16.4 not found after §13.16.3 — the heading shape changed.");

        // SR9's arms are printed one per line as `a) if the literal is <class>, 'PICTURE <symbol>(length)'`.
        var arms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines[start..end])
        {
            var m = Regex.Match(line,
                @"^\s*[abc]\)\s*if the literal is (\w+),\s*'PICTURE\s*([XN1])\(length\)'");
            if (m.Success) arms[m.Groups[1].Value] = m.Groups[2].Value;
        }
        Assert.True(arms.Count == 3,
            "§13.16.3 SR9's three implied-PICTURE arms did not parse — fix the scanner against the printed "
            + $"rule, never the other way round; {arms.Count} parsed.");

        var expected = new Dictionary<LiteralClass, string>
        {
            [LiteralClass.Alphanumeric] = arms["alphanumeric"],
            [LiteralClass.Boolean] = arms["boolean"],
            [LiteralClass.National] = arms["national"],
        };
        foreach (var (cls, symbol) in expected)
            Assert.Equal($"{symbol}(7)", new ImpliedPicture(cls, 7).Text);
    }

    /// <summary>The classifier itself, at the level the three arms are chosen — including the two spellings the
    /// end-to-end programs cannot distinguish from their output: the literal's CLASS decides the arm for every
    /// quoted form (plain and hexadecimal), and a figurative constant's class comes from its CONTEXT.</summary>
    [Theory]
    [InlineData("\"HELLO\"", "X(5)")]
    [InlineData("'HELLO'", "X(5)")]
    [InlineData("X\"414243\"", "X(3)")]
    [InlineData("N\"HELLO\"", "N(5)")]
    [InlineData("NX\"00410042\"", "N(2)")]
    [InlineData("B\"1011\"", "1(4)")]
    [InlineData("BX\"F\"", "1(4)")]
    [InlineData("ALL\"AB\"", "X(2)")]
    [InlineData("ALLN\"AB\"", "N(2)")]
    [InlineData("SPACE", "X(1)")]
    [InlineData("ALLSPACES", "X(1)")]
    public void Sr9Classifier_SelectsTheArmFromTheLiteralsClassAndLength(string raw, string expected)
    {
        var implied = DataBinder.Sr9ImpliedFor(raw, Usage.Display);
        Assert.NotNull(implied);
        Assert.Equal(expected, implied!.Value.Text);
    }

    /// <summary>The operands SR9 grants NOTHING for — the classifier answers null and §13.16.3 SR8 governs.
    /// A zero-length literal is excluded by SR9's own words in every one of its spellings (§8.3.3.1: "If the
    /// opening and closing delimiters are contiguous, the length of the literal is zero").</summary>
    [Theory]
    [InlineData("\"\"")]
    [InlineData("N\"\"")]
    [InlineData("B\"\"")]
    [InlineData("ALL\"\"")]
    [InlineData("42")]
    [InlineData("-12.5")]
    [InlineData("NULL")]
    [InlineData("NULLS")]
    public void Sr9Classifier_GrantsNothingOutsideItsThreeCategories(string raw)
        => Assert.Null(DataBinder.Sr9ImpliedFor(raw, Usage.Display));

    /// <summary>§8.3.3.6.4 GR1 and GR4 on the classifier: the CONTEXT moves a figurative constant's class, and
    /// GR4's boolean arm is the ZERO format's alone.</summary>
    [Fact]
    public void Sr9Classifier_FigurativeClassFollowsTheContext()
    {
        Assert.Equal("N(1)", DataBinder.Sr9ImpliedFor("SPACE", Usage.National)!.Value.Text);
        Assert.Equal("1(1)", DataBinder.Sr9ImpliedFor("ZERO", Usage.Bit)!.Value.Text);
        Assert.Equal("X(1)", DataBinder.Sr9ImpliedFor("SPACE", Usage.Bit)!.Value.Text);
        // A written literal keeps its own class whatever the context: SR9 reads the LITERAL, and GR1 speaks only
        // of a figurative constant.
        Assert.Equal("X(2)", DataBinder.Sr9ImpliedFor("\"AB\"", Usage.National)!.Value.Text);
    }

    /// <summary>⛔ The §13.18.60.4 GR1 context walk is asked PULL-style here (the pass runs before
    /// `UsageInheritancePass` pushes the same fact down), so it is measured against a chain DEEPER THAN ONE: the
    /// USAGE clause sits two levels above the entry. A self-only or one-level reading implies `X(1)`, which
    /// §13.18.60.3 SR12 then refuses — so this program compiling at all is the witness, and its LENGTH 1 with a
    /// national twin proves the entry is the national item GR1 makes it (probe_the_shape_the_subject_hides: the
    /// flat case cannot tell the two readings apart).</summary>
    [Fact]
    public void Sr9_FigurativeContext_TravelsTheWholeAncestorChain()
    {
        var (ok, stdout, detail) = CompileAndRun(
            Program("SR9DEEP",
                "       01 G USAGE NATIONAL.\n          05 H.\n             10 A VALUE SPACE.\n"
                + "             10 B VALUE N\"XY\".\n",
                "           DISPLAY \"A=\" FUNCTION LENGTH(A) \" B=[\" B \"]\" FUNCTION LENGTH(B)\n"),
            dialectLevel: 2002);
        Assert.True(ok, detail);
        // A takes GR1's national representation from two levels up — an implied X(1) under USAGE NATIONAL would
        // be §13.18.60.3 SR12's refusal, so compiling at all is the witness — and B is SR9 c)'s N(2).
        Assert.Equal("A=1 B=[XY]2", stdout);
    }
}
