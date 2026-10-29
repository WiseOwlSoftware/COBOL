// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY ALTERNATIVE OF A CALL ARGUMENT IS NAMED BY THE ONE FORMAT-1 NARROWING, OR DECLARED PRINTED BY FORMAT 1
/// (kb/Work PB3996). ISO §14.9.4.2 prints Format 1's USING as <c>[ BY REFERENCE ] { identifier-2 } …</c> or
/// <c>BY CONTENT { identifier-2 } …</c>; Format 2 alone prints BY VALUE, OMITTED, arithmetic-expression-1,
/// boolean-expression-1 and literal-2. The grammar parses the formats' shared <c>callArgument</c> wide, so a
/// Format-2-only alternative reaches a binder arm that binds it. PB130 narrowed those spellings inside each arm,
/// and the two OMITTED arms were missed: <c>CALL "P" USING W OMITTED</c>, <c>USING BY REFERENCE OMITTED</c> and
/// <c>USING BY CONTENT W OMITTED</c> compiled and ran. <c>CallBinder.Format2OnlySpellingOf</c> now asks the question
/// once per argument, before any arm; this test keeps a NEXT alternative from being unnarrowed by default.
/// <para><b>What this test can and cannot see.</b> It is reflection over the generated parser (immune to a rename)
/// plus a source scan of the one classifier over comment-stripped text, so it sees what the classifier NAMES, not what
/// it answers; the behaviour is pinned by the <c>pb3996-call-format1-omitted-*</c> goldens. A new alternative that
/// Format 1 does print is added to the allow-list below WITH the clause that prints it, which is the decision a
/// reviewer then sees.</para>
/// </summary>
public sealed class CallArgumentFormatDriftTests
{
    private const string Classifier = "private Format2OnlySpelling? Format2OnlySpellingOf";

    /// <summary>The alternatives §14.9.4.2 FORMAT 1 prints, per rule context — each is identifier-2 (or an
    /// address-identifier, which §14.9.4.3 SR3 makes one). Every other accessor of these contexts is a Format-2-only
    /// spelling and must be read by the classifier.</summary>
    private static readonly IReadOnlyDictionary<Type, string[]> Format1Prints = new Dictionary<Type, string[]>
    {
        // callArgument: the bare identifier-2 is the arithmeticExpression the binder reduces to its sole reference.
        [typeof(CobolParserCore.CallArgumentContext)] = ["addressIdentifier"],
        // callByReference: `[ BY REFERENCE ] { identifier-2 }`; NULL is read here only to be refused by §14.9.4.3 SR3.
        [typeof(CobolParserCore.CallByReferenceContext)] = ["addressIdentifier", "predefinedNull", "receivingIdentifier"],
        // callByContent: `BY CONTENT { identifier-2 }`; the expression operands are Format 2's.
        [typeof(CobolParserCore.CallByContentContext)] = ["addressIdentifier"],
    };

    /// <summary>The words that introduce a BY phrase — they select an alternative, they are not one.</summary>
    private static readonly HashSet<string> PhraseWords = new(StringComparer.Ordinal)
    {
        "BY", "REFERENCE", "CONTENT", "VALUE",
    };

    private static string CallBinderSource() =>
        StripComments(File.ReadAllText(TestRepo.Src(
            "WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "CallBinder.cs")));

    /// <summary>The parse-node accessors a rule context exposes: one per rule reference or token in its alternatives.
    /// A repeated sub-rule has an array-returning accessor, so both shapes count (kb/Work PB407).</summary>
    private static IEnumerable<string> Accessors(Type context) =>
        context.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => (m.GetParameters().Length == 0
                         && (typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                             || typeof(ITerminalNode).IsAssignableFrom(m.ReturnType)))
                        || (m.ReturnType.IsArray && m.GetParameters().Length == 0
                            && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType.GetElementType()!)))
            .Select(m => m.Name)
            .Where(n => !PhraseWords.Contains(n))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);

