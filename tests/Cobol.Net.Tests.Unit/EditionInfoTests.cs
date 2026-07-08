// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The rearch PHASE 02 edition leaf value types (<c>Cobol.Net.Editions</c>): <see cref="EditionInfo"/> is the
/// single immutable edition value (the one <c>DialectLevel</c> source), <see cref="EditionDiagnostic"/> +
/// <see cref="IDiagnosticSink"/> are the structured report channel. These assert the value semantics before the
/// registry moves onto them (P2.4).
/// </summary>
public sealed class EditionInfoTests
{
    [Theory]
    [InlineData(85, 18)]
    [InlineData(2002, 31)]
    [InlineData(2014, 31)]
    [InlineData(2023, 31)]
    public void MaxDigits_Is18AtLegacy_31At2002Plus(int year, int expected)
        => Assert.Equal(expected, EditionInfo.Of(year).MaxDigits);

    [Fact]
    public void Has_IsAtOrAfterIntroduction()
    {
        var ed = EditionInfo.Of(2014);
        Assert.True(ed.Has(85));
        Assert.True(ed.Has(2002));
        Assert.True(ed.Has(2014));
        Assert.False(ed.Has(2023));
    }

    [Fact]
    public void Latest_Is2023_Strict()
    {
        Assert.Equal(2023, EditionInfo.Latest.Year);
        Assert.False(EditionInfo.Latest.Permissive);
    }

    [Fact]
    public void Of_RejectsUnknownYear()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EditionInfo.Of(2000));
        Assert.Throws<ArgumentOutOfRangeException>(() => EditionInfo.Of(0));
    }

    [Fact]
    public void Of_CarriesThePermissiveAxis()
    {
        Assert.True(EditionInfo.Of(2023, permissive: true).Permissive);
        Assert.False(EditionInfo.Of(2023).Permissive);
    }

    [Fact]
    public void EditionDiagnostic_And_Sink_CarryTheStructuredValue()
    {
        var captured = new List<EditionDiagnostic>();
        IDiagnosticSink sink = new ListSink(captured);
        var d = new EditionDiagnostic("COBOLNET0900", EditionSeverity.Error, "invoke-2002",
            "INVOKE requires COBOL-2002", "statement in paragraph M", "ISO §14.9.23");
        sink.Report(d);

        var got = Assert.Single(captured);
        Assert.Equal("COBOLNET0900", got.Code);
        Assert.Equal(EditionSeverity.Error, got.Severity);
        Assert.Equal("invoke-2002", got.ConstructId);
    }

    private sealed class ListSink(List<EditionDiagnostic> sink) : IDiagnosticSink
    {
        public void Report(in EditionDiagnostic d) => sink.Add(d);
    }
}
