// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5556 — <see cref="FileModel.ExtendRefusals"/> is the ONE statement of when a file cannot be OPENed EXTEND
/// (§1985:VII.4.3.3 SR2 and SR3, §1985:VIII.4.4.3 SR1, §1985:IX.4.4.3 SR1), read by <c>StatementValidation.CheckOpenModeForFile</c>
/// (one diagnostic per cause) and, as <see cref="FileModel.CanOpenExtend"/>, by the USE ... ON EXTEND applicability rule
/// (§1985:VII.4.6.4 GR5e). The expectations are written from the rules, not from the property: each row is the file's attributes
/// and the causes the rules name.
/// </summary>
public sealed class FileModelExtendTests
{
    private static FileModel File(string name, FileAccessMode access = FileAccessMode.Sequential, bool linage = false,
        FileModel? sharesReelWith = null, bool report = false)
    {
        var f = new FileModel { CobolName = name, AssignTarget = name + ".dat", AccessMode = access };
        if (linage) f.Linage = new LinageInfo(new LinageOperand(5, null), null, null, null);
        if (sharesReelWith is not null) f.MultipleFileReels.Add(new MultipleFileReel([f, sharesReelWith]));
        if (report) f.ReportNames.Add(new ReportClauseName("R", default));
        return f;
    }

    private static FileModel Mate() => File("MATE");

    public static TheoryData<FileAccessMode, bool, bool, bool, ExtendRefusal> Rows => new()
    {
        // access mode, LINAGE, on a multiple file reel, a report file, the causes the rules name
        { FileAccessMode.Sequential, false, false, false, ExtendRefusal.None },
        { FileAccessMode.Random, false, false, false, ExtendRefusal.AccessMode },
        { FileAccessMode.Dynamic, false, false, false, ExtendRefusal.AccessMode },
        { FileAccessMode.Sequential, true, false, false, ExtendRefusal.Linage },
        { FileAccessMode.Sequential, false, true, false, ExtendRefusal.MultipleFileReel },      // §1985:VII.4.3.3 SR2
        { FileAccessMode.Sequential, false, true, true, ExtendRefusal.None },                   // a report file's OPEN is the Report Writer's
        { FileAccessMode.Random, true, false, false, ExtendRefusal.AccessMode | ExtendRefusal.Linage },
        { FileAccessMode.Sequential, true, true, false, ExtendRefusal.Linage | ExtendRefusal.MultipleFileReel },
        { FileAccessMode.Dynamic, true, true, false, ExtendRefusal.AccessMode | ExtendRefusal.Linage | ExtendRefusal.MultipleFileReel },
        { FileAccessMode.Dynamic, true, true, true, ExtendRefusal.AccessMode | ExtendRefusal.Linage },
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void ExtendRefusals_NameEveryCauseTheRulesName_AndCanOpenExtendIsNoneOfThem(
        FileAccessMode access, bool linage, bool onReel, bool report, ExtendRefusal expected)
    {
        var file = File("F", access, linage, onReel ? Mate() : null, report);
        Assert.Equal(expected, file.ExtendRefusals);
        Assert.Equal(expected == ExtendRefusal.None, file.CanOpenExtend);
    }
}
