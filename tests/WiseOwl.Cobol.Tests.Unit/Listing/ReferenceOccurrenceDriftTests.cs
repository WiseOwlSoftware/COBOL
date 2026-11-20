// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Symbols;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ THE SYMBOL TABLE'S OCCURRENCES ARE COMPLETE AND TRUE (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Guards"): the two
/// invariants the cross-reference stands on, held with no list of resolve paths.
/// <list type="bullet">
/// <item><b>Completeness, independent of path.</b> Every program of the corpus is parsed and bound with its references recorded, its parse
/// tree is walked, and each name slot's class is read from the label the grammar gives it (step 7a's labels, the element label's PREFIX):
/// each <c>ref</c> node has EXACTLY one occurrence at its first token's position, each <c>def</c> node's name is an entry of the table,
/// and a <c>qual</c>, <c>nn</c> or <c>name</c> node has none of its own. A reference rule's head slot is the occurrence of the
/// label on the transition INTO the rule (R5-2), so it is counted once, there. Every occurrence recorded stands at a <c>ref</c> node: a
/// resolve path that reaches a symbol without the table, a double record and a position recorded wrongly are each red, whatever the path.
/// A slot whose class depends on resolution (a function-name that may be an intrinsic, a RAISING word that may be an exception-name, an
/// ASSIGN target that may be a device-name; kb/Work PB5925's lead <c>2026-11-18-resolution-dependent-slots</c>) counts only when the word
/// identified a symbol: there an occurrence is admitted, never demanded.</item>
/// <item><b>Pairing.</b> Every store position a statement's written operands occupy (<c>BoundStores.StorePositionsOf</c>) pairs with an
/// occurrence of that statement, through the place instance the reference's builder linked to it (<c>EntryStore.UnpairedStorePositions</c>
/// is empty), so the day a binder rebuilds a linked place by value the pairing is red.</item>
/// </list>
/// <para>The corpus is every NIST CCVS program the golden run compiles (a green or divergent row of <c>tests/nist/corpus.tsv</c>), at its
/// own edition, as <c>SequencePointCoverageDriftTests</c> reads it: every verb of every 1985 module. The test is in the Unit assembly,
/// which reads the generated parser's label fields (as <c>GrammarNameSlotDriftTests</c> does) and the driver's internal front end.</para>
/// </summary>
public sealed class ReferenceOccurrenceDriftTests
{
    private static readonly GrammarNameSlots.Analysis Grammar = new(typeof(CobolParserCore));
    private static readonly HashSet<string> ReferenceRules = [.. Grammar.ReferenceRules];

    /// <summary>The rules whose slot's class depends on resolution (section 6.7.1, "Where the class depends on resolution").</summary>
    private static readonly HashSet<string> ResolutionDependent = ["functionName", "raisingTarget", "assignTarget", "rerunTarget"];

    /// <summary>One written name slot: where it stands (the resultant line and column of its first token), its class prefix, the rule
    /// that holds it and its label, and whether an occurrence is only admitted there.</summary>
    internal readonly record struct Slot(int Line, int Column, string Prefix, string Rule, string Label, bool Optional, string Word);

    /// <summary>Whether a labelled node writes no name: the alternative its rule matched is an element labelled <c>nn</c> (a literal
    /// beside a constant-name, a class word beside a class-name, a keyword beside a file-name), or it is a special register's reference
    /// (<c>LINAGE-COUNTER</c>, <c>LINE-COUNTER</c>, <c>PAGE-COUNTER</c>: a dataReference whose word is its qualifier, section 6.7.1's
    /// "A reference that writes no name is no name").</summary>
    private static bool NamesNothing(ParserRuleContext node)
    {
        if (node.Start.Type is CobolParserCore.LINAGE_COUNTER or CobolParserCore.LINE_COUNTER or CobolParserCore.PAGE_COUNTER) return true;
        // A reference rule writes a name only when the path it matched BEGINS with its head slot (an integer operand's literal, a USE
        // phrase's INPUT keyword is no name).
        if (ReferenceRules.Contains(CobolParserCore.ruleNames[node.RuleIndex]))
            return !LabelFields(node.GetType()).Any(f => PrefixOf(f.Name) is "ref" or "name"
                && f.GetValue(node) switch { ParserRuleContext c => c.Start.TokenIndex, IToken t => t.TokenIndex, _ => -1 } == node.Start.TokenIndex);
        foreach (var field in LabelFields(node.GetType()))
            if (PrefixOf(field.Name) == "nn"
                && field.GetValue(node) switch { ParserRuleContext c => c.Start.TokenIndex, IToken t => t.TokenIndex, _ => -1 } == node.Start.TokenIndex)
                return true;
        // A name rule whose matched element is itself a name rule: ask the element.
        return node.ChildCount == 1 && node.GetChild(0) is ParserRuleContext only && NamesNothing(only);
    }

