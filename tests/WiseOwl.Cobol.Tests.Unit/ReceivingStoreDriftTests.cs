// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB871 — the ELEMENTARY CHARACTER RECEIVING STORE lives in ONE place, <c>CodeGen/Verbs/ReceivingStore.cs</c>.
/// ISO §8.5.1.10.4 is written over "a receiving operand", not over MOVE: every verb that transfers "according to the
/// rules for the MOVE statement" owes a dynamic-length receiver its whole new value. The defect was that the
/// dynamic arm existed only inside <c>MoveEmitter</c>, and UNSTRING and ACCEPT each built the fixed-width arm
/// themselves — at the PICTURE's one position (§13.18.19.3 SR1). This pins the structure, not a case: an emitter
/// under <c>CodeGen/Verbs</c> that builds either store directly is a new private copy of the rule.
/// </summary>
public sealed class ReceivingStoreDriftTests
{
    /// <summary>The one file allowed to spell the two stores. Deliberately NO other entry: <c>OoEmitter</c>'s BY
    /// CONTENT boolean argument copy was the last private copy, and it is "the MOVE store for the formal's
    /// category" by its own comment (§14.8.2.3.3 rule 2d), so it calls the home too.</summary>
    private static readonly HashSet<string> Allowed = new() { "ReceivingStore.cs" };

