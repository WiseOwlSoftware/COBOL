// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>One name slot of a rule: the ATN state its transition leaves and what it crosses into (a rule's name, or a
/// token's symbolic name).</summary>
internal sealed record NameSlot(int State, string Target);

/// <summary>A rule whose classified label fields do not equal its name slots one for one, element by element: <see cref="Slots"/>
/// are the slot transitions its ATN crosses, <see cref="Fields"/> the distinct label fields with a classifying prefix its generated
/// context classes hold, each with the element it labels (<see cref="GrammarNameSlots.Analysis.LabelledElementsOf"/>).</summary>
internal sealed record NameSlotFinding(string Rule, IReadOnlyList<NameSlot> Slots, IReadOnlyList<string> Fields)
{
    public override string ToString() =>
        $"{Rule}: {Slots.Count} name slot(s) [{string.Join(", ", Slots.Select(s => $"{s.Target}@{s.State}"))}] but "
        + $"{Fields.Count} classified label field(s) [{string.Join(", ", Fields)}]";
}

/// <summary>
/// The name slots of a generated ANTLR parser, read from its ATN, and the check that each carries its own classifying
/// label (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Guards"). Nothing here lists a slot or a rule: the
/// slots, the name rules and the reference rules are all computed from the ATN, and the labels are read off the
/// generated context classes.
/// <list type="bullet">
///   <item>A SLOT is a transition into <c>cobolWord</c>, <c>reservedGatedWord</c>, a NAME RULE or a REFERENCE RULE, or a
///     token that is an ALTERNATIVE OF A SLOT: a one-element alternative of a block one of whose other alternatives is
///     one transition into a name rule (the <c>INTEGERLIT</c> of <c>procedureName</c>).</item>
///   <item>The NAME RULES are the closure from <c>cobolWord</c> and <c>reservedGatedWord</c>: a rule joins when every
///     path from its start state to its stop state crosses exactly one transition into the set (or one alternative-of-a-
///     slot token) and nothing else.</item>
///   <item>A REFERENCE RULE is a rule that is not a name rule some path of which BEGINS with a transition into a name
///     rule or an alternative-of-a-slot token: its HEAD, whose class a use-site label gives (R5-2). <c>procedureName</c>
///     and <c>dataReference</c> are reference rules; a rule whose paths begin with a keyword is not.</item>
///   <item>Each slot transition is one ELEMENT OCCURRENCE, so N, the count of a rule's slot transitions, counts a slot
///     in a loop once and two alternatives' slots twice. M is the count of distinct label fields of the rule's context
///     class (and the classes of its labelled alternatives) whose name has a classifying prefix (<see cref="Prefixes"/>).
///     ANTLR generates one field per label NAME, so a label reused on two slots, or a slot with no label, makes
///     M &lt; N; a classified label on an element that is no slot makes M &gt; N.</item>
///   <item>The comparison is ELEMENT BY ELEMENT, not of the two counts (listing step 7 review lead R1-1): each slot is keyed by
///     the element it crosses into (the rule, or "a token" for an alternative-of-a-slot token) and each classified field by the
///     element its generated type holds (the rule whose context it is, or "a token" for an <c>IToken</c>), and the two multisets
///     must be equal. A classified label left on an element that is no slot can then never hide an unlabelled slot of the same
///     rule, which equal counts would.</item>
/// </list>
/// </summary>
internal static class GrammarNameSlots
{
    /// <summary>The classifying prefixes of a label (section 6.7.1): <c>ref</c> a reference, <c>qual</c> a qualifier,
    /// <c>def</c> a definition, <c>nn</c> a word that names nothing the program declares, <c>name</c> inside a name
    /// rule's own body (the class is the use site's).</summary>
    internal static readonly IReadOnlyList<string> Prefixes = ["ref", "qual", "def", "nn", "name", "lit"];