    /// <summary>The word a labelled node writes first (its first token's text).</summary>
    private static string Word(ParserRuleContext node) => node.Start.Text;

    /// <summary>Every classified slot of <paramref name="tree"/>, read from the labels.</summary>
    internal static List<Slot> Slots(IParseTree tree)
    {
        var slots = new List<Slot>();
        Walk(tree, slots);
        return slots;
    }

    private static void Walk(IParseTree node, List<Slot> slots)
    {
        if (node is not ParserRuleContext context) return;
        string rule = CobolParserCore.ruleNames[context.RuleIndex];
        foreach (var field in LabelFields(context.GetType()))
        {
            string prefix = PrefixOf(field.Name);
            if (prefix is "name" or "qual" or "nn") continue;
            switch (field.GetValue(context))
            {
                case ParserRuleContext labelled:
                    // A reference rule's head is the occurrence of the label on the transition into the rule (R5-2), not its own.
                    if (prefix == "ref" && ReferenceRules.Contains(rule) && labelled.Start.TokenIndex == context.Start.TokenIndex) break;
                    // A label in a loop holds its last iteration; the earlier ones are the siblings the same ATN state invoked.
                    foreach (var child in context.children.OfType<ParserRuleContext>().Where(c => c.invokingState == labelled.invokingState))
                        if (!NamesNothing(child))
                            slots.Add(new Slot(child.Start.Line, child.Start.Column, prefix, rule, field.Name,
                                ResolutionDependent.Contains(CobolParserCore.ruleNames[child.RuleIndex]), Word(child)));
                    break;
                case IToken token:
                    if (prefix == "ref" && ReferenceRules.Contains(rule) && token.TokenIndex == context.Start.TokenIndex) break;
                    slots.Add(new Slot(token.Line, token.Column, prefix, rule, field.Name, false, token.Text));
                    break;
            }
        }
        foreach (var child in context.children ?? [])
            Walk(child, slots);
    }

    private static readonly Dictionary<Type, FieldInfo[]> s_fields = [];

