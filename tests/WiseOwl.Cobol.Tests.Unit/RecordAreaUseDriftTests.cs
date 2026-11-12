// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.IO;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ HOW MUCH OF AN OCCURS DEPENDING GROUP A STATEMENT USES OF ITS RECORD AREA IS ASKED OF <c>OperandText.AreaUse</c>, ONCE
/// (kb/Work PB5309, PB5377). §1985:VI.5.8.4 GR3 sizes the part of the table used from the DEPENDING item (GR3a) or, for a group
/// that holds its own DEPENDING item and is a receiving item, at its maximum (GR3b); a READ ... INTO sender is fixed by
/// §1985:VII.3.8.4 GR12 alone. The three arms of that choice are one enum, so the two consumers that name one cannot each
/// re-derive it: the INTO sender reads the area <c>Whole</c>, and RECEIVE's receiving area <c>Receiving</c>.
/// <para>It reads the compiler's comment-stripped sources, so it can see that each consumer names its arm, and that
/// INSPECT reads its operands as the characters an item holds (<c>AsItemCharacters</c>, kb/Work PB5375) and never through the
/// scaled sending image; the goldens <c>pb5309-read-into-odo-first-record</c>, <c>pb5377-receive-into-group-with-own-odo</c> and
/// <c>pb5375-inspect-scaled-numeric-characters</c> cover the behaviour.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RecordAreaUseDriftTests
{
    private static string Code(string relative) =>
        Regex.Replace(Regex.Replace(File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), relative)), @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");

    [Fact]
    public void TheIntoSender_ReadsTheAreaWhole()
    {
        string source = Code(Path.Combine("CodeGen", "Emit", "OperandText.cs"));
        Assert.Matches(@"CurrentRecordImage\s*\(\s*BoundCurrentRecord\s+n\s*\)\s*=>\s*RuntimeApi\.StrWindow\(\s*RecordAreaImage\(\s*n\.Area\s*,\s*use:\s*AreaUse\.Whole\s*\)", source);
    }

    [Fact]
    public void ReceiveInto_FillsTheAreaItReceivesInto()
    {
        string source = Code(Path.Combine("CodeGen", "Verbs", "CommunicationEmitter.cs"));
        Assert.Matches(@"McsReceive\([^;]*RecordAreaImage\(\s*into\s*,\s*use:\s*OperandText\.AreaUse\.Receiving\s*\)", source);
    }

    [Fact]
    public void Inspect_ReadsItsOperandsAsTheCharactersAnItemHolds()
    {
        string source = Code(Path.Combine("CodeGen", "Verbs", "InspectEmitter.cs"));
        Assert.DoesNotMatch(@"OperandText\.AsString\(", source);
        Assert.Equal(2, Regex.Matches(source, @"OperandText\.AsItemCharacters\(").Count);
    }
}
