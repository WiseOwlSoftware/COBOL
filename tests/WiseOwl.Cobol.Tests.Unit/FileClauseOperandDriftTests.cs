// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY DATA ITEM A FILE'S OWN CLAUSES NAME IS ON ONE LIST, <see cref="FileModel.ClauseOperandItems"/>, SO A PROGRAM
/// CONTAINED IN THE ONE THAT DESCRIBES A GLOBAL FILE CAN REACH IT (kb/Work PB4454). The contained program's I-O statements
/// read the file's RELATIVE KEY, ASSIGN USING, RECORD VARYING DEPENDING ON, FILE STATUS and LINAGE items, which are the
/// container's local items that ISO §13.18.27.4 GR2 does not let it name and §8.4.5 resolves in the container. The binder
/// bridges and places the roots on that list (<c>DataBinder.GlobalFileOperandRoots</c>); a data-item operand added to
/// <see cref="FileModel"/> and left off the list would stop the compilation of a contained program, with the
/// <see cref="InvalidOperationException"/> of an item the place table never saw.
/// <para>So every <see cref="DataItem"/> property of <see cref="FileModel"/> is either read by the list or named here as
/// resident in the file's record area (the records are the file's GLOBAL names, §13.18.27.4 GR1). A property in neither
/// is a red test, not a silently unreachable operand.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FileClauseOperandDriftTests
{
    /// <summary>The operands the list must read, by property name.</summary>
    private static readonly string[] Operands =
        [nameof(FileModel.AssignUsingItem), nameof(FileModel.FileStatusItem), nameof(FileModel.RelativeKeyItem),
         nameof(FileModel.VaryingDependingItem)];

    /// <summary>The properties that name the file's own record descriptions: global names with the file.</summary>
    private static readonly string[] RecordResident =
        [nameof(FileModel.ImpliedRecord), nameof(FileModel.AreaRecord), nameof(FileModel.CharacterAnchor),
         nameof(FileModel.Records), nameof(FileModel.WrittenRecords), nameof(FileModel.OutOfLineRecords)];

    private static bool HoldsDataItems(Type t) =>
        t == typeof(DataItem) || (t.IsGenericType && t.GetGenericArguments().Contains(typeof(DataItem)));

    [Fact]
    public void EveryDataItemPropertyOfAFile_IsAnOperandTheListReadsOrARecordTheFileOwns()
    {
        var unclassified = typeof(FileModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => HoldsDataItems(p.PropertyType) && p.Name != nameof(FileModel.ClauseOperandItems))
            .Select(p => p.Name)
            .Except(Operands).Except(RecordResident)
            .ToList();
        Assert.True(unclassified.Count == 0,
            "FileModel gained a data-item property that neither FileModel.ClauseOperandItems reads nor FileClauseOperandDriftTests "
            + "names as record-resident. If the file's own clause names it, add it to ClauseOperandItems (the contained "
            + "program of a GLOBAL file reaches its storage through that list, kb/Work PB4454); if it is a record, name it "
            + "in RecordResident. Found: " + string.Join(", ", unclassified));
    }

    [Fact]
    public void TheList_ReadsEveryOperandAndTheLinageOperands()
    {
        string source = File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "Binding", "Model", "FileModel.cs"));
        var body = Regex.Match(source, @"IEnumerable<DataItem> ClauseOperandItems\s*\{(?<b>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(body.Success, "FileModel.ClauseOperandItems was not found");
        var missing = Operands.Where(o => !Regex.IsMatch(body.Groups["b"].Value, $@"\b{o}\b")).ToList();
        Assert.True(missing.Count == 0,
            "FileModel.ClauseOperandItems does not read " + string.Join(", ", missing));
        Assert.Matches(@"Linage\?\.Operands", body.Groups["b"].Value);
        foreach (var op in new LinageInfo(new LinageOperand(1, null), null, null, null).Operands)
            Assert.Null(op.Item);   // the list reads LinageOperand.Item, the one data-item member of a LINAGE operand
        Assert.Single(typeof(LinageOperand).GetProperties(), p => p.PropertyType == typeof(DataItem));
    }
}