    [Fact]
    public void EveryAlternativeOfAnArgument_IsNamedByTheOneClassifier_OrPrintedByFormat1()
    {
        string source = CallBinderSource();
        // callArgument's boolean and arithmetic alternatives reach the classifier through the ONE reduction
        // (ConditionBinder.ReduceArgument, ISO §14.9.4.4 GR8), whose call is the statement that names them.
        string reduction = Regex.Match(source, @"var bareArg = ConditionBinder\.ReduceArgument\([^;]*;").Value;
        Assert.True(reduction.Length > 0, "BindCall no longer reduces the keyword-less argument through "
            + "ConditionBinder.ReduceArgument as `var bareArg = …;` — re-point this test rather than deleting it.");
        string read = MethodBody(source, Classifier) + reduction;

        var unread = new List<string>();
        foreach (var (context, printed) in Format1Prints)
        {
            var accessors = Accessors(context).ToList();
            Assert.True(accessors.Count >= 3, $"{context.Name} exposes fewer than three accessors "
                + $"(found: {string.Join(", ", accessors)}) — the reflection in this test is stale.");
            foreach (string accessor in accessors.Where(a => !printed.Contains(a, StringComparer.Ordinal)))
                if (!Regex.IsMatch(read, $@"\.\s*{Regex.Escape(accessor)}\s*\("))
                    unread.Add($"{context.Name}.{accessor}()");
        }

        Assert.True(unread.Count == 0,
            $"CallBinder.Format2OnlySpellingOf never reads {unread.Count} alternative(s) of the CALL argument grammar: "
            + $"{string.Join(", ", unread)}. A spelling it does not name is bound by whichever arm parses it with no "
            + "Format-1 check at all (kb/Work PB3996: `USING W OMITTED` compiled clean). Classify it as Format 2 only "
            + "(return its refusal) or, if ISO §14.9.4.2 Format 1 prints it, add it to Format1Prints with that clause.");
    }

    [Fact]
    public void TheClassifierRunsOncePerArgument_BeforeAnyArm_AndNoArmNarrowsOnItsOwn()
    {
        string source = CallBinderSource();
        string bind = MethodBody(source, "public BoundStatement BindCall");

        // The question is asked before the first arm of the dispatch…
        int ask = bind.IndexOf("Format2OnlySpellingOf(a, bareArg)", StringComparison.Ordinal);
        int firstArm = bind.IndexOf("if (a.callByReference() is { } byRef)", StringComparison.Ordinal);
        Assert.True(ask >= 0 && firstArm > ask,
            "BindCall must ask Format2OnlySpellingOf(a, bareArg) for every argument BEFORE the dispatch over "
            + "callByReference / callByContent / callByValue / the keyword-less arms (kb/Work PB3996).");

        // …and is the only place Format 1's narrowing is framed: the per-arm copies (PB130's) are gone.
        Assert.DoesNotContain("BareNeedsFormat2", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Format1VendorSpelling", source, StringComparison.Ordinal);
        string narrowing = MethodBody(source, "private bool Format1Narrowing");
        Assert.Equal(Regex.Matches(source, "CallContentOperandFormat").Count,
            Regex.Matches(narrowing, "CallContentOperandFormat").Count);
    }

    /// <summary>The source text of one member, from its signature to its matching closing brace — brace-counted, so a
    /// nested block or a local function cannot end it early. Comments are already stripped by the caller.</summary>
    private static string MethodBody(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{signature}' is no longer in the source — this drift test names a member that "
            + "has been renamed or moved; re-point it rather than deleting it.");
        int open = source.IndexOf('{', at);
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return source[at..(i + 1)];
        }
        throw new InvalidOperationException($"unbalanced braces after '{signature}'");
    }

    /// <summary>Line and block comments removed, so a member merely NAMED in a doc comment is not mistaken for one
    /// the code reads.</summary>
    private static string StripComments(string text) =>
        Regex.Replace(Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\r\n]*", "");
}
