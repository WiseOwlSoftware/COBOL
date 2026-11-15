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
/// ⛔ A FACILITY STATEMENT'S OPERANDS ARE ALL READ (kb/Work PB3568, CLAUDE.md rule 5).
/// <para>While VALIDATE, SEND and RECEIVE were declined they bound to a no-op, and for a long time they bound NOTHING else: an undefined name, a reference-modified
/// VALIDATE operand and a pointer operand compiled clean, and so did an undefined name in the statement list of a
/// SEND's ON EXCEPTION phrase. None of the three is declined now (SEND and RECEIVE: kb/Work PB4700; VALIDATE: kb/Work PB5577), but the census that
/// found the defect still guards them: the operands the grammar gives each facility statement rule are a census that
/// fails when a rule grows one, until the binder reads it or the roster says why it cannot.</para>
/// </summary>
public sealed class DeclinedFacilityOperandDriftTests
{
    private static readonly string StatementBinderPath =
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Bound", "StatementBinder.cs");

    private static readonly string ValidateBinderPath =
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "ValidateBinder.cs");

    private static readonly string MessagingBinderPath =
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "MessagingBinder.cs");

    private static readonly string GrammarPath =
        TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar", "CobolParserCore.g4");

    /// <summary>The statement rules whose operands a binder reads for their identifier rules, each with the number of
    /// <c>dataReference</c> operands the grammar gives it (<c>ValidateBinder</c> for VALIDATE (kb/Work PB5577),
    /// <c>MessagingBinder</c> for SEND and RECEIVE, kb/Work PB4743). Adding an operand to a rule is an adjudication:
    /// the binder reads it and a refusal golden pins it (an undefined name in each RECEIVE and SEND operand:
    /// <c>conformance-suite/wiseowl/cases/14.9.31-RECEIVE</c> and <c>14.9.38-SEND</c>, kb/Work PB3594, PB4700), or the
    /// reason it cannot be read is written here.</summary>
    private static readonly Dictionary<string, (int InGrammar, string Reason)> Roster =
        new(StringComparer.Ordinal)
        {
            ["validateFacilityStatement"] = (1,
                "VALIDATE { identifier-1 } … — the repeated operand, every one screened against §2023:14.9.50.3 SR1/SR2/SR4/SR5"),
            ["messagingReceiveStatement"] = (3,
                "RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 — the three data references; data-name-1 and -2 are names (ScreenedNames)"),
            ["messagingSendStatement"] = (3,
                "SEND [TO] { literal-1 | message-server-name-1 | data-name-2 } FROM identifier-1 [RETURNING data-name-1] — "
                + "FROM and RETURNING always resolve (RETURNING, a name, after its shape is screened); the TO operand resolves only in Format 2, where it is a data-name-2 "
                + "(§2023:14.9.38.3 SR4): in Format 1 it names a message-server-name-1, 'the name of a message server run unit' "
                + "(SR2), which is no data item, and the grammar's one dataReference alternative cannot tell the two apart, "
                + "so MessagingBinder.ClassifySend decides the format first (kb/Work PB4090)"),
        };

    /// <summary>⛔ THE OPERANDS THE GENERAL FORMATS PRINT AS NAMES, each asked its WRITTEN shape by
    /// <c>MessagingBinder.ScreenName</c> before it resolves (kb/Work PB4118): a subscript, a reference modifier or a
    /// special register is no name. The census is the other half of <see cref="Roster"/>: an operand there that is a
    /// data-name or a message-server-name (not an identifier) belongs here, so a statement that grows one fails until
    /// the binder screens it or this list says why it need not.</summary>
    private static readonly (string Face, string ClauseConstant)[] ScreenedNames =
    [
        ("SEND TO operand", "SendClause"),             // message-server-name-1 | data-name-2 (§2023:14.9.38.2)
        ("SEND RETURNING data-name-1", "SendClause"),  // Format 1
        ("RECEIVE data-name-1", "ReceiveClause"),      // §2023:14.9.31.2
        ("RECEIVE data-name-2", "ReceiveClause"),
    ];

    [Fact]
    public void EveryNameOperandOfTheDeclinedStatements_IsScreenedForItsWrittenShape()
    {
        string facility = CodeOf(MessagingBinderPath);
        foreach (var (face, clauseConstant) in ScreenedNames)
            Assert.True(Regex.IsMatch(facility, @"ScreenName\([^;]*""" + Regex.Escape(face) + @"""[^;]*\b" + clauseConstant + @"\)"),
                $"MessagingBinder no longer asks ScreenName about '{face}' with {clauseConstant}: a name printed by the "
                + "general format is refused written as a subscripted, reference-modified or special-register reference, and "
                + "the refusal quotes the ONE clause that prints the operand (kb/Work PB4118, PB4169).");
    }

    /// <summary>⛔ THE TWO CLAUSE CONSTANTS CITE THE CLAUSES THEIR NAMES SAY (kb/Work PB4169): a RECEIVE operand is told
    /// RECEIVE's §2023:14.9.31.2 and a SEND operand SEND's §2023:14.9.38.2, never one sentence naming both.</summary>
    [Fact]
    public void TheClauseConstants_CiteTheirOwnStatementsAndNeverBothInOneSentence()
    {
        string facility = CodeOf(MessagingBinderPath);
        Assert.Matches(@"SendClause\s*=\s*""ISO §2023:14\.9\.38\.2""", facility);
        Assert.Matches(@"ReceiveClause\s*=\s*""ISO §2023:14\.9\.31\.2""", facility);
        Assert.DoesNotMatch(@"14\.9\.38\.2[^;]*14\.9\.31\.2|14\.9\.31\.2[^;]*14\.9\.38\.2", facility);
    }

    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    /// <summary>The facility statement rules are the ones the statement binder binds from their introduction (<c>BindIntroducedAt</c>: VALIDATE, kb/Work PB5577; SEND and RECEIVE, kb/Work PB4744),
    /// and the operands each gives <c>dataReference</c> match the roster — so a rule that grows an operand fails here
    /// until the binder reads it or the roster says why it cannot.</summary>
    [Fact]
    public void TheDeclinedStatementRules_AndTheirOperands_AreTheRoster()
    {
        string binderCode = CodeOf(StatementBinderPath);
        var routed = Regex.Matches(binderCode, @"s\.(\w+)\(\) is \{ \} \w+ =>\s*BindIntroducedAt\(")
            .Select(m => m.Groups[1].Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(Roster.Keys.OrderBy(x => x, StringComparer.Ordinal), routed);

        string grammar = CodeOf(GrammarPath);
        string facility = CodeOf(ValidateBinderPath) + CodeOf(MessagingBinderPath);
        foreach (var (rule, (inGrammar, reason)) in Roster)
        {
            var m = Regex.Match(grammar, @"^" + rule + @"\s*\r?\n\s*:(?<body>.*?)\r?\n\s*;", RegexOptions.Multiline | RegexOptions.Singleline);
            Assert.True(m.Success, $"grammar rule '{rule}' not found in CobolParserCore.g4");
            int refs = Regex.Matches(m.Groups["body"].Value, @"\bdataReference\b").Count;
            Assert.True(refs == inGrammar,
                $"{rule} gives {refs} dataReference operand(s) in the grammar, the roster expects {inGrammar} ({reason}). "
                + "Read the new operand in ValidateBinder or MessagingBinder, or record in the roster why it cannot be resolved.");
        }

        // the binder half: each roster rule has its method over the generated context, so the roster cannot name a
        // rule the binder does not read.
        foreach (string context in new[] { "ValidateFacilityStatementContext", "MessagingReceiveStatementContext", "MessagingSendStatementContext" })
            Assert.True(facility.Contains(context, StringComparison.Ordinal),
                $"neither ValidateBinder nor MessagingBinder binds {context}.");
    }
}
