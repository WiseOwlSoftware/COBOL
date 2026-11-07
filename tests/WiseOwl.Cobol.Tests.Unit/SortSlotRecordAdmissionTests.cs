// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHICH SD RECORDS THE SORT STORE CARRIES WITH THEIR MANAGED SLOTS (kb/Work PB4772, review round 1). The slot-carrying
/// path (<c>SortBinder.SortRecordCarriesSlots</c>) admits a fixed-size, single-description SD record in the INPUT/OUTPUT
/// PROCEDURE form only. Everything else a pointer-leafed SD record can meet stays the COBOL1756 deferral, never a
/// silent compile: a variable-length file releases a size (ISO §2023:13.18.43.4 GR13 a) the leaf-vector path ignores, and
/// USING/GIVING/MERGE move a FILE record, which has no managed-slot half. Each case names its own reason text.
/// </summary>
public sealed class SortSlotRecordAdmissionTests
{
    private const string Files = """
       FD FI.
       01 FIREC PIC X(10).
       FD FO.
       01 FOREC PIC X(10).
""";

    private const string Phases = """
       INP SECTION.
           CONTINUE.
       OUTP SECTION.
           CONTINUE.
""";

    private static string Program(string id, string sdClause, string fileSection, string statement) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {id}.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb4772sf.wrk".
           SELECT FI ASSIGN TO "pb4772fi.dat".
           SELECT FO ASSIGN TO "pb4772fo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SF{sdClause}.
       01 SREC TYPE T-REC.
{fileSection}
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 SKEY PIC 9(2).
          05 SPTR USAGE POINTER.
          05 STXT PIC X(5).
       01 WS-N PIC 9(2) VALUE 2.
       PROCEDURE DIVISION.
       MAIN SECTION.
           {statement}
           STOP RUN.
{Phases}
""";

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) Compile(string source)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4772_");
        try
        {
            string src = Path.Combine(dir, "pb4772.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4772.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Errors, r.Warnings);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static void AssertDeferred(string source, string reason)
    {
        var (ok, errors, warnings) = Compile(source);
        Assert.True(ok, string.Join(Environment.NewLine, errors));
        Assert.Contains(warnings, w => w.Contains("COBOL1756", StringComparison.Ordinal)
                                       && w.Contains(reason, StringComparison.Ordinal));
    }

    private const string ProcForm = "SORT SF ON ASCENDING KEY SKEY OF SREC INPUT PROCEDURE IS INP OUTPUT PROCEDURE IS OUTP.";

    [Fact]
    public void FixedSd_InputOutputProcedureForm_IsAdmitted()
    {
        var (ok, errors, warnings) = Compile(Program("PB4772A", "", Files, ProcForm));
        Assert.True(ok, string.Join(Environment.NewLine, errors));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOL1756", StringComparison.Ordinal));
    }

    /// <summary>A RECORD VARYING SD: RELEASE sends only the released size (§2023:13.18.43.4 GR13 a), which the leaf-vector
    /// path would ignore, so the record stays the deferral.</summary>
    [Fact]
    public void VaryingSd_StaysTheDeferral() => AssertDeferred(
        Program("PB4772B", " RECORD IS VARYING IN SIZE FROM 2 TO 30 DEPENDING ON WS-N", Files, ProcForm),
        "pointer/object-class leaf");

    [Fact]
    public void SortUsing_OverASlotRecord_StaysTheDeferral() => AssertDeferred(Program("PB4772C", "", Files,
        "SORT SF ON ASCENDING KEY SKEY OF SREC USING FI OUTPUT PROCEDURE IS OUTP."), "managed-slot half");

    [Fact]
    public void SortGiving_OverASlotRecord_StaysTheDeferral() => AssertDeferred(Program("PB4772D", "", Files,
        "SORT SF ON ASCENDING KEY SKEY OF SREC INPUT PROCEDURE IS INP GIVING FO."), "managed-slot half");

    [Fact]
    public void Merge_OverASlotRecord_StaysTheDeferral() => AssertDeferred(Program("PB4772E", "", Files,
        "MERGE SF ON ASCENDING KEY SKEY OF SREC USING FI FO OUTPUT PROCEDURE IS OUTP."), "managed-slot half");
}
