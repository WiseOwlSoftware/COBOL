// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ <b>A REFERENCE-MODIFIED MOVE RECEIVER IS CLASSIFIED BY THE SENDER'S CATEGORY, NEVER SHORT-CIRCUITED BY ITS PLACE
/// SHAPE, AND EVERY CHARACTER RECEIVER TAKES THE ONE §14.9.25.4 GR6 a) SENDING CHARACTERS</b> (kb/Work PB3512).
/// <c>MoveClassifier.Kind</c> used to open with <c>if (target is RefModPlace) return MoveKind.RefModSlice;</c>, so every
/// slice took one arm before the sender's category was asked, and that arm stored the sender's raw text: a signed
/// numeric sender moved its operational sign into the slice (<c>MOVE</c> of a PIC S9(4) 2468 into <c>X5(1:5)</c>
/// stored "246H") although ISO §8.4.3.3.4 GR6 makes the slice "an elementary data item without the JUSTIFIED clause"
/// and §14.9.25.4 GR6 a) says "the operational sign is not moved". The wrong answer is silent and legal source
/// produces it, and the next place-shaped kind would inherit it, so the SHAPE is pinned: no <see cref="MoveKind"/>
/// names a place shape, <c>Kind</c> asks no place-shape question of its own, and the GR6 a) sign drop is written in
/// ONE emitter member that both the whole-item and the slice receiving halves call.
/// </summary>
public sealed class RefModMoveClassificationDriftTests
{
    private static string Classifier() => StripComments(File.ReadAllText(
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Bound", "MoveClassifier.cs")));

    private static string MoveEmitter() => StripComments(File.ReadAllText(
        TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs")));

    /// <summary>A kind is the CONVERSION a (sender, receiver) pair takes; WHERE it is written is the emitter's
    /// receiving half. A kind named for a place shape is a kind decided before the sender is asked.</summary>
    [Fact]
    public void MoveKind_NamesNoPlaceShape()
    {
        var shaped = Enum.GetNames<MoveKind>()
            .Where(n => n.Contains("RefMod", StringComparison.Ordinal) || n.Contains("Slice", StringComparison.Ordinal))
            .ToList();
        Assert.True(shaped.Count == 0,
            $"MoveKind has place-shaped member(s) {string.Join(", ", shaped)}: a reference-modified receiver is "
            + "§8.4.3.3.4 GR6's elementary item and is classified by the sender's category like any other (kb/Work PB3512)");
    }

    /// <summary><c>MoveClassifier.Kind</c> never tests the receiver's place type itself: a slice is asked only through
    /// the category readers (<c>IsGroupPlace</c>, <c>ReceivingCategory</c>), which answer §8.4.3.3.4 GR6's unique
    /// data item, so the sender's category is always classified before any answer for a slice.</summary>
    [Fact]
    public void MoveClassifier_Kind_AsksNoPlaceShapeQuestion()
    {
        var kind = Regex.Match(Classifier(), @"public\s+static\s+MoveKind\s+Kind\s*\([^)]*\)(?<b>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(kind.Success, "MoveClassifier.Kind is no longer recognizable");
        Assert.DoesNotMatch(@"\btarget\s+is\s+(?!not\b)\w*Place\b", kind.Groups["b"].Value);
        Assert.Contains("IsGroupSender(source)", kind.Groups["b"].Value);
    }

    /// <summary>The GR6 a) sign drop — <c>OperandText.AsString(…, deSign: true)</c> — is spelled ONCE in the MOVE
    /// emitter, in <c>SendingCharacters</c>, and the slice store reaches the sender only through it.</summary>
    [Fact]
    public void MoveEmitter_CharacterReceivers_TakeTheOneSendingCharacters()
    {
        string src = MoveEmitter();
        int spellings = Regex.Matches(src, @"OperandText\.AsString\(\s*source\s*,\s*num\s*,\s*deSign:\s*true\s*\)").Count;
        Assert.True(spellings == 1,
            $"MoveEmitter spells the §14.9.25.4 GR6 a) sending characters {spellings} time(s); SendingCharacters is the "
            + "ONE place, so a whole item and a slice cannot disagree about the sign (kb/Work PB3512)");
        var slice = Regex.Match(src, @"private\s+string\s+SliceStore\s*\([^)]*\)(?<b>.*?)\n    \};",
            RegexOptions.Singleline);
        Assert.True(slice.Success, "MoveEmitter.SliceStore is no longer recognizable");
        Assert.Contains("SendingCharacters(source)", slice.Groups["b"].Value);
        Assert.DoesNotContain("OperandText.AsString", slice.Groups["b"].Value);
    }

    /// <summary>Comment-stripped source, so a mechanism DISCUSSED in prose is never mistaken for one performed in
    /// code.</summary>
    private static string StripComments(string src) =>
        Regex.Replace(Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
}