    /// <summary>The prefix of a LITERAL that names a symbol (a CALL or CANCEL literal-1 names a program): no name rule is its slot, so
    /// the label is the grammar's declaration of it (section 6.7.1, "The program-name literal"), outside the one-for-one comparison of
    /// slots and labels, and the occurrence oracle reads it as a <c>ref</c> slot.</summary>
    internal const string LiteralPrefix = "lit";

    /// <summary>Whether <paramref name="field"/> is a literal-slot label (<see cref="LiteralPrefix"/>).</summary>
    internal static bool IsLiteralSlot(string field) => IsClassified(field) && field.StartsWith(LiteralPrefix, StringComparison.Ordinal);

    /// <summary>The two rules the closure starts from; a parser without them has no name slots to check, which is an
    /// input this check cannot resolve, so it throws.</summary>
    internal static readonly IReadOnlyList<string> BaseRules = ["cobolWord", "reservedGatedWord"];

    /// <summary>Whether <paramref name="field"/> is a name-slot label field: a classifying prefix, a name and an ordinal
    /// (<see cref="GrammarSource.LabelPattern"/>, the one statement of the label's form).</summary>
    internal static bool IsClassified(string field) => GrammarSource.Label().IsMatch(field);

    /// <summary>The analysis of one generated parser type: its ATN, rule names and vocabulary through the members ANTLR's
    /// C# target generates (<c>_ATN</c>, <c>ruleNames</c>, <c>DefaultVocabulary</c>).</summary>
    internal sealed class Analysis
    {
        private readonly ATN _atn;
        private readonly string[] _ruleNames;
        private readonly IVocabulary _vocabulary;
        private readonly HashSet<int> _base;
        private readonly HashSet<int> _nameRules;
        private readonly HashSet<int> _referenceRules;
        private readonly HashSet<Transition> _altOfSlotTokens = new(ReferenceEqualityComparer.Instance);
        private readonly Type _parser;