    private static FieldInfo[] LabelFields(Type context)
    {
        lock (s_fields)
        {
            if (!s_fields.TryGetValue(context, out var fields))
                s_fields[context] = fields = [.. context.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => GrammarNameSlots.IsClassified(f.Name))];
            return fields;
        }
    }

    private static bool IsFiller(string word) => WiseOwl.Cobol.Runtime.CobolNames.UpperFold(word) == "FILLER";

    private static string PrefixOf(string label) => GrammarNameSlots.Prefixes.First(p => label.StartsWith(p, StringComparison.Ordinal));

    /// <summary>Parse and bind <paramref name="options"/>'s source as the driver does, its references recorded: the tree, the occurrences
    /// (each with its entry's class and name), the entries' positions, the unpaired store positions, and the bind's errors.</summary>
    internal static (CobolParserCore.CompilationUnitContext Tree, List<(SymbolOccurrence Occurrence, NameClass Class, string Name)> Occurrences,
        HashSet<string> EntryLines, IReadOnlyList<(SymbolPosition Statement, string Verb, string Operand)> Unpaired, IReadOnlyList<string> Errors)
        Bind(CompilerDriver.Options options)
    {
        var frontend = CompilerDriver.NewFrontend(options, new Frontend.CompilationInputs(), recordsListing: false);
        var diagnostics = new DiagnosticBag();
        var tree = frontend.Parse(options.SourcePath, diagnostics);
        Assert.False(diagnostics.HasErrors, string.Join("\n", diagnostics.Diagnostics));
        Assert.NotNull(tree);
        var edition = new EditionContext(options.DialectLevel, options.Permissive) { RecordsReferences = true, VerifiesStorePairing = true };
        var binder = new BinderDriver();
        binder.Bind(tree!, edition, frontend.Directives);
        var store = binder.Session.OoClasses.Entries;
        var occurrences = store.Occurrences().Select(o => (o.Occurrence, store[o.Entry].Class, store[o.Entry].Name)).ToList();
        var entryLines = new HashSet<string>(StringComparer.Ordinal);
        for (int id = 1; id <= store.Count; id++) entryLines.Add(WiseOwl.Cobol.Runtime.CobolNames.UpperFold(store[new SymbolId(id)].Name));
        return (tree!, occurrences, entryLines, store.UnpairedStorePositions, edition.Diagnostics);
    }

    /// <summary>The completeness oracle's verdict on one program: each <c>ref</c> slot with no occurrence, each occurrence at no
    /// <c>ref</c> slot, each <c>def</c> slot on a line no entry is declared on, and each unpaired store position.</summary>
    internal static List<string> Findings(CompilerDriver.Options options)
    {
        var (tree, occurrences, entryLines, unpaired, errors) = Bind(options);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        var slots = Slots(tree);
        var findings = new List<string>();
        var recorded = occurrences.GroupBy(o => (o.Occurrence.At.Line, o.Occurrence.At.Column)).ToDictionary(g => g.Key, g => g.ToList());
        var refs = slots.Where(s => s.Prefix is "ref" or GrammarNameSlots.LiteralPrefix).GroupBy(s => (s.Line, s.Column)).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var (at, slot) in refs)
            if (!recorded.ContainsKey(at) && !slot.All(s => s.Optional))
                findings.Add($"ref slot {slot[0].Rule}.{slot[0].Label} at {at.Line}:{at.Column} has no occurrence");
        foreach (var (at, found) in recorded)
            if (!refs.ContainsKey(at))
                findings.Add($"occurrence of {found[0].Class} '{found[0].Name}' at {at.Line}:{at.Column} stands at no ref slot");
        // A definition's entry stands where section 6.6 places it (a data entry at its level-number, an index-name on its table's line,
        // a program at its first token), so a def slot is held to its NAME: the table entered a name of that spelling.
        foreach (var slot in slots.Where(s => s.Prefix == "def" && !entryLines.Contains(WiseOwl.Cobol.Runtime.CobolNames.UpperFold(s.Word.Trim('"', '\''))) && !IsFiller(s.Word)))
            findings.Add($"def slot {slot.Rule}.{slot.Label} '{slot.Word}' at {slot.Line}:{slot.Column} entered no entry of its name");
        foreach (var (statement, verb, operand) in unpaired)
            findings.Add($"{verb} at {statement.Line}:{statement.Column} stores into '{operand}' through a place linked to no occurrence of it");
        return findings;
    }

    [Theory]
    [MemberData(nameof(SequencePointCoverageDriftTests.NistPrograms), MemberType = typeof(SequencePointCoverageDriftTests))]
    public void EveryWrittenReferenceIsRecorded_OnceAtItsPosition_AndEveryStorePairs(string program)
    {
        var options = new CompilerDriver.Options(TestRepo.Nist("programs", program + ".cob"),
            Path.Combine(Path.GetTempPath(), "wiseowl-xref-oracle", program + ".dll"), NistTestName: program, DialectLevel: 85);
        var findings = Findings(options);
        Assert.True(findings.Count == 0, $"{program}: {findings.Count} findings\n" + string.Join("\n", findings.Take(30)));
    }
}
