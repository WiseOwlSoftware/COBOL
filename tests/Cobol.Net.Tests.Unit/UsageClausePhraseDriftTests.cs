// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WHICH USAGE EACH OPTIONAL USAGE-CLAUSE PHRASE MAY FOLLOW IS WRITTEN ONCE, IN <c>UsagePhraseRules</c>, AND IT IS
/// THE PRINTED FIGURE'S (ISO §13.18.60.2; kb/Work PB174, PB3517).
///
/// <para><b>Why this test exists.</b> The grammar parses the clause's four optional phrases — <c>SIGNED</c> /
/// <c>UNSIGNED</c>, <c>WITH NO SIGN</c>, the endianness-phrase and the encoding-phrase — after EVERY usage word
/// (<c>usageClause : (USAGE IS?)? usageKeyword binarySign? noSignPhrase? floatFormatPhrase*</c>), and the binder
/// narrows. The narrowing was once three hand-written arms in <c>DataBinder.BindEntry</c>, and the fourth was
/// never written: <c>PIC 9(2) USAGE COMP UNSIGNED</c>, <c>USAGE DISPLAY SIGNED</c> and <c>PIC S9(3) PACKED-DECIMAL
/// UNSIGNED</c> compiled clean, the word ignored (kb/Work PB3517, found by the differential fuzz). Earlier, five of
/// the figure's optional tails had been dropped from the grammar silently (kb/Work PB174). Each failure was a phrase
/// or a usage added without anyone asking what the printed line says.</para>
///
/// <para><b>What makes it a drift test.</b> Five checks, each against an inventory the test reads rather than one
/// somebody remembered: (1) every <c>usageKeyword</c> alternative read out of the <c>.g4</c> has a row below saying
/// which phrases its printed line carries; (2) every phrase rule read out of the <c>usageClause</c> rule maps onto a
/// <see cref="UsagePhrase"/> and every member has a <see cref="UsagePhraseRules"/> row; (3) the production table
/// agrees with this test's transcription of the figure for every grammar usage word, through the compiler's own
/// <see cref="PictureAnalyzer.ParseUsage"/>; (4) the four refusal descriptors are read only by the table, and the
/// clause's phrase children only by <c>DataBinder.ScreenUsagePhrases</c> and the edition gate, so the rule cannot
/// grow a second copy; (5) every grammar rule that hosts <c>usageClause</c> is read out of the <c>.g4</c> files and
/// probed, so a host whose reader never asks the table (the REPORT SECTION's, until branch review R1-1 of PB3517)
/// fails. A new phrase, usage word or host fails here until its row is decided.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class UsageClausePhraseDriftTests : CobolNetTestBase
{
    /// <summary>Which of the four optional phrases the PRINTED §13.18.60.2 figure (PDF p.533 = printed folio 503,
    /// rendered) writes on each usage word's line. BINARY-CHAR/-SHORT/-LONG/-DOUBLE print <c>{ SIGNED | UNSIGNED
    /// }</c>; PACKED-DECIMAL prints <c>[ WITH NO SIGN ]</c>; FLOAT-BINARY-32/-64/-128 print <c>[ endianness-phrase
    /// ]</c>; FLOAT-DECIMAL-16/-34 print a bracketed choice-indicator group over { encoding-phrase,
    /// endianness-phrase }. Every other line prints none. The COMP-n words are not on the figure: they are this
    /// implementation's vendor spellings (docs/CONFORMANCE.md's nonstandard-extension register), and each takes the
    /// phrases of the standard usage it spells — COMP-3 is PACKED-DECIMAL's spelling (register row
    /// <c>usage-comp-3</c>), so it takes WITH NO SIGN; COMP / COMP-4 are BINARY's and COMP-1 / COMP-2 the
    /// implementor float usages', which print none; COMP-5 has no standard twin and takes none.</summary>
    private static readonly Dictionary<string, UsagePhrase[]> PrintedPhrases = new(StringComparer.Ordinal)
    {
        ["DISPLAY"] = [],
        ["COMPUTATIONAL"] = [],
        ["COMPUTATIONAL_1"] = [],
        ["COMPUTATIONAL_2"] = [],
        ["COMPUTATIONAL_3"] = [UsagePhrase.NoSign],
        ["COMPUTATIONAL_4"] = [],
        ["COMPUTATIONAL_5"] = [],
        ["COMP"] = [],
        ["COMP_1"] = [],
        ["COMP_2"] = [],
        ["COMP_3"] = [UsagePhrase.NoSign],
        ["COMP_4"] = [],
        ["COMP_5"] = [],
        ["FLOAT_SHORT"] = [],
        ["FLOAT_LONG"] = [],
        ["FLOAT_EXTENDED"] = [],
        ["FLOAT_BINARY_32"] = [UsagePhrase.Endianness],
        ["FLOAT_BINARY_64"] = [UsagePhrase.Endianness],
        ["FLOAT_BINARY_128"] = [UsagePhrase.Endianness],
        ["FLOAT_DECIMAL_16"] = [UsagePhrase.Endianness, UsagePhrase.Encoding],
        ["FLOAT_DECIMAL_34"] = [UsagePhrase.Endianness, UsagePhrase.Encoding],
        ["BINARY_CHAR"] = [UsagePhrase.Sign],
        ["BINARY_SHORT"] = [UsagePhrase.Sign],
        ["BINARY_LONG"] = [UsagePhrase.Sign],
        ["BINARY_DOUBLE"] = [UsagePhrase.Sign],
        ["BINARY"] = [],
        ["PACKED_DECIMAL"] = [UsagePhrase.NoSign],
        ["INDEX"] = [],
        // USAGE MESSAGE-TAG (kb/Work PB487): the printed line is the bare word; the usage is DECLINED non-support
        // (Annex A.3 item 4) and refused by name, COBOLNET1943.
        ["MESSAGE_TAG"] = [],
        ["NATIONAL"] = [],
        ["BIT"] = [],
        // The four RULES print their own operand tails (TO type-name-1, TO prototype-name, the OBJECT REFERENCE
        // class phrase), written inside their grammar rules, and none of the four optional phrases.
        ["dataPointerUsage"] = [],
        ["programPointerUsage"] = [],
        ["functionPointerUsage"] = [],
        ["objectReferenceUsage"] = [],
    };

    /// <summary>The grammar rule each phrase is written as, under <c>usageClause</c> (the two float phrases through
    /// <c>floatFormatPhrase</c>).</summary>
    private static readonly Dictionary<string, UsagePhrase> PhraseRule = new(StringComparer.Ordinal)
    {
        ["binarySign"] = UsagePhrase.Sign,
        ["noSignPhrase"] = UsagePhrase.NoSign,
        ["endiannessPhrase"] = UsagePhrase.Endianness,
        ["encodingPhrase"] = UsagePhrase.Encoding,
    };

    private static string GrammarPath =>
        Path.Combine(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "Core", "CobolData.g4");

    /// <summary>The body of parser rule <paramref name="rule"/>, comments stripped, read straight out of the .g4.
    /// The repo's grammar style puts a rule's name alone on its line and its body in indented <c>:</c>/<c>|</c>
    /// lines ending at a lone <c>;</c>.</summary>
    private static List<string> RuleBodyLines(string rule)
    {
        var lines = new List<string>();
        bool inRule = false;
        foreach (string line in File.ReadAllLines(GrammarPath))
        {
            if (Regex.IsMatch(line, $@"^{rule}\s*$")) { inRule = true; continue; }
            if (!inRule) continue;
            if (line.TrimStart().StartsWith(';')) break;
            string body = line.Split("//")[0].TrimStart();
            if (body.StartsWith(':') || body.StartsWith('|')) lines.Add(body[1..]);
        }
        return lines;
    }

    /// <summary>The <c>usageKeyword</c> rule's alternatives (tokens and rule names).</summary>
    private static SortedSet<string> GrammarUsageKeywords()
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string body in RuleBodyLines("usageKeyword"))
            foreach (Match m in Regex.Matches(body, @"[A-Za-z_][A-Za-z0-9_]*")) found.Add(m.Value);
        return found;
    }

    /// <summary>The parser rules <c>usageClause</c> writes after its keyword, with <c>floatFormatPhrase</c>
    /// expanded into its alternatives. Parser rules are the lower-camel words; tokens are upper case.</summary>
    private static SortedSet<string> GrammarPhraseRules()
    {
        static IEnumerable<string> RuleRefs(string rule) =>
            RuleBodyLines(rule).SelectMany(b => Regex.Matches(b, @"\b[a-z][A-Za-z0-9]*\b").Select(m => m.Value));
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string r in RuleRefs("usageClause").Where(r => r != "usageKeyword"))
        {
            if (r == "floatFormatPhrase") foreach (string f in RuleRefs("floatFormatPhrase")) found.Add(f);
            else found.Add(r);
        }
        return found;
    }

    /// <summary>The COBOL spelling of a <c>usageKeyword</c> alternative, as <c>DataBinder.UsageKeyword</c> hands it
    /// to <see cref="PictureAnalyzer.ParseUsage"/>.</summary>
    private static string Spelling(string keyword) => keyword switch
    {
        "dataPointerUsage" => "POINTER",
        "programPointerUsage" => "PROGRAM-POINTER",
        "functionPointerUsage" => "FUNCTION-POINTER",
        "objectReferenceUsage" => "OBJECT REFERENCE",
        _ => keyword.Replace('_', '-'),
    };

    [Fact]
    public void EveryUsageKeyword_HasDecidedItsPhrases()
    {
        var grammar = GrammarUsageKeywords();
        Assert.NotEmpty(grammar);   // a sweep that found nothing would pass every assertion below vacuously
        var classified = new SortedSet<string>(PrintedPhrases.Keys, StringComparer.Ordinal);
        Assert.True(grammar.SetEquals(classified),
            "the grammar's usageKeyword inventory and this test's §13.18.60.2 phrase classification have diverged.\n"
            + $"  grammar : {string.Join(", ", grammar)}\n"
            + $"  table   : {string.Join(", ", classified)}\n"
            + "RENDER THE PRINTED PAGE (python scripts/render-spec-page.py 533) and record which of the four optional "
            + "phrases the standard writes on the new usage's line, then make UsagePhraseRules agree.");
    }

    [Fact]
    public void EveryGrammarPhrase_IsARowOfTheOneTable()
    {
        var grammar = GrammarPhraseRules();
        Assert.True(grammar.SetEquals(PhraseRule.Keys),
            "usageClause's phrase rules and UsagePhrase have diverged.\n"
            + $"  grammar : {string.Join(", ", grammar)}\n"
            + $"  mapped  : {string.Join(", ", PhraseRule.Keys)}\n"
            + "A phrase the grammar parses after every usage word needs a UsagePhrase member and a UsagePhraseRules "
            + "row deciding which usages the §13.18.60.2 figure prints it on, or it is accepted (and ignored) on all "
            + "of them, which is kb/Work PB3517.");
        foreach (UsagePhrase phrase in Enum.GetValues<UsagePhrase>())
        {
            Assert.Contains(phrase, PhraseRule.Values);
            Assert.Equal(phrase, UsagePhraseRules.Of(phrase).Phrase);
        }
    }

    [Fact]
    public void TheTable_AgreesWithThePrintedFigure_ForEveryUsageWord()
    {
        var disagreements = new List<string>();
        foreach (string keyword in GrammarUsageKeywords())
        {
            Usage usage = PictureAnalyzer.ParseUsage(Spelling(keyword), new EditionContext(2023), "data item 'T'");
            foreach (UsagePhrase phrase in Enum.GetValues<UsagePhrase>())
            {
                bool printed = PrintedPhrases[keyword].Contains(phrase);
                if (UsagePhraseRules.Of(phrase).Admits(usage) != printed)
                    disagreements.Add($"USAGE {Spelling(keyword)} ({usage}) + {phrase}: the figure says "
                        + $"{(printed ? "printed" : "not printed")}, the table says the opposite");
            }
        }
        Assert.True(disagreements.Count == 0, string.Join("\n", disagreements));
    }

    /// <summary>The rule lives in one place: the refusal descriptors are named only by the table (and defined in
    /// the catalog), and the clause's phrase children are read only by the one screen and by the edition gate,
    /// which asks only whether a phrase was WRITTEN (a phrase's introduction edition is the same whatever usage it
    /// follows).</summary>
    [Fact]
    public void TheRule_IsWrittenOnlyInTheTable()
    {
        var refusalNames = new Regex(@"\b(UsageSignPhraseScope|UsageNoSignPhraseScope|UsageEndiannessPhraseScope"
            + @"|UsageEncodingPhraseScope)\b|""COBOLNET(4733|1565|1716|1717)""");
        var phraseReaders = new Regex(@"\.(binarySign|noSignPhrase|floatFormatPhrase)\(\)");
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file).Replace('\\', '/');
            if (rel.Contains("/obj/") || rel.Contains("/bin/") || rel.Contains("/Generated/")) continue;
            string name = Path.GetFileName(file);
            string text = File.ReadAllText(file);
            if (name is not ("UsagePhrase.cs" or "DiagnosticCatalog.cs") && refusalNames.IsMatch(text))
                offenders.Add($"{rel}: names a usage-phrase refusal outside UsagePhraseRules");
            if (name is not ("DataBinder.UsagePhrases.cs" or "VersionConformancePass.cs") && phraseReaders.IsMatch(text))
                offenders.Add($"{rel}: reads a USAGE clause phrase outside DataBinder.ScreenUsagePhrases");
        }
        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    /// <summary>One phrase as a programmer writes it, per <see cref="UsagePhrase"/> member — what the host probe
    /// below writes after USAGE DISPLAY, whose §13.18.60.2 line prints none of the four.</summary>
    private static readonly Dictionary<UsagePhrase, string> PhraseSample = new()
    {
        [UsagePhrase.Sign] = "UNSIGNED",
        [UsagePhrase.NoSign] = "WITH NO SIGN",
        [UsagePhrase.Endianness] = "HIGH-ORDER-LEFT",
        [UsagePhrase.Encoding] = "BINARY-ENCODING",
    };

    /// <summary>How each grammar rule that hosts <c>usageClause</c> is probed: a program that writes one entry per
    /// <see cref="UsagePhrase"/> (named <c>R-1</c>, <c>R-2</c>, …, each <c>USAGE DISPLAY</c> plus that phrase) in
    /// that host, or <see langword="null"/> for a host whose whole module is declined (the screen description
    /// entry, Annex A.4.2, refused by <c>ScreenFacility</c> as COBOLNET1560).</summary>
    private static readonly Dictionary<string, Func<string, string, string>?> UsageClauseHosts = new(StringComparer.Ordinal)
    {
        // §13.16.2 format 1: a data description entry.
        ["dataDescriptionClause"] = static (id, phrases) => Program(id, string.Join("\n",
            phrases.Split('|').Select((p, i) => $"       01 R-{i + 1} PIC 9(3) USAGE DISPLAY {p}."))),
        // §13.15.2: a report group description entry, whose USAGE clause §13.15.4 GR2 makes "the same clauses as
        // those that are described under the general format for a data description entry".
        ["reportGroupClause"] = static (id, phrases) => ReportProgram(id, string.Join("\n",
            phrases.Split('|').Select((p, i) =>
                $"          05 R-{i + 1} COLUMN {1 + 4 * i} PIC 9(3) SOURCE W USAGE DISPLAY {p}."))),
        ["screenClause"] = null,
    };

    /// <summary>Every rule in the grammar whose alternatives name <paramref name="ruleRef"/>, read out of every
    /// <c>.g4</c> under the Grammar directory (the repo's style: a rule's name alone on its line, its body in
    /// <c>:</c>/<c>|</c> lines ending at a lone <c>;</c>).</summary>
    private static SortedSet<string> RulesReferencing(string ruleRef)
    {
        var hosts = new SortedSet<string>(StringComparer.Ordinal);
        var reference = new Regex($@"\b{ruleRef}\b");
        foreach (string g4 in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4",
                     SearchOption.AllDirectories))
        {
            string? rule = null;
            foreach (string line in File.ReadAllLines(g4))
            {
                if (Regex.Match(line, @"^([a-z][A-Za-z0-9]*)\s*$") is { Success: true } name) { rule = name.Groups[1].Value; continue; }
                if (rule is null) continue;
                if (line.TrimStart().StartsWith(';')) { rule = null; continue; }
                string body = line.Split("//")[0];
                if (rule != ruleRef && reference.IsMatch(body)) hosts.Add(rule);
            }
        }
        return hosts;
    }

    /// <summary>⛔ EVERY PLACE THE GRAMMAR ACCEPTS A USAGE CLAUSE SCREENS ITS PHRASES (kb/Work PB3517, branch review
    /// R1-1). The one table is only as good as the set of readers that ask it: a report group description entry
    /// parses the same <c>usageClause</c>, and its reader once took the usage word alone, so <c>USAGE DISPLAY
    /// UNSIGNED</c> and <c>USAGE DISPLAY WITH NO SIGN</c> in a REPORT SECTION compiled clean and printed exactly what
    /// the phrase-free entry printed — while <see cref="TheRule_IsWrittenOnlyInTheTable"/>, which finds readers of
    /// the phrase children, stayed green, because a reader that ignores them is not a reader. This test finds the
    /// HOSTS instead, out of the grammar, and probes each with every phrase on a usage that prints none of them:
    /// each entry must draw its phrase's own refusal, once, naming the entry. A new host of <c>usageClause</c> fails
    /// here until it has a probe (or is recorded as declined).</summary>
    [Fact]
    public void EveryGrammarHostOfTheUsageClause_ScreensItsPhrases_PinnedToSpec()
    {
        var hosts = RulesReferencing("usageClause");
        Assert.NotEmpty(hosts);   // a sweep that found nothing would pass vacuously
        Assert.True(hosts.SetEquals(UsageClauseHosts.Keys),
            "the grammar's hosts of usageClause and this test's probes have diverged.\n"
            + $"  grammar : {string.Join(", ", hosts)}\n"
            + $"  probed  : {string.Join(", ", UsageClauseHosts.Keys)}\n"
            + "A new host must pass its USAGE clause to DataBinder.ScreenUsagePhrases and get a probe here.");
        Assert.True(Enum.GetValues<UsagePhrase>().ToHashSet().SetEquals(PhraseSample.Keys),
            "every UsagePhrase member needs a sample in PhraseSample");

        var phrases = Enum.GetValues<UsagePhrase>();
        string written = string.Join("|", phrases.Select(p => PhraseSample[p]));
        foreach (var (host, build) in UsageClauseHosts)
        {
            if (build is null)
            {
                var declined = CompileText($"host-{host}", 2023, id => ScreenProgram(id));
                Assert.Contains(declined, e => e.Contains("COBOLNET1560", StringComparison.Ordinal)
                    && e.Contains("the USAGE clause of a screen description entry", StringComparison.Ordinal));
                continue;
            }
            var errors = CompileText($"host-{host}", 2023, id => build(id, written));
            for (int i = 0; i < phrases.Length; i++)
            {
                string code = UsagePhraseRules.Of(phrases[i]).Refusal.Code;
                string entry = $"'R-{i + 1}'";
                Assert.True(errors.Count(e => e.Contains(code, StringComparison.Ordinal)) == 1,
                    $"{host}: USAGE DISPLAY {PhraseSample[phrases[i]]} should draw exactly one {code}; drew:\n"
                    + string.Join("\n", errors));
                Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal)
                    && e.Contains(entry, StringComparison.Ordinal) && e.Contains("not USAGE DISPLAY ", StringComparison.Ordinal));
            }
            Assert.True(errors.Count == phrases.Length, $"{host}: expected only the {phrases.Length} phrase "
                + $"refusals, drew:\n{string.Join("\n", errors)}");
        }
    }

    /// <summary>kb/Work PB3517's repro, verbatim in substance: three entries, each writing SIGNED / UNSIGNED on a
    /// usage whose line prints no such phrase, draw ONE COBOLNET4733 EACH and nothing else of the family. Before the
    /// fix the program compiled clean and printed <c>07 AB -005</c>.</summary>
    [Fact]
    public void SignPhraseOnNonBinaryUsages_DrawsOneRefusalPerEntry_PinnedToSpec()
    {
        var errors = CompileAt("pb3517", """
                   01 WS-U PIC 9(2) USAGE COMP UNSIGNED VALUE 7.
                   01 WS-X PIC X(2) USAGE DISPLAY SIGNED VALUE "AB".
                   01 WS-P PIC S9(3) PACKED-DECIMAL UNSIGNED VALUE -5.
            """, 2014);
        Assert.Equal(3, errors.Count(e => e.Contains("COBOLNET4733", StringComparison.Ordinal)));
        Assert.Equal(3, errors.Count);
        foreach (string entry in new[] { "'WS-U'", "'WS-X'", "'WS-P'" })
            Assert.Contains(errors, e => e.Contains(entry, StringComparison.Ordinal) && e.Contains("COBOLNET4733"));
    }

    /// <summary>The negative direction, one usage per phrase that does not print it, each drawing the phrase's own
    /// refusal and naming the usage — what keeps the table honest (a table that admitted everywhere would pass the
    /// positive leg alone).</summary>
    [Theory]
    [InlineData("PIC 9(4) USAGE BINARY SIGNED", UsagePhrase.Sign, "USAGE BINARY")]
    [InlineData("PIC 9(4) COMP-5 UNSIGNED", UsagePhrase.Sign, "USAGE COMP-5")]
    [InlineData("USAGE FLOAT-LONG UNSIGNED", UsagePhrase.Sign, "USAGE FLOAT-LONG")]
    [InlineData("USAGE INDEX SIGNED", UsagePhrase.Sign, "USAGE INDEX")]
    [InlineData("PIC N(2) USAGE NATIONAL UNSIGNED", UsagePhrase.Sign, "USAGE NATIONAL")]
    [InlineData("PIC 9(4) USAGE COMP WITH NO SIGN", UsagePhrase.NoSign, "USAGE COMP")]
    [InlineData("USAGE BINARY-LONG NO SIGN", UsagePhrase.NoSign, "USAGE BINARY-LONG")]
    [InlineData("PIC 9(4) USAGE DISPLAY WITH NO SIGN", UsagePhrase.NoSign, "USAGE DISPLAY")]
    [InlineData("USAGE COMP-1 HIGH-ORDER-LEFT", UsagePhrase.Endianness, "USAGE COMP-1")]
    [InlineData("USAGE COMP-2 HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE COMP-2")]
    [InlineData("USAGE FLOAT-SHORT HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE FLOAT-SHORT")]
    [InlineData("USAGE FLOAT-LONG HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE FLOAT-LONG")]
    [InlineData("USAGE FLOAT-EXTENDED HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE FLOAT-EXTENDED")]
    [InlineData("USAGE INDEX HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE INDEX")]
    [InlineData("USAGE POINTER HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE POINTER")]
    [InlineData("USAGE BINARY-DOUBLE HIGH-ORDER-RIGHT", UsagePhrase.Endianness, "USAGE BINARY-DOUBLE")]
    [InlineData("USAGE FLOAT-BINARY-32 BINARY-ENCODING", UsagePhrase.Encoding, "USAGE FLOAT-BINARY-32")]
    [InlineData("PIC 9(4) USAGE PACKED-DECIMAL DECIMAL-ENCODING", UsagePhrase.Encoding, "USAGE PACKED-DECIMAL")]
    public void PhraseOnAUsageWhoseLineDoesNotPrintIt_IsRefusedByName_PinnedToSpec(
        string entry, UsagePhrase phrase, string usageWords)
    {
        Assert.False(PrintedPhrases.Any(kv => kv.Value.Contains(phrase) && usageWords == "USAGE " + Spelling(kv.Key)),
            $"{usageWords} is classified as printing {phrase}");
        string code = UsagePhraseRules.Of(phrase).Refusal.Code;
        var errors = CompileAt($"neg-{phrase}-{usageWords}", $"01 W {entry}.", 2023);
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal)
            && e.Contains($"not {usageWords} ", StringComparison.Ordinal));
        foreach (UsagePhrase other in Enum.GetValues<UsagePhrase>().Where(p => p != phrase))
            Assert.DoesNotContain(errors, e => e.Contains(UsagePhraseRules.Of(other).Refusal.Code, StringComparison.Ordinal));
    }

    /// <summary>The positive direction: a usage whose printed line carries a phrase ACCEPTS it, clean. The
    /// endianness-phrase on FLOAT-BINARY-128 / FLOAT-DECIMAL-16/-34 is applicable too, but those usages are
    /// documented non-support (COBOLNET1564, Annex A.3 items 17/19), so they cannot supply a clean compile.</summary>
    [Theory]
    [InlineData("USAGE BINARY-CHAR SIGNED")]
    [InlineData("USAGE BINARY-CHAR UNSIGNED")]
    [InlineData("BINARY-SHORT UNSIGNED")]
    [InlineData("USAGE IS BINARY-LONG SIGNED")]
    [InlineData("USAGE BINARY-DOUBLE UNSIGNED")]
    [InlineData("PIC 9(5) USAGE PACKED-DECIMAL WITH NO SIGN")]
    [InlineData("PIC 9(5) PACKED-DECIMAL NO SIGN")]
    [InlineData("USAGE FLOAT-BINARY-32 HIGH-ORDER-LEFT")]
    [InlineData("USAGE FLOAT-BINARY-32 HIGH-ORDER-RIGHT")]
    [InlineData("USAGE FLOAT-BINARY-64 HIGH-ORDER-LEFT")]
    [InlineData("USAGE FLOAT-BINARY-64 HIGH-ORDER-RIGHT")]
    public void PhraseOnAUsageWhoseLinePrintsIt_CompilesClean_PinnedToSpec(string entry)
    {
        var errors = CompileAt($"pos-{entry}", $"01 W {entry}.", 2023);
        Assert.True(errors.Count == 0, $"`{entry}` is legal 2023 source (§13.18.60.2) but drew:\n"
            + string.Join("\n", errors));
    }

    /// <summary>§5.2.6.4: "any single alternative may be specified only once" — and, in the same clause, "The
    /// alternatives may be specified in any order", so the ORDER leg must stay clean while the REPEAT leg rejects.
    /// Without the order leg a screen that rejected the second phrase unconditionally would look correct.</summary>
    [Fact]
    public void RepeatedFloatPhrase_Rejects_ButOrderIsFree_PinnedToSpec()
    {
        var repeated = CompileAt("rep", "01 W USAGE FLOAT-BINARY-32 HIGH-ORDER-LEFT HIGH-ORDER-RIGHT.", 2023);
        Assert.Contains(repeated, e => e.Contains("COBOLNET1718", StringComparison.Ordinal));

        // FLOAT-DECIMAL-16 is the only line that prints BOTH float phrases, so it is the only place the any-order
        // rule is observable. The usage is documented non-support (COBOLNET1564) — assert that the ONLY complaint is
        // that non-support, in EITHER order: no COBOLNET1716/1717/1718.
        foreach (string order in new[] { "BINARY-ENCODING HIGH-ORDER-RIGHT", "HIGH-ORDER-RIGHT BINARY-ENCODING" })
        {
            var errors = CompileAt("ord-" + order.Replace(' ', '-'), $"01 W USAGE FLOAT-DECIMAL-16 {order}.", 2023);
            Assert.Contains(errors, e => e.Contains("COBOLNET1564", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1716", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1717", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1718", StringComparison.Ordinal));
        }
    }

    /// <summary>The 2014 introduction gate (constructs.json <c>usage-float-format-phrase-2014</c>): the phrase is a
    /// 2014 language element in its own right — Annex A.3 item 18 names it separately from item 17's usages — so it
    /// draws COBOLNET0900 below 2014 and nothing at 2014+.</summary>
    [Fact]
    public void EndiannessPhrase_IsGatedAt2014_PinnedToSpec()
    {
        const string source = "01 W USAGE FLOAT-BINARY-32 HIGH-ORDER-RIGHT.";
        Assert.Contains(CompileAt("gate2002", source, 2002), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.DoesNotContain(CompileAt("gate2014", source, 2014), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.DoesNotContain(CompileAt("gate2023", source, 2023), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
    }

    /// <summary>The SIGNED / UNSIGNED phrase exists only on the COBOL-2002 fixed-width binary usages, so it needs no
    /// gate of its own: below 2002 the usage word draws its introduction gate, and on any other usage the phrase is
    /// refused at every edition by the table (COBOLNET4733) — the words are not reserved before 2002, and the
    /// refusal does not depend on that.</summary>
    [Fact]
    public void SignPhrase_IsRefusedOffTheBinaryUsages_AtEveryEdition_PinnedToSpec()
    {
        foreach (int dialect in new[] { 85, 2002, 2014, 2023 })
            Assert.Contains(CompileAt($"ed{dialect}", "01 W PIC 9(4) USAGE COMP UNSIGNED.", dialect),
                e => e.Contains("COBOLNET4733", StringComparison.Ordinal));
        Assert.Empty(CompileAt("ed2002pos", "01 W USAGE BINARY-SHORT UNSIGNED.", 2002));
        Assert.Contains(CompileAt("ed85pos", "01 W USAGE BINARY-SHORT UNSIGNED.", 85),
            e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
    }

    /// <summary>A PROGRAM-ID unique per fixture — .NET serves a STALE same-named assembly otherwise, so a shared name
    /// would silently make every later case re-observe the first one's diagnostics.</summary>
    private static string Program(string programId, string entries) => $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
            {entries}
                   PROCEDURE DIVISION.
                   MAIN.
                       STOP RUN.
            """;

    /// <summary>A COBOL word unique per (fixture tag, edition): the tag's FNV-1a hash, so two tags can never share
    /// a truncated name.</summary>
    private static string ProgramId(string tag, int dialect)
    {
        uint hash = 2166136261;
        foreach (char c in tag) hash = (hash ^ c) * 16777619;
        return $"UCP-{hash:X8}-{dialect}";
    }

    /// <summary>A REPORT SECTION fixture: one detail group <c>DL</c> whose elementary entries are
    /// <paramref name="entries"/>, each sourcing the working-storage item <c>W</c>.</summary>
    private static string ReportProgram(string programId, string entries) => $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "{programId}.txt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT REPORT IS R1.
                   WORKING-STORAGE SECTION.
                   01 W PIC 9(3) VALUE 5.
                   REPORT SECTION.
                   RD R1 PAGE LIMIT 10.
                   01 DL TYPE DETAIL LINE 1.
            {entries}
                   PROCEDURE DIVISION.
                   MAIN.
                       OPEN OUTPUT RPT
                       INITIATE R1
                       GENERATE DL
                       TERMINATE R1
                       CLOSE RPT
                       STOP RUN.
            """;

    /// <summary>A SCREEN SECTION fixture whose one elementary screen item writes a USAGE clause with a phrase.</summary>
    private static string ScreenProgram(string programId) => $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   DATA DIVISION.
                   SCREEN SECTION.
                   01 SCR.
                      05 S-1 LINE 1 COLUMN 1 PIC 9(3) USAGE DISPLAY UNSIGNED.
                   PROCEDURE DIVISION.
                   MAIN.
                       STOP RUN.
            """;

    private List<string> CompileAt(string tag, string entries, int dialect)
    {
        string indented = string.Join("\n", entries.Split('\n').Select(l => l.Trim().Length == 0 ? "" : "       " + l.Trim()));
        return CompileText(tag, dialect, id => Program(id, indented));
    }

    private List<string> CompileText(string tag, int dialect, Func<string, string> source)
    {
        string id = ProgramId(tag, dialect);
        string src = Path.Combine(TempDir, $"{id}.cob");
        File.WriteAllText(src, source(id));
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: dialect, SourceFormat: InitialReferenceFormat.Auto));
        return [.. r.Errors, .. r.Warnings];
    }
}
