// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DECLINED FACILITY'S STATEMENT STILL READS ITS IDENTIFIERS (kb/Work PB3568, CLAUDE.md rule 5).
/// <para>The recognize-and-name arms (<c>StatementBinder.BindUnsupportedFacility</c>: VALIDATE, the MCS SEND and
/// RECEIVE) bind to a no-op, and for a long time they bound NOTHING else: an undefined name, a reference-modified
/// VALIDATE operand and a pointer operand compiled clean, and so did an undefined name in the statement list of a
/// SEND's ON EXCEPTION phrase. Declining a facility licenses its BEHAVIOUR to be absent (ISO §4.2.6 ¶3, §4.2.7); the
/// statement's operands are identifiers all the same, and the general identifier rules and VALIDATE's §14.9.50.3
/// syntax rules apply to them. The shape that keeps the NEXT declined statement from repeating it: a declined
/// statement is routed through a <c>BindDeclined…</c> method that reads its operands in
/// <c>DeclinedFacilityBinder</c> first, and the operands the grammar gives each declined rule are a census that
/// fails when a rule grows one.</para>
/// </summary>
public sealed class DeclinedFacilityOperandDriftTests
{
    private static readonly string StatementBinderPath =
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Bound", "StatementBinder.cs");

    private static readonly string DeclinedFacilityBinderPath =
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "DeclinedFacilityBinder.cs");

    private static readonly string GrammarPath =
        TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar", "CobolParserCore.g4");

    /// <summary>The declined statement rules, each with the number of <c>dataReference</c> operands the grammar gives
    /// it and the number <c>DeclinedFacilityBinder</c> resolves. Adding an operand to a rule — or a declined rule —
    /// is an adjudication: either the binder resolves it, or the reason it cannot is written here.</summary>
    private static readonly Dictionary<string, (int InGrammar, int Resolved, string Reason)> Roster =
        new(StringComparer.Ordinal)
        {
            ["validateFacilityStatement"] = (1, 1,
                "VALIDATE { identifier-1 } … — the repeated operand, every one screened against §14.9.50.3 SR1/SR2/SR4/SR5"),
            ["mcsReceiveStatement"] = (3, 3,
                "RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 — the three data references; data-name-1 and -2 are names (ScreenedNames)"),
            ["mcsSendStatement"] = (3, 3,
                "SEND [TO] { literal-1 | message-server-name-1 | data-name-2 } FROM identifier-1 [RETURNING data-name-1] — "
                + "FROM and RETURNING always resolve (RETURNING, a name, after its shape is screened); the TO operand resolves only in Format 2, where it is a data-name-2 "
                + "(§14.9.38.3 SR4): in Format 1 it names a message-server-name-1, 'the name of a message server run unit' "
                + "(SR2), which is no data item, and the grammar's one dataReference alternative cannot tell the two apart, "
                + "so DeclinedFacilityBinder.ClassifySend decides the format first (kb/Work PB4090)"),
        };

    /// <summary>⛔ THE OPERANDS THE GENERAL FORMATS PRINT AS NAMES, each asked its WRITTEN shape by
    /// <c>DeclinedFacilityBinder.ScreenName</c> before it resolves (kb/Work PB4118): a subscript, a reference modifier or a
    /// special register is no name. The census is the other half of <see cref="Roster"/>: an operand there that is a
    /// data-name or a message-server-name (not an identifier) belongs here, so a statement that grows one fails until
    /// the binder screens it or this list says why it need not.</summary>
    private static readonly (string Face, string ClauseConstant)[] ScreenedNames =
    [
        ("SEND TO operand", "SendClause"),             // message-server-name-1 | data-name-2 (§14.9.38.2)
        ("SEND RETURNING data-name-1", "SendClause"),  // Format 1
        ("RECEIVE data-name-1", "ReceiveClause"),      // §14.9.31.2
        ("RECEIVE data-name-2", "ReceiveClause"),
    ];

    [Fact]
    public void EveryNameOperandOfTheDeclinedStatements_IsScreenedForItsWrittenShape()
    {
        string facility = CodeOf(DeclinedFacilityBinderPath);
        foreach (var (face, clauseConstant) in ScreenedNames)
            Assert.True(Regex.IsMatch(facility, @"ScreenName\([^;]*""" + Regex.Escape(face) + @"""[^;]*\b" + clauseConstant + @"\)"),
                $"DeclinedFacilityBinder no longer asks ScreenName about '{face}' with {clauseConstant}: a name printed by the "
                + "general format is refused written as a subscripted, reference-modified or special-register reference, and "
                + "the refusal quotes the ONE clause that prints the operand (kb/Work PB4118, PB4169).");
    }

    /// <summary>⛔ THE TWO CLAUSE CONSTANTS CITE THE CLAUSES THEIR NAMES SAY (kb/Work PB4169): a RECEIVE operand is told
    /// RECEIVE's §14.9.31.2 and a SEND operand SEND's §14.9.38.2, never one sentence naming both.</summary>
    [Fact]
    public void TheClauseConstants_CiteTheirOwnStatementsAndNeverBothInOneSentence()
    {
        string facility = CodeOf(DeclinedFacilityBinderPath);
        Assert.Matches(@"SendClause\s*=\s*""ISO §14\.9\.38\.2""", facility);
        Assert.Matches(@"ReceiveClause\s*=\s*""ISO §14\.9\.31\.2""", facility);
        Assert.DoesNotMatch(@"14\.9\.38\.2[^;]*14\.9\.31\.2|14\.9\.31\.2[^;]*14\.9\.38\.2", facility);
    }

    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    /// <summary>Every recognize-and-name call in the statement binder sits in a <c>BindDeclined…</c> method, which
    /// reads the statement's operands (<c>DeclinedFacility.Bind…</c>) BEFORE naming the facility — so a declined
    /// statement cannot be routed to the no-op without its identifiers being read.</summary>
    [Fact]
    public void EveryRecognizeAndNameCall_SitsInAMethodThatReadsTheOperandsFirst()
    {
        string code = CodeOf(StatementBinderPath);
        var methodHeader = new Regex(@"^\s+(?:private|internal|public)\s+[\w<>\[\],? ]+?\s+(\w+)\s*(?:<[^>]+>)?\s*\(", RegexOptions.Multiline);
        var sites = new List<string>();
        string current = "";
        foreach (string line in code.Split('\n'))
        {
            if (methodHeader.Match(line) is { Success: true } h) current = h.Groups[1].Value;
            if (!line.Contains("BindUnsupportedFacility(", StringComparison.Ordinal) || current == "BindUnsupportedFacility") continue;
            sites.Add(current);
        }

        Assert.True(sites.Count >= 1,
            "the scan found no BindUnsupportedFacility call site — VALIDATE and the MCS SEND and RECEIVE route through it "
            + "(BindDeclinedStatement); the guard would pass vacuously on a renamed method.");
        var offenders = sites.Where(s => !s.StartsWith("BindDeclined", StringComparison.Ordinal)).Distinct().ToList();
        Assert.True(offenders.Count == 0,
            "BindUnsupportedFacility is called outside a BindDeclined… method (" + string.Join(", ", offenders)
            + "): a declined facility's statement must read its identifiers in DeclinedFacilityBinder first "
            + "(kb/Work PB3568).");

        foreach (string method in sites.Distinct())
        {
            var declaration = Regex.Match(code, @"(?:private|internal|public)[^\n]*\b" + method + @"\b\s*[<(]");
            Assert.True(declaration.Success, $"{method} is no longer declared in StatementBinder.cs.");
            int start = declaration.Index;
            int next = Regex.Match(code[(start + 1)..], @"\n    (public|internal|private) ").Index;
            string body = next <= 0 ? code[start..] : code[start..(start + 1 + next)];
            Assert.True(body.Contains("DeclinedFacility.Bind", StringComparison.Ordinal) || body.Contains("bindOperands(", StringComparison.Ordinal),
                $"{method} names a declined facility without reading its operands through DeclinedFacilityBinder.");
        }
    }

    /// <summary>The declined statement rules are the ones the statement binder routes to a <c>BindDeclined…</c> method,
    /// and the operands each gives <c>dataReference</c> match the roster — so a rule that grows an operand fails here
    /// until the binder reads it or the roster says why it cannot.</summary>
    [Fact]
    public void TheDeclinedStatementRules_AndTheirOperands_AreTheRoster()
    {
        string binderCode = CodeOf(StatementBinderPath);
        var routed = Regex.Matches(binderCode, @"s\.(\w+)\(\) is \{ \} \w+ =>\s*BindDeclined\w+\(")
            .Select(m => m.Groups[1].Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(Roster.Keys.OrderBy(x => x, StringComparer.Ordinal), routed);

        string grammar = CodeOf(GrammarPath);
        string facility = CodeOf(DeclinedFacilityBinderPath);
        foreach (var (rule, (inGrammar, resolved, reason)) in Roster)
        {
            var m = Regex.Match(grammar, @"^" + rule + @"\s*\r?\n\s*:(?<body>.*?)\r?\n\s*;", RegexOptions.Multiline | RegexOptions.Singleline);
            Assert.True(m.Success, $"grammar rule '{rule}' not found in CobolParserCore.g4");
            int refs = Regex.Matches(m.Groups["body"].Value, @"\bdataReference\b").Count;
            Assert.True(refs == inGrammar,
                $"{rule} gives {refs} dataReference operand(s) in the grammar, the roster expects {inGrammar} ({reason}). "
                + "Read the new operand in DeclinedFacilityBinder, or record in the roster why it cannot be resolved.");
            Assert.True(resolved <= inGrammar, $"{rule}: the roster resolves more operands than the grammar has.");
        }

        // the binder half: each roster rule has its method over the generated context, so the roster cannot name a
        // rule the binder does not read.
        foreach (string context in new[] { "ValidateFacilityStatementContext", "McsReceiveStatementContext", "McsSendStatementContext" })
            Assert.True(facility.Contains(context, StringComparison.Ordinal),
                $"DeclinedFacilityBinder no longer binds {context}.");
    }
}
