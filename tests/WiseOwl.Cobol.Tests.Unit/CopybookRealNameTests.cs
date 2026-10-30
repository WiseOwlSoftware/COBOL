// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB1680 — library text is named by the file's REAL directory-entry spelling, never by the spelling that was
/// probed. The DOC-A.1-40 determination probes <c>.CPY</c> before <c>.cpy</c> (GnuCOBOL's order); on a case-insensitive
/// file system (Windows, the default macOS volume) the probe <c>book.CPY</c> finds <c>book.cpy</c>, and the name that
/// reached the diagnostics, the line map and the dependency record was the probe's. Every assertion below holds on both
/// kinds of host: the expected name is the one the file was created with.
/// </summary>
public sealed class CopybookRealNameTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_RealName_");

    public void Dispose() { ScratchDirectory.Remove(_dir); }

    /// <summary>True when the file system the scratch directory is on matches names without regard to case.</summary>
    private bool CaseInsensitive()
    {
        string probe = Path.Combine(_dir, "CaseProbe.tmp");
        File.WriteAllText(probe, "");
        return File.Exists(Path.Combine(_dir, "caseprobe.TMP"));
    }

    [Fact]
    public void LocateFile_NamesTheEntryAsItsDirectorySpellsIt()
    {
        string real = Path.Combine(_dir, "Mixed.Cpy");
        File.WriteAllText(real, "01 A PIC X.\n");
        var inputs = new CompilationInputs();
        string probe = Path.Combine(_dir, "MIXED.CPY");
        if (CaseInsensitive())
            Assert.Equal(real, inputs.LocateFile(probe));   // the probe's spelling is not the answer
        else
            Assert.Null(inputs.LocateFile(probe));          // another name on this file system
        Assert.Equal(real, inputs.LocateFile(real));
        Assert.Null(inputs.LocateFile(Path.Combine(_dir, "Absent.cpy")));
        Assert.Contains(inputs.Probes, p => !p.Exists && p.Path.EndsWith("Absent.cpy", StringComparison.Ordinal));   // the miss is recorded
    }

    [Fact]
    public void LocateFile_OnACaseSensitiveFileSystem_PrefersTheExactSpelling()
    {
        File.WriteAllText(Path.Combine(_dir, "two.CPY"), "01 U PIC X.\n");
        File.WriteAllText(Path.Combine(_dir, "two.cpy"), "01 L PIC X.\n");
        if (Directory.GetFiles(_dir, "two.*").Length != 2) return;   // a case-insensitive file system holds one entry: nothing to prefer
        var inputs = new CompilationInputs();
        Assert.Equal(Path.Combine(_dir, "two.cpy"), inputs.LocateFile(Path.Combine(_dir, "two.cpy")));
        Assert.Equal(Path.Combine(_dir, "two.CPY"), inputs.LocateFile(Path.Combine(_dir, "two.CPY")));
    }

    [Fact]
    public void Copy_ByTextName_ReportsTheLibraryTextByTheFilesRealName()
    {
        // `.CPY` is probed before `.cpy`: where the file system folds case that probe hits gr9dbook.cpy.
        string real = Path.Combine(_dir, "gr9dbook.cpy");
        File.WriteAllText(real, "       01 QX PIC X(0).\n");
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: 2023, permissive: false);
        var m = ConditionalCompilationProcessor.ProcessWithCopyMapped(
            MappedText.Identity("COPY gr9dbook.\n", "t.cob"), copy,
            WiseOwl.Cobol.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
        var origin = m.Lines.Select(l => l.Origin.File).Single(f => f != "t.cob");
        Assert.Equal(real, origin);   // Ordinal: the spelling the file system holds
    }
}
