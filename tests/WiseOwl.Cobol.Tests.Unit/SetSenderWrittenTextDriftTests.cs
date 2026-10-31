// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SET DIAGNOSTIC QUOTES ITS SENDING OPERAND AS THE PROGRAMMER WROTE IT (kb/Work PB4209, the SET sibling of PB3961).
/// <para><c>ZeroTokenRewriter</c> gives a bare ZERO in an arithmetic slot the text <c>0</c> on purpose (readers of an
/// arithmetic operand parse that text), and <c>GetText()</c> concatenates tokens with no separators. A SET
/// message that built its sender from <c>amount.GetText()</c> therefore printed <c>SET 'N' TO '0'</c> for
/// <c>SET N TO ZERO</c> and <c>TO 0+1</c> for <c>ZERO + 1</c>. The sender text of the whole SET family
/// (<c>SetSender.Text</c>, which the pointer, program-pointer, function-pointer and object-reference arms print) has
/// ONE producer, <c>SetSenders.Classify</c> / <c>SetSender.OfReference</c>, and the two amounts SetBinder names itself
/// (<c>BindSetTo</c>'s <c>senderText</c>, <c>BindSetUpDown</c>'s <c>ReportNoFormat</c>) read the source extent through
/// <c>DataBinder.WrittenText</c>. This file asserts that no SET sender or amount goes back to <c>GetText()</c>.</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternThisTestKeysOn_FiresOnTheDefect"/> feeds the
/// matcher the exact lines the tree carried and asserts it catches them and passes their repaired forms.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SetSenderWrittenTextDriftTests
{
    private static readonly string[] Files =
    [
        Path.Combine("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "SetBinder.cs"),
        Path.Combine("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "SetSender.cs"),
    ];

    /// <summary>The sending amount (<c>amount</c>) or the sender's data reference (<c>dref</c>, in
    /// <c>SetSender.OfReference</c> and <c>SetSenders.Classify</c>) read by <c>GetText()</c>.</summary>
    private static readonly Regex TokenTextOfSender = new(
        @"(?<![\w.])amount\.GetText\(\)|\(\s*dref\.GetText\(\)\s*,",
        RegexOptions.Compiled);

    [Fact]
    public void NoSetSenderOrAmountIsNamedByItsTokenText()
    {
        var offenders = new List<string>();
        foreach (string rel in Files)
        {
            string[] lines = File.ReadAllLines(TestRepo.Src(rel));
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", System.StringComparison.Ordinal) || code.StartsWith("///", System.StringComparison.Ordinal))
                    continue;
                if (TokenTextOfSender.IsMatch(lines[i])) offenders.Add($"{rel}:{i + 1}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A SET sender or amount is named by GetText(), which prints a rewritten ZERO as '0' and glues the tokens "
            + "of an expression (kb/Work PB4209). Use DataBinder.WrittenText. Sites: " + string.Join(", ", offenders));
    }

    /// <summary>⛔ THE GATE FIRES. The defect half is the text the tree carried; the repaired half is its replacement.</summary>
    [Fact]
    public void ThePatternThisTestKeysOn_FiresOnTheDefect()
    {
        Assert.Matches(TokenTextOfSender, "        string senderText = amount.GetText();");
        Assert.Matches(TokenTextOfSender, "_fmt.ReportNoFormat(recvs, kinds, SetDirections.UpDown, amount.GetText());");
        Assert.Matches(TokenTextOfSender, "SetSender(dref.GetText(), dref, null, null)");
        Assert.Matches(TokenTextOfSender, "new(dref.GetText(), dref, null, null)");
        Assert.DoesNotMatch(TokenTextOfSender, "        string senderText = DataBinder.WrittenText(amount);");
        Assert.DoesNotMatch(TokenTextOfSender, "new(DataBinder.WrittenText(dref), dref, null, null)");
        Assert.DoesNotMatch(TokenTextOfSender, "string text = DataBinder.WrittenText(amount);");
    }
}