    [Fact]
    public void NoVerbEmitter_BuildsTheElementaryCharacterStoreItself()
    {
        var dir = TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs");
        var rx = new Regex(@"RuntimeApi\.(StrStoreAligned|DynStore)\(");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            string name = Path.GetFileName(file);
            if (Allowed.Contains(name)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (rx.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//"))
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0,
            "An emitter builds the elementary character receiving store itself instead of calling "
            + "ReceivingStore.Characters — a dynamic-length receiver would take the PICTURE's width there "
            + "(ISO §8.5.1.10.4; kb/Work PB871):\n" + string.Join("\n", offenders));
    }

    /// <summary>kb/Work PB3512 (branch review R1-1) — every USAGE decision in the receiving store reads the item's
    /// OPERAND picture (<c>DataItem.OperandPic</c>), never its raw <c>Pic</c>. ISO §14.9.25.4 GR4: "Bit group items
    /// and national group items are treated as elementary items in the MOVE statement", and a GROUP-USAGE NATIONAL /
    /// BIT group's usage lives only on its as-if picture (§13.18.29.4 GR1 b) / GR2 b)). The storage readers used to
    /// switch on raw <c>Pic</c>, which is null for such a group, so a group MOVE into a national group (whole or
    /// reference-modified) took the sending bytes as its characters. Golden:
    /// <c>pb3512_group_sender_national_bit_slice</c>.</summary>
    [Fact]
    public void TheReceivingStore_AsksUsageOfTheOperandPicture_NeverTheRawPicture()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "ReceivingStore.cs"));
        var rawUsage = new Regex(@"(?<![A-Za-z])Pic\s+(switch|is\s+(not\s+)?\{[^}]*Usage)");
        var offenders = new List<string>();
        int operandReads = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("//")) continue;
            if (rawUsage.IsMatch(lines[i])) offenders.Add($"ReceivingStore.cs:{i + 1}: {lines[i].Trim()}");
            if (lines[i].Contains("OperandPic switch", StringComparison.Ordinal)
                || lines[i].Contains("OperandPic is", StringComparison.Ordinal)) operandReads++;
        }
        // Make the scan able to FAIL: the usage readers must still be here, spelled over the operand picture.
        Assert.True(operandReads >= 4, $"expected the storage usage readers over OperandPic, found {operandReads}");
        Assert.True(offenders.Count == 0,
            "A receiving-store usage decision reads the raw PICTURE, which is null for a national / bit group "
            + "(ISO §14.9.25.4 GR4; kb/Work PB3512):\n" + string.Join("\n", offenders));
    }

    /// <summary>kb/Work PB3666, PB3697, determination D-B5 — EVERY STORE OF CHARACTERS INTO A BOOLEAN ITEM CONVERTS THEM. ISO §14.6.8.6
    /// transfers "after any necessary conversion, into the corresponding boolean positions"; the implementation converts
    /// each character ('1' to boolean one, every other to zero) in <c>CobolBool.FromCharacters</c>, so DISPLAY of the item and
    /// the boolean operators over it cannot disagree. The fitting store <c>CobolBool.Store</c> is emitted ONLY by
    /// <c>RuntimeApi.StrStoreBoolean</c>; the sites that fit by another rule convert through the same two entry points: a
    /// reference-modified slice's splice, a STRING image, and every storage-area store (a GR4 group move into an elementary
    /// item, a READ or RETURN into an elementary record area) in <c>ReceivingStore.StorageCharacters</c>, which converts the
    /// DECODED characters of a display or national boolean item and leaves a USAGE BIT item alone. A new emitter that
    /// deposits characters into a boolean item and spells <c>CobolString.Store(..., pad: '0')</c> itself would leave
    /// <c>AB000</c> in the item again. Goldens: <c>pb3666_boolean_receiver_converts_characters</c>.</summary>
    [Fact]
    public void EveryStoreIntoABooleanItem_ConvertsItsCharacters()
    {
        var offenders = new List<string>();
        var privateBooleanStore = new Regex(@"Store[^\n]*pad:\s*'0'");
        foreach (var file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (privateBooleanStore.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//"))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0,
            "A CodeGen file builds the boolean-zero-padded character store itself instead of RuntimeApi.StrStoreBoolean, "
            + "so it deposits the characters unconverted (D-B5; kb/Work PB3666):\n" + string.Join("\n", offenders));

        // Make the scan able to FAIL: the converting entry points must still be spelled where each store reaches them.
        string runtimeApi = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs"));
        Assert.Contains("nameof(CobolBool.Store)", runtimeApi);
        Assert.Contains("nameof(CobolBool.FromCharacters)", runtimeApi);
        string verbs(string f) => File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", f));
        // A storage-area store (a GR4 group move into an elementary item, a READ or RETURN into an elementary record area)
        // converts the DECODED characters of a boolean item, display or national, in ONE place: StorageCharacters
        // (kb/Work PB3697). It converts the characters the area decodes to, never the area's bytes (review R1-1 of PB3666:
        // N"101" became three CJK characters); a USAGE BIT item is exempt, every bit pattern being valid.
        string home = verbs("ReceivingStore.cs");
        string storageCharacters = Between(home, "public static string StorageCharacters(", "private static string DecodedStorage(");
        Assert.Contains("RuntimeApi.BoolFromCharacters(", storageCharacters);
        Assert.Contains("Category: PicCategory.Boolean, Usage: not Usage.Bit", storageCharacters);
        Assert.Contains("DecodedStorage(item, area, positions)", storageCharacters);
        // The representation decode and the pass-through test are one pair: the decoding arms are the complement of
        // AreaHoldsCharacters, and the reference-modified arm asks the predicate rather than spelling it (kb/Work PB3698).
        string decoded = Between(home, "private static string DecodedStorage(", "public static string ContentCharacters(");
        Assert.Contains("{ Usage: Usage.National }", decoded);
        Assert.Contains("{ Usage: Usage.Bit }", decoded);
        Assert.Contains("is not { Usage: Usage.National or Usage.Bit }", Between(home, "private static bool AreaHoldsCharacters(", ";"));
        Assert.Single(Regex.Matches(home, Regex.Escape("is not { Usage: Usage.National or Usage.Bit }")));
        Assert.Contains("AreaHoldsCharacters(item)", Between(home, "public static string SliceStorageArea(", "public static string StorageBytes("));
        // ...and the fitting store itself has no boolean arm: it would convert a second time, and a caller that fits
        // characters into a boolean item asks StrStoreBoolean (below), not Characters.
        Assert.DoesNotContain("RuntimeApi.StrStoreBoolean(", Between(home, "public static string Characters(", "private static bool AreaHoldsCharacters("));
        // A record area is a STORE into the record's own item only when no other description shares it (review R1-1 and
        // R1-2 of PB3697): the verbatim store never converts, and the one image channel names which of the two it is at
        // every call, so a restore (ROLLBACK), a shared area's READ and an "also available" store cannot convert.
        string verbatim = Between(home, "public static string VerbatimStorageArea(", "private static string WriteDecoded(");
        Assert.Contains("DecodedStorage(target.Item, area, positions)", verbatim);
        Assert.DoesNotContain("StorageCharacters(", verbatim);
        string seqIo = verbs("SequentialIoEmitter.cs");
        Assert.Contains("ReceivingStore.VerbatimStorageArea(", Between(seqIo, "public void EmitImageInto(", "public void EmitStoreFileStatus("));
        var imageCalls = Regex.Matches(seqIo + verbs("TransactionEmitter.cs") + verbs("SortEmitter.cs") + verbs("KeyedIoEmitter.cs"),
            @"(?<!void )EmitImageInto\([^;]*;");
        Assert.NotEmpty(imageCalls);
        foreach (Match call in imageCalls)
            Assert.Contains("storesIntoOwnItem:", call.Value);
        Assert.Single(Regex.Matches(seqIo, Regex.Escape("storesIntoOwnItem: file.AreaHasOneDescription")));
        Assert.Contains("storesIntoOwnItem: false", verbs("TransactionEmitter.cs"));
        // ...and the one-description predicate counts every sharer: a second record, a SAME RECORD AREA peer and an EXTERNAL
        // file's run-unit area, which another runtime element describes too (§13.18.22.4 GR4 b; review R2-1 of PB3697).
        string fileModel = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Model", "FileModel.cs"));
        Assert.Contains("public bool AreaHasOneDescription => Records.Count == 1 && !IsExternal && !SameRecordAreaPeers.Any();", fileModel);
        Assert.Contains("RuntimeApi.StrStoreBoolean(", verbs("MoveEmitter.cs"));         // the elementary MOVE
        Assert.Contains("RuntimeApi.BoolFromCharacters(", verbs("StringEmitter.cs"));    // STRING into a USAGE DISPLAY boolean item
        Assert.Contains("RuntimeApi.BoolFromCharacters(",
            File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs")));   // a reference-modified boolean slice
    }

    /// <summary>The text of <paramref name="source"/> from the first <paramref name="from"/> to the next
    /// <paramref name="to"/> after it, failing loudly when either marker moved so a scan over it cannot pass vacuously.</summary>
    private static string Between(string source, string from, string to)
    {
        int start = source.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"ReceivingStore.cs no longer spells '{from}'");
        int end = source.IndexOf(to, start + from.Length, StringComparison.Ordinal);
        Assert.True(end >= 0, $"ReceivingStore.cs no longer spells '{to}' after '{from}'");
        return source.Substring(start, end - start);
    }

    [Fact]
    public void TheOneHome_StillCarriesBothArms()
    {
        // Make the scan above able to FAIL: if the home stops spelling the two stores, the allow-list entry is
        // stale and the drift test would pass vacuously over a rule that moved somewhere unscanned.
        string home = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "ReceivingStore.cs"));
        Assert.Contains("RuntimeApi.DynStore(", home);
        Assert.Contains("RuntimeApi.StrStoreAligned(", home);
    }

    /// <summary>kb/Work PB1013 — ACCEPT format 1 sizes its device transfer by the RECEIVER's size at execution
    /// (ISO §14.9.1.4 GR3/GR4 "the size of the receiving data item"; an ANY LENGTH receiver's is its argument's,
    /// §13.18.2.4 GR1 b)), which is <c>ReceivingStore.CharacterPositions</c>. Every elementary arm used to spell the
    /// DECLARED width itself (<c>DisplayTextWidth</c>, <c>pic.Length</c>, <c>DynamicReceivingSize</c>), so an ANY
    /// LENGTH receiver took one character. This fails if any device read in <c>EmitAcceptDevice</c> other than the
    /// reference-modified slice sizes itself — the group arm included, whose size is the group's character
    /// positions (a bit / national group's as-if positions; kb/Work PB1653).</summary>
    [Fact]
    public void AcceptDevice_SizesEveryElementaryTransferByTheOneReceiverSize()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "AcceptDisplayEmitter.cs"));
        int start = src.IndexOf("private void EmitAcceptDevice(", StringComparison.Ordinal);
        Assert.True(start >= 0, "EmitAcceptDevice not found — the drift test must follow the method");
        int end = src.IndexOf("/// <summary>", start, StringComparison.Ordinal);
        string body = end < 0 ? src[start..] : src[start..end];
        Assert.Contains("ReceivingStore.CharacterPositions(target)", body);
        var offenders = new List<string>();
        foreach (var line in body.Split('\n'))
        {
            if (line.TrimStart().StartsWith("//")) continue;
            foreach (Match m in Regex.Matches(line, @"AcceptSource\.Device(?:Boolean)?\(([^)]*)\)"))
                if (m.Groups[1].Value is not ("{size}" or "{len}"))
                    offenders.Add(line.Trim());
        }
        Assert.True(offenders.Count == 0,
            "An ACCEPT device arm sizes its transfer from something other than the receiver's run-time size "
            + "(ReceivingStore.CharacterPositions — ISO §14.9.1.4 GR3; kb/Work PB1013):\n" + string.Join("\n", offenders));
    }
}