        public Analysis(Type parser)
        {
            _parser = parser;
            _atn = (ATN)(parser.GetField("_ATN", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                         ?? throw new InvalidOperationException($"{parser.FullName} has no public static _ATN"));
            _ruleNames = (string[])(parser.GetField("ruleNames", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                                    ?? throw new InvalidOperationException($"{parser.FullName} has no public static ruleNames"));
            _vocabulary = (IVocabulary)(parser.GetField("DefaultVocabulary", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                                        ?? throw new InvalidOperationException($"{parser.FullName} has no DefaultVocabulary"));
            _base = [.. BaseRules.Select(RuleIndex)];
            _nameRules = [.. _base];
            // The closure: a rule joins when every path crosses exactly one slot. Alternative-of-a-slot tokens depend on
            // the set, so both are recomputed until nothing changes.
            bool grew = true;
            while (grew)
            {
                grew = false;
                RecomputeAltOfSlotTokens();
                for (int r = 0; r < _ruleNames.Length; r++)
                {
                    if (_nameRules.Contains(r) || !EveryPathCrossesExactlyOneSlot(r)) continue;
                    _nameRules.Add(r);
                    grew = true;
                }
            }
            _referenceRules = [.. Enumerable.Range(0, _ruleNames.Length).Where(r => !_nameRules.Contains(r) && SomePathBeginsWithASlot(r))];
        }

        public IReadOnlyList<string> NameRules => [.. _nameRules.Where(r => !_base.Contains(r)).Select(r => _ruleNames[r]).Order(StringComparer.Ordinal)];

        public IReadOnlyList<string> ReferenceRules => [.. _referenceRules.Select(r => _ruleNames[r]).Order(StringComparer.Ordinal)];

        public int RuleIndex(string rule)
        {
            int i = Array.IndexOf(_ruleNames, rule);
            return i >= 0 ? i : throw new InvalidOperationException($"{_parser.FullName} has no rule '{rule}'");
        }

        /// <summary>The slot transitions of rule <paramref name="rule"/>, one per element occurrence, in state order.</summary>
        public IReadOnlyList<NameSlot> SlotsOf(string rule)
        {
            int r = RuleIndex(rule);
            if (_base.Contains(r)) return [];
            var slots = new List<NameSlot>();
            foreach (ATNState s in _atn.states)
            {
                if (s is null || s.ruleIndex != r) continue;
                for (int i = 0; i < s.NumberOfTransitions; i++)
                {
                    Transition t = s.Transition(i);
                    if (t is RuleTransition rt && (_nameRules.Contains(rt.target.ruleIndex) || _referenceRules.Contains(rt.target.ruleIndex)))
                        slots.Add(new NameSlot(s.stateNumber, _ruleNames[rt.target.ruleIndex]));
                    else if (_altOfSlotTokens.Contains(t))
                        slots.Add(new NameSlot(s.stateNumber, TokenName(t)));
                }
            }
            return slots;
        }

        /// <summary>The distinct classified label fields of rule <paramref name="rule"/>'s generated context class and the
        /// classes of its labelled alternatives, each as <c>Class.field</c>.</summary>
        public IReadOnlyList<string> FieldsOf(string rule)
        {
            Type context = ContextOf(rule);
            return [.. _parser.GetNestedTypes(BindingFlags.Public)
                .Where(t => t == context || t.IsSubclassOf(context))
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(f => IsClassified(f.Name) && !IsLiteralSlot(f.Name))
                    .Select(f => $"{t.Name}.{f.Name}"))
                .Order(StringComparer.Ordinal)];
        }

        /// <summary>The element each distinct classified label field of rule <paramref name="rule"/> labels, read off its generated
        /// type: the rule whose context class the field holds (a list label's element type for <c>+=</c>), or
        /// <see cref="TokenElement"/> for a token label.</summary>
        public IReadOnlyList<string> LabelledElementsOf(string rule)
        {
            Type context = ContextOf(rule);
            return [.. _parser.GetNestedTypes(BindingFlags.Public)
                .Where(t => t == context || t.IsSubclassOf(context))
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(f => IsClassified(f.Name) && !IsLiteralSlot(f.Name))
                .Select(f => ElementOf(f.FieldType))
                .Order(StringComparer.Ordinal)];
        }

        /// <summary>What a slot is keyed by in the element-by-element comparison: the rule it crosses into, or
        /// <see cref="TokenElement"/> for an alternative-of-a-slot token.</summary>
        private string ElementOf(NameSlot slot) => Array.IndexOf(_ruleNames, slot.Target) >= 0 ? slot.Target : TokenElement;

        /// <summary>What a token slot and a token label are both keyed by: ANTLR types every token label <c>IToken</c>.</summary>
        internal const string TokenElement = "(token)";

        private static string ElementOf(Type fieldType)
        {
            Type element = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>)
                ? fieldType.GetGenericArguments()[0]
                : fieldType;
            if (typeof(IToken).IsAssignableFrom(element)) return TokenElement;
            string name = element.Name.EndsWith("Context", StringComparison.Ordinal) ? element.Name[..^"Context".Length] : element.Name;
            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        /// <summary>Every rule whose slots and classified fields are not one for one, element by element.</summary>
        public IReadOnlyList<NameSlotFinding> Findings()
        {
            var findings = new List<NameSlotFinding>();
            foreach (string rule in _ruleNames)
            {
                if (_base.Contains(RuleIndex(rule))) continue;
                IReadOnlyList<NameSlot> slots = SlotsOf(rule);
                IReadOnlyList<string> fields = FieldsOf(rule);
                IEnumerable<string> slotElements = slots.Select(ElementOf).Order(StringComparer.Ordinal);
                if (!slotElements.SequenceEqual(LabelledElementsOf(rule), StringComparer.Ordinal))
                    findings.Add(new NameSlotFinding(rule, slots, fields));
            }
            return findings;
        }

        private Type ContextOf(string rule)
        {
            string name = char.ToUpperInvariant(rule[0]) + rule[1..] + "Context";
            return _parser.GetNestedType(name, BindingFlags.Public)
                   ?? throw new InvalidOperationException($"{_parser.FullName} has no context class {name} for rule '{rule}'");
        }

        private string TokenName(Transition t) => t switch
        {
            AtomTransition a => _vocabulary.GetSymbolicName(a.token) ?? a.token.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => t.Label?.ToString(_vocabulary) ?? t.GetType().Name,
        };

        private static bool IsElement(Transition t) => t is RuleTransition || !t.IsEpsilon;

        private bool IsDirectSlot(Transition t) =>
            (t is RuleTransition rt && _nameRules.Contains(rt.target.ruleIndex)) || _altOfSlotTokens.Contains(t);

        /// <summary>The one element of an alternative that starts at <paramref name="start"/> and ends at
        /// <paramref name="end"/>, or null when it has none or more than one or branches.</summary>
        private static Transition? SingleElement(ATNState start, ATNState end)
        {
            Transition? element = null;
            ATNState s = start;
            for (int guard = 0; guard < 10_000; guard++)
            {
                if (s == end) return element;
                if (s.NumberOfTransitions != 1) return null;
                Transition t = s.Transition(0);
                if (IsElement(t))
                {
                    if (element is not null) return null;
                    element = t;
                    s = t is RuleTransition rt ? rt.followState : t.target;
                }
                else
                {
                    s = t.target;
                }
            }
            return null;
        }

        private void RecomputeAltOfSlotTokens()
        {
            _altOfSlotTokens.Clear();
            foreach (ATNState s in _atn.states)
            {
                if (s is not BlockStartState block || block.NumberOfTransitions < 2) continue;
                var singles = new List<Transition>();
                for (int i = 0; i < block.NumberOfTransitions; i++)
                {
                    Transition? one = SingleElement(block.Transition(i).target, block.endState);
                    if (one is not null) singles.Add(one);
                }
                if (!singles.Any(t => t is RuleTransition rt && _nameRules.Contains(rt.target.ruleIndex))) continue;
                foreach (Transition t in singles.Where(t => t is not RuleTransition))
                    _altOfSlotTokens.Add(t);
            }
        }

        /// <summary>Every path from the rule's start to its stop crosses exactly one direct slot and no other element.</summary>
        private bool EveryPathCrossesExactlyOneSlot(int rule)
        {
            ATNState stop = _atn.ruleToStopState[rule];
            var seen = new HashSet<(int State, int Count)>();
            var work = new Stack<(ATNState State, int Count)>();
            work.Push((_atn.ruleToStartState[rule], 0));
            bool any = false;
            while (work.Count > 0)
            {
                (ATNState s, int count) = work.Pop();
                if (!seen.Add((s.stateNumber, count))) continue;
                if (s == stop)
                {
                    if (count != 1) return false;
                    any = true;
                    continue;
                }
                for (int i = 0; i < s.NumberOfTransitions; i++)
                {
                    Transition t = s.Transition(i);
                    if (!IsElement(t)) { work.Push((t.target, count)); continue; }
                    if (!IsDirectSlot(t) || count >= 1) return false;
                    work.Push((t is RuleTransition rt ? rt.followState : t.target, count + 1));
                }
            }
            return any;
        }

        /// <summary>Some path of the rule begins with a direct slot (its head).</summary>
        private bool SomePathBeginsWithASlot(int rule)
        {
            var seen = new HashSet<int>();
            var work = new Stack<ATNState>();
            work.Push(_atn.ruleToStartState[rule]);
            while (work.Count > 0)
            {
                ATNState s = work.Pop();
                if (!seen.Add(s.stateNumber)) continue;
                for (int i = 0; i < s.NumberOfTransitions; i++)
                {
                    Transition t = s.Transition(i);
                    if (!IsElement(t)) { work.Push(t.target); continue; }
                    if (IsDirectSlot(t)) return true;
                }
            }
            return false;
        }
    }
}
