// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// THE LIMIT ON THE SIZE OF ONE DATA ITEM (kb/Work PB5695; docs/CONFORMANCE.md §3 D-ITEM-SIZE): 134,217,728 (2^27) character
/// positions, elementary or group. No edition's text bounds the size of a data item, so the limit is the implementation's
/// (§2023:4.5, "the limits of an implementation"), and it is the one an elementary item's PICTURE already met (COBOL2148). Each
/// expected verdict is derived from that number and the §2023:13.18.38 / §1985 OCCURS arithmetic of the entry, never copied
/// from a run: a group is the sum of its subordinates, each times its OCCURS count. The compiles are CheckOnly, so an item AT
/// the limit is never allocated. Before the limit a group past it compiled clean at every edition, its size wrapped in the
/// compiler's 32-bit model, and the program allocated until the host ran out of memory.
/// </summary>
public sealed class DataItemSizeLimitTests
{
    private static CompilerDriver.Result Check(string name, string dataEntries, int edition)
    {
        string dir = ScratchDirectory.Create("itemsize");
        try
        {
            string path = Path.Combine(dir, name + ".cob");
            File.WriteAllText(path, $"""
                       IDENTIFICATION DIVISION.
                       PROGRAM-ID. {name}.
                       DATA DIVISION.
                       WORKING-STORAGE SECTION.
                {dataEntries}
                       PROCEDURE DIVISION.
                       MAIN-P.
                           STOP RUN.
                """);
            return CompilerDriver.Compile(new CompilerDriver.Options(path, DialectLevel: edition, CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // 1 x 134,217,728 = 134,217,728 positions: AT the limit, accepted.
    private const string AtLimit = """
               01 T.
                  05 E PIC X OCCURS 134217728.
        """;

    // 1,024 x 131,072 = 134,217,728 positions: AT the limit through a wider element, accepted.
    private const string AtLimitWideElements = """
               01 T.
                  05 E PIC X(1024) OCCURS 131072.
        """;

    // 134,217,728 + 1 = 134,217,729 positions: one past the limit, refused.
    private const string OnePast = """
               01 T.
                  05 E PIC X OCCURS 134217728.
                  05 F PIC X.
        """;

    // 1,000 x 2,000,000,000 = 2,000,000,000,000 positions (kb/Work PB5695's own entry), refused.
    private const string Incident = """
               01 T.
                  05 E PIC X(1000) OCCURS 2000000000.
        """;

    // (2,000,000,000)^3 x 9 positions: past the 64-bit range, the size saturates and is still refused, never wrapped.
    private const string PastLong = """
               01 T.
                  05 A OCCURS 2000000000.
                     10 B OCCURS 2000000000.
                        15 C OCCURS 2000000000.
                           20 D PIC X(9).
        """;

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnItemAtTheLimit_IsAccepted(int edition)
    {
        foreach (var (name, entries) in new[] { ("ATLIMIT", AtLimit), ("ATLIMIT2", AtLimitWideElements) })
        {
            var r = Check(name, entries, edition);
            Assert.True(r.Success, $"{name} at {edition}: " + string.Join("\n", r.Errors));
        }
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AGroupPastTheLimit_IsRefusedWithItsTrueSize(int edition)
    {
        foreach (var (name, entries, size) in new[]
        {
            ("ONEPAST", OnePast, "134,217,729"),
            ("INCIDENT", Incident, "2,000,000,000,000"),
            ("PASTLONG", PastLong, "more than 9,223,372,036,854,775,806"),
        })
        {
            var r = Check(name, entries, edition);
            Assert.False(r.Success, $"{name} at {edition} compiled");
            var report = Assert.Single(r.Errors, e => e.Contains("COBOL7641"));
            Assert.Contains($"'T': its size, {size} character positions", report);
            Assert.Contains("134,217,728", report);
        }
    }
}
