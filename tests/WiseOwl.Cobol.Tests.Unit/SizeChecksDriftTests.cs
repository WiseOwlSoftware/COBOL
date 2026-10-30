// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB4092, PB4093 — size error checking is PER EC-SIZE NAME (§14.6.13.1.1 NOTE 3: "An exception condition is set
/// to exist only if checking for that condition is enabled"), and "per name" has ONE table: <see cref="EcState.SizeCheckRows"/>
/// pairs each Table 13 name with its <see cref="SizeChecks"/> bit and the run-time flag a presenting GENERATE or TERMINATE
/// sets for the report compose. A condition that has a checked kernel with a continuing unchecked twin and no row here
/// would be taken by no statement; a row with no bit, or no flag, would be taken by every statement at compile time and by
/// none at run time. These tests keep the three in step, so a fourth name joins by construction.
/// </summary>
public sealed class SizeChecksDriftTests
{
    private static IEnumerable<SizeChecks> SingleBits() =>
        Enum.GetValues<SizeChecks>().Where(b => b != SizeChecks.None && (((int)b) & ((int)b - 1)) == 0);

    [Fact]   // Every single-bit SizeChecks member has exactly one row, and every row is one catalogued level-3 EC-SIZE name.
    public void EveryBit_HasExactlyOneRow_NamingACataloguedEcSizeCondition()
    {
        var rows = EcState.SizeCheckRows;
        foreach (var bit in SingleBits())
            Assert.True(rows.Count(r => r.Bit == bit) == 1, $"SizeChecks.{bit} must have exactly one EcState.SizeCheckRows row");
        foreach (var row in rows)
        {
            Assert.Contains(row.Bit, SingleBits());
            Assert.True(ExceptionCatalog.TryGet(row.Ec, out var info) && info.Level == 3 && info.IsFatal,
                $"{row.Ec} is not a catalogued fatal level-3 exception-name (Table 13)");
            Assert.StartsWith("EC-SIZE-", row.Ec, StringComparison.Ordinal);
        }
    }

    [Fact]   // Each row's flag exists on the runtime facade, and setting it alone makes SizeChecksEnabled answer exactly its bit.
    public void EveryRow_FlagIsTheRuntimeSwitchOfItsBit()
    {
        var engine = new ExceptionEngine();
        foreach (var row in EcState.SizeCheckRows)
        {
            var prop = typeof(ExceptionEngine).GetProperty(row.Flag);
            Assert.True(prop is { PropertyType: var t, CanWrite: true } && t == typeof(bool),
                $"ExceptionEngine.{row.Flag} (row {row.Ec}) must be a writable bool");
            engine.PushAllCheckingOff();
            Assert.Equal(SizeChecks.None, engine.SizeChecksEnabled);
            prop!.SetValue(engine, true);
            Assert.Equal(row.Bit, engine.SizeChecksEnabled);
            prop.SetValue(engine, false);
            Assert.NotNull(typeof(ExceptionState).GetProperty(row.Flag));   // the facade the emitted OpenGateFlags scope sets
        }
    }

    [Fact]   // The checked-kernel selection reads the names an exception-checking statement enables - and only those.
    public void ChecksOf_MapsEnabledNamesToTheirBits()
    {
        Assert.Equal(SizeChecks.None, EcState.ChecksOf(["EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION", "EC-SIZE-ADDRESS"]));
        Assert.Equal(SizeChecks.Overflow, EcState.ChecksOf(["EC-SIZE-OVERFLOW"]));
        Assert.Equal(SizeChecks.Truncation | SizeChecks.Underflow, EcState.ChecksOf(["EC-SIZE-TRUNCATION", "EC-SIZE-UNDERFLOW", "EC-SIZE-ZERO-DIVIDE"]));
        Assert.Equal(SizeChecks.All, EcState.ChecksOf(["EC-SIZE-OVERFLOW", "EC-SIZE-UNDERFLOW", "EC-SIZE-TRUNCATION"]));
    }
}
