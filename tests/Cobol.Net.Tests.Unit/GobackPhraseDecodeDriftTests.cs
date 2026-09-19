// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A STATEMENT WHOSE PARSE NODE IS DECODED TWICE, ONCE PER DISPATCH ARM, LOSES A PHRASE ON ONE OF THEM. That
/// is this repository's most reproducible defect shape, and <c>gobackStatement</c> is where it was measured:
/// <c>CallBinder.BindGoback</c> forked to <c>OoBinder.OoBindMethodGoback</c> when the statement was inside a
/// METHOD, handing the whole <c>GobackStatementContext</c> to a second binder that re-decided which phrases
/// existed — and read only <c>dataReference()</c> and <c>raisingPhrase()</c>. The COBOL-2023 status phrase was
/// discarded in silence, so ISO §14.9.18.3 SR6/SR7/SR8 and the 2023 introduction gate never ran in a method
/// (kb/Work PB411: <c>GOBACK WITH ERROR STATUS ""</c> inside a method compiled and ran clean at --std 2023).
/// <para>The repair was structural: <c>CallBinder.DecodeGobackPhrases</c> reads the rule ONCE, before the
/// §14.9.18.4 GR2/GR4 fork, and the method arm now takes the decoded <c>GobackPhrases</c> instead of the parse
/// node. These three facts are what keep that true when the grammar gains its next phrase.</para>
/// <para><b>What this test can and cannot see.</b> Facts 1 and 2 are source scans over comment-stripped text,
/// so they see what the code NAMES, not what it does with it; fact 3 is reflection over the generated parser and
/// cannot be fooled by a rename. Together they catch the exact drift that occurred — a sub-rule added to
/// <c>gobackStatement</c> that the decode never reads, or an arm that goes back to reading the parse node.</para>
/// </summary>
public sealed class GobackPhraseDecodeDriftTests
{
    private static string CallBinderSource() =>
        StripComments(File.ReadAllText(TestRepo.Src(
            "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "CallBinder.cs")));

    private static string OoBinderSource() =>
        StripComments(File.ReadAllText(TestRepo.Src(
            "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs")));

    /// <summary>The sub-rule accessors the generated <c>GobackStatementContext</c> exposes — one per rule
    /// reference in <c>gobackStatement</c>'s grammar alternative. ANTLR names each accessor after the rule, so
    /// this set IS "every phrase the grammar can put on a GOBACK".
    /// <para>⛔ A REPEATED sub-rule counts too, and its accessor has a DIFFERENT SHAPE: for
    /// <c>(raisingPhrase | statusPhrase)*</c> ANTLR emits <c>RaisingPhraseContext[] raisingPhrase()</c> plus an
    /// indexed <c>raisingPhrase(int)</c>, neither of which is a zero-argument <c>ParserRuleContext</c> return.
    /// Matching only the singular shape made this probe BLIND to exactly the phrases §14.9.18.2's choice
    /// indicators put on the statement (kb/Work PB407) — the element type is what the set is keyed on, so the
    /// probe answers the same question whether a phrase is written once or repeated.</para></summary>
    private static IReadOnlyList<string> SubRuleAccessors() =>
        typeof(CobolParserCore.GobackStatementContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0
                        && (typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                            || (m.ReturnType.IsArray
                                && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType.GetElementType()!))))
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void EverySubRuleOfGobackStatement_IsReadByTheOneDecode()
    {
        var accessors = SubRuleAccessors();
        Assert.True(accessors.Count >= 3,
            "gobackStatement exposes fewer sub-rule accessors than the three phrases §14.9.18.2 gives it "
            + $"(found: {string.Join(", ", accessors)}) — the reflection in this test is stale.");

        string body = MethodBody(CallBinderSource(), "private GobackPhrases DecodeGobackPhrases");
        var missing = accessors.Where(a => !Regex.IsMatch(body, $@"\.\s*{Regex.Escape(a)}\s*\(")).ToList();

        Assert.True(missing.Count == 0,
            $"CallBinder.DecodeGobackPhrases never reads {missing.Count} sub-rule(s) of gobackStatement: "
            + $"{string.Join(", ", missing)}. A phrase the ONE decode does not read is a phrase BOTH arms of the "
            + "§14.9.18.4 GR2/GR4 fork silently ignore — its syntax rules and its edition gate stop running "
            + "(kb/Work PB411). Decode it and give it a GobackPhrases member.");
    }

    [Fact]
    public void TheMethodArm_NeverReadsTheGobackParseNode()
    {
        string oo = OoBinderSource();
        Assert.DoesNotContain("GobackStatementContext", oo, StringComparison.Ordinal);

        // …and the decode is reached before the fork, not after it: the fork line hands the DECODED record on.
        string bind = MethodBody(CallBinderSource(), "public BoundStatement BindGoback");
        Assert.Matches(@"DecodeGobackPhrases\s*\(\s*g\s*\)", bind);
        Assert.Matches(@"host\.Oo\.OoBindMethodGoback\s*\(\s*p\s*\)", bind);
    }

    [Fact]
    public void EveryDecodedPhrase_IsConsumedByAnArmOfTheFork()
    {
        Type phrases = typeof(CobolNet.Binding.Bound.BoundGoback).Assembly
            .GetTypes().Single(t => t.Name == "GobackPhrases");
        string program = MethodBody(CallBinderSource(), "public BoundStatement BindGoback");
        string method = MethodBody(OoBinderSource(), "public BoundStatement OoBindMethodGoback");

        var unconsumed = phrases.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(n => !Regex.IsMatch(program, $@"\bp\s*\.\s*{Regex.Escape(n)}\b")
                        && !Regex.IsMatch(method, $@"\bp\s*\.\s*{Regex.Escape(n)}\b"))
            .ToList();

        Assert.True(unconsumed.Count == 0,
            $"GobackPhrases declares {unconsumed.Count} decoded phrase(s) no arm of the fork reads: "
            + $"{string.Join(", ", unconsumed)}. A phrase decoded and then dropped runs as though it had not "
            + "been written.");
    }

    /// <summary>The source text of one method, from its signature to its matching closing brace — brace-counted,
    /// so a nested block or a local function cannot end it early. Comments are already stripped by the caller.</summary>
    private static string MethodBody(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{signature}' is no longer in the source — this drift test names a method that "
            + "has been renamed or moved; re-point it rather than deleting it.");
        int open = source.IndexOf('{', at);
        int arrow = source.IndexOf("=>", at, StringComparison.Ordinal);
        if (arrow >= 0 && (open < 0 || arrow < open))   // an expression-bodied member ends at its semicolon
            return source[at..(source.IndexOf(';', arrow) + 1)];
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return source[at..(i + 1)];
        }
        throw new InvalidOperationException($"unbalanced braces after '{signature}'");
    }

    /// <summary>Line and block comments removed, so a member merely NAMED in a doc comment (this subject is
    /// discussed at length in both files) is not mistaken for one the code reads.</summary>
    private static string StripComments(string text) =>
        Regex.Replace(Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\r\n]*", "");
}
