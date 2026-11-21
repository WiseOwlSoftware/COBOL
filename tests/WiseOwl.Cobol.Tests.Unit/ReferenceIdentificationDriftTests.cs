// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DATA REFERENCE IS IDENTIFIED IN ONE PLACE, AND A SUM COUNTER IS COUNTED WITH THE DATA-NAMES OF ITS SPELLING
/// (kb/Work PB3287, PB3232).
/// <para><b>PB3287.</b> <c>ReferenceResolver.ResolveForAddressOf</c> re-sequenced the front half of
/// <c>ResolveImplCore</c> by hand (the ref-mod count, the name lookup, the subscript screen) and dropped two of its
/// screens: ADDRESS OF a LINKAGE record the procedure division may not reference (ISO §2023:13.7.3 SR4) compiled clean, and
/// an ambiguous or index-name operand was told it "names none". The sequence is now ONE method,
/// <c>Identify</c>; the two entries differ only by what they build after it. A screen added to it reaches both, and a
/// third entry that re-wrote the sequence would be red here.</para>
/// <para><b>PB3232.</b> A REPORT SECTION sum counter is a data-name (§2023:13.18.54.4 GR5), so for §2023:8.4.2.2.3 SR1 it is one
/// candidate among the ordinary data items of its spelling. <c>SumCounterFamilyFor</c> counted the counters alone, and
/// consulted the ordinary lookup only when the written qualifiers reached no counter, so the counter silently won
/// every collision it had no qualifier for.</para>
/// <para>These are source scans because the property is WHERE the rule is written; the behavioural half is the
/// conformance corpus (<c>pb3232_sum_counter_qualified_beside_data_item</c>, the <c>pb3232-</c> and <c>pb3287-</c>
/// negatives, <c>pb3287_address_of_linkage_sr4_admitted</c>).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReferenceIdentificationDriftTests
{
    /// <summary>The reference-resolution subsystem as one text: the rules live in the funnel and the parts it composes.</summary>
    private static readonly string ReferenceResolver = ResolverSources.Text;

    /// <summary>The text of the member whose declaration line contains <paramref name="signature"/>, through its closing
    /// brace (brace matching over the source; a member is found by its signature, so a rename is red here).</summary>
    private static string BodyOf(string signature)
    {
        int at = ReferenceResolver.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{signature} is no longer in the reference-resolution source — the scan measured nothing");
        int open = ReferenceResolver.IndexOf('{', at);
        int depth = 0;
        for (int i = open; i < ReferenceResolver.Length; i++)
        {
            if (ReferenceResolver[i] == '{') depth++;
            else if (ReferenceResolver[i] == '}' && --depth == 0) return ReferenceResolver[open..(i + 1)];
        }
        throw new InvalidOperationException($"unbalanced braces after {signature}");
    }

    /// <summary>Drop line comments so a rule NAMED in prose is never mistaken for a rule APPLIED in code.</summary>
    private static string Code(string s) => Regex.Replace(s, @"//[^\n]*", " ");

    [Fact]
    public void EveryIdentifierEntry_AsksTheOneIdentificationStep()
    {
        string core = Code(BodyOf("private RefResolution ResolveImplCore("));
        string addressOf = Code(BodyOf("internal AddressOfOperand? ResolveForAddressOf("));
        Assert.Contains("Identify(dref, report, forAddressOf: false)", core, StringComparison.Ordinal);
        Assert.Contains("Identify(dref, report: true, forAddressOf: true)", addressOf, StringComparison.Ordinal);

        // The entries keep only their own tail: none re-reads the written reference, re-looks the name up or re-runs a
        // screen the identification step owns (the pre-PB3287 ResolveForAddressOf did all four).
        foreach (var (entry, body) in new[] { ("ResolveImplCore", core), ("ResolveForAddressOf", addressOf) })
            foreach (string owned in new[]
                     { "ReadWritten(", "DataItemNamed(", "IdentifyDataName(", "ReadSubscripts(",
                       "ScreenRefModCount(", "ScreenLinkageReference(", "ReportUnidentified(" })
                Assert.False(body.Contains(owned, StringComparison.Ordinal),
                    $"ReferenceResolver.{entry} calls {owned} itself — that belongs to ReferenceResolver.Identify, the ONE "
                    + "identification step of a data reference (kb/Work PB3287); a second sequence drops screens (§2023:13.7.3 SR4, "
                    + "§2023:8.4.2.2.3 SR1) the first one has.");
    }

    [Fact]
    public void TheIdentificationStep_OwnsEveryScreenOfTheFrontHalf()
    {
        string identify = Code(BodyOf("private Identified Identify("));
        foreach (string screen in new[]
                 { "CapacityRegisterFor(", "IsExceptionObjectRegister(", "SumCounterFor(", "ReadWritten(", "ScreenRefModCount(",
                   "DataItemNamed(", "ReportUnidentified(", "ScreenLinkageReference(", "ReadSubscripts(" })
            Assert.True(identify.Contains(screen, StringComparison.Ordinal),
                $"ReferenceResolver.Identify no longer asks {screen} — the screen moved out of the one identification step.");
        // §2023:13.7.3 SR4 is asked at exactly three sites of the resolver — the identification step (every identifier), the
        // reported lookup of a TABLE name (FindReportedItem) and the queued screen of a subscript or reference-modification
        // data-name (SubscriptBinder.BindPosition, kb/Work PB5846) — besides the definition.
        Assert.Equal(4,Regex.Matches(Code(ReferenceResolver), @"\bScreenLinkageReference\(").Count);
    }

    /// <summary>⛔ A NAME LOOKUP THAT REACHES A DATA ITEM FROM THE PROCEDURE DIVISION ASKS THE LINKAGE SCREEN, AND THE SET OF
    /// FILES THAT LOOK A NAME UP IS CLOSED (kb/Work PB5846). <c>SubscriptBinder.ResolveSubscriptName</c> resolved a subscript
    /// or reference-modification data-name through the unqualified lookup and never reached
    /// <c>ScreenLinkageReference</c>, so a LINKAGE item the procedure division may not reference (§1985:X.5.1 GR4,
    /// §2023:13.7.3 SR4) compiled as <c>T (L2-N)</c> though <c>DISPLAY L2-N</c> was refused. A source file of the binder that
    /// reaches the data items a written data-name names (<c>NameResolution.DataItemNamed</c>, the one verdict;
    /// <c>SymbolTable.IdentifyDataName</c>, the candidate set beneath it, and the clause and region sets beside it,
    /// <c>IdentifyEntryClauseOperand</c>, <c>IdentifyRegionOperand</c> and <c>IdentifyWithin</c>; since remainder slice 3 no lookup
    /// answers a name's text)
    /// is therefore either one that asks the screen (<c>ScreenLinkageReference</c> / <c>FindReportedItem</c> / <c>Identify</c>)
    /// or one named here with the reason its lookups are no procedure-division reference; a new file with such a lookup is red
    /// until it is one or the other.</summary>
    [Fact]
    public void EveryBinderFileThatLooksANameUp_AsksTheLinkageScreenOrIsNamedWithWhy()
    {
        var noProcedureReference = new Dictionary<string, string>
        {
            ["ConstantEntryBinder.cs"] = "data-division constant entries resolve other entries' names, never a procedure-division operand",
            ["ObjectPropertyResolver.cs"] = "probes whether a qualified head resolves, to choose the property-chain reading; the head is identified through Identify",
            ["PtrBindBasedAndAddressablesPass.cs"] = "the whole-unit storage-claim scan of ADDRESS OF targets, which reads a declaration's depth, not a reference",
            ["CounterResolver.cs"] = "counts the data items spelled as a sum counter's name (an ambiguity count, no item used); the counter's own subscripts go through ReadSubscripts",
            ["KeyedIoBinder.cs"] = "counts a KEY phrase operand's data-name readings beside its record-key-names (an ambiguity count, no item used); data-name-1 itself is identified by ReferenceResolver",
            ["ReportGroupResolution.cs"] = "counts the data items spelled as a report group's unqualified name (an ambiguity count, no item used)",
            ["FileControlBinder.cs"] = "data-division clause operands (the KEY phrases of a SELECT), never a procedure-division operand",
            ["FileControlBinder.IoControl.cs"] = "data-division clause operands (the I-O-CONTROL paragraph), never a procedure-division operand",
            ["FileControlBinder.Resolve.cs"] = "data-division clause operands (RECORD KEY, ASSIGN USING), never a procedure-division operand",
            ["ReportSectionBinder.cs"] = "data-division clause operands (the report clauses), never a procedure-division operand",
            ["DataBinder.cs"] = "data-division clause operands (ResolveClauseOperand: FILE STATUS, LINAGE, RELATIVE KEY and the other file clauses; SAME AS), never a procedure-division operand",
            ["DataBinder.Configuration.cs"] = "counts the data items spelled as a constant-name at the constant's own tier (an ambiguity count, no item used)",
            ["OdoResolvePass.cs"] = "a data-division clause operand (OCCURS DEPENDING ON data-name-1), never a procedure-division operand",
            ["OccursKeyResolvePass.cs"] = "a data-division clause operand (OCCURS KEY data-name-2), never a procedure-division operand",
            ["ResolveRedefinesPass.cs"] = "data-division clause operands (RENAMES data-name-2 and data-name-3), never a procedure-division operand",
            ["SortBinder.cs"] = "a SORT table key is identified within data-name-2 (§2023:14.9.40.3 SR14a), which ReferenceResolver.ResolveTableOperand identified and screened, so the key lies in a record already screened",
            ["SymbolTable.DataNames.cs"] = "the identification itself (IdentifyDataName); its callers are the files this test reads",
            ["SymbolTable.ConditionNames.cs"] = "the condition-name identification's existence question (DeclaresConditionName), no item used",
            ["SymbolTable.Declarations.cs"] = "the declaration questions of the data division, answered as facts, no item used",
        };
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding");
        // Every member that answers the data items a written data-name names, wherever a binder file reaches it: the procedure
        // division's verdict, the table's identification beneath it, and the clause, region and record sets beside it (review R1-3
        // of step 6b's remainder slice 1: matching DataItemNamed alone let a file call ctx.Symbols.IdentifyDataName, or the qualified
        // set, for a procedure-division operand unseen).
        var lookup = new Regex(@"\.(DataItemNamed|IdentifyDataName|IdentifyEntryClauseOperand|IdentifyRegionOperand|IdentifyWithin)\(");
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(file);
            if (name == "NameResolution.cs") continue;   // the definitions and FindReportedItem itself
            string code = Code(File.ReadAllText(file));
            if (!lookup.IsMatch(code)) continue;
            bool asksScreen = code.Contains("ScreenLinkageReference(", StringComparison.Ordinal)
                              || code.Contains("FindReportedItem(", StringComparison.Ordinal)
                              // the data-name Identify; SymbolTable.Identify of another name class (a file-name, a cd-name) screens nothing
                              || Regex.IsMatch(code, @"(?<!Symbols\.)\bIdentify\(");
            if (noProcedureReference.ContainsKey(name))
            {
                Assert.False(asksScreen, $"{name} is named as a file whose lookups are no procedure-division reference, yet it asks the linkage screen: drop it from the list.");
                continue;
            }
            Assert.True(asksScreen,
                $"{name} looks a data-name up (DataItemNamed / IdentifyDataName / IdentifyEntryClauseOperand / IdentifyRegionOperand / IdentifyWithin) and neither asks "
                + "NameResolution.ScreenLinkageReference nor looks the item up through FindReportedItem / Identify: a LINKAGE item the "
                + "procedure division may not reference (§1985:X.5.1 GR4, §2023:13.7.3 SR4) would compile through it as a subscript or "
                + "reference-modification operand did (kb/Work PB5846). Ask the screen, or name the file in the closed list with why.");
        }
    }

    [Fact]
    public void ASumCounterIsCountedWithTheOrdinaryDataItemsOfItsSpelling()
    {
        string body = Code(BodyOf("internal (ReportModel Report, ReportSumFamily Family)? SumCounterFamilyFor("));
        // The counters (`matches`) and the ordinary candidates the SAME qualifiers reach in the SAME source element are
        // ONE count: the ambiguity verdict is asked of their sum, never of the counters alone.
        Assert.Matches(@"matches\.Count\s*\+\s*data\.Symbols\.IdentifyDataName\(written,", body);
        Assert.Matches(@"DeclaringDepth\(item\)\s*==\s*counterDepth", body);
        Assert.Matches(@"if\s*\(\s*sameSpelling\s*>\s*1\s*\)", body);
        Assert.DoesNotMatch(@"if\s*\(\s*matches\.Count\s*>\s*1\s*\)", body);
    }

    [Fact]
    public void TheTableAllArgument_AsksTheSameCounterResolver()
    {
        // The other arm of the same dispatch: FUNCTION SUM(counter(ALL)) takes its counter from SumCounterFamilyFor too, so
        // it inherits the counted-together verdict and keeps no candidate set of its own.
        string text = File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"),
            "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));
        Assert.Contains("ctx.Refs.Counters.SumCounterFamilyFor(dref, report: true)", text, StringComparison.Ordinal);
        // ... and its table lookup is the REPORTED one (the §2023:8.4.2.2 report and the §2023:13.7.3 SR4 screen), not the bare verdict (DataItemNamed).
        Assert.Contains("ctx.Refs.Names.FindReportedItem(dref, named)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Names.DataItemNamed(", text, StringComparison.Ordinal);
    }

    /// <summary>The two references that name a TABLE rather than a place — the SEARCH / SORT table operand and the
    /// table(ALL) intrinsic argument — look their item up through <c>FindReportedItem</c>, which reports an unidentified
    /// name and screens a LINKAGE item the procedure division may not reference (§2023:13.7.3 SR4). A bare
    /// lookup (<c>DataItemNamed</c>) there is the pre-PB3287 shape: <c>SEARCH L-TABLE</c> compiled clean over such a record.</summary>
    [Fact]
    public void TheTableOperand_LooksItsItemUpThroughTheReportedLookup()
    {
        string body = Code(BodyOf("public TableOperand? ResolveTableOperand("));
        Assert.Contains("FindReportedItem(dref, named)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("DataItemNamed(", body, StringComparison.Ordinal);
        string reported = Code(BodyOf("internal DataItem? FindReportedItem("));
        Assert.Contains("ReportUnidentified(", reported, StringComparison.Ordinal);
        Assert.Contains("ScreenLinkageReference(", reported, StringComparison.Ordinal);
    }
}
