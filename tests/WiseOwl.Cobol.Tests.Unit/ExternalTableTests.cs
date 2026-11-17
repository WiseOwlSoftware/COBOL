// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The run-unit EXTERNAL file-connector conformance check (ISO §2023:14.8.4.2, <see cref="ExternalTable"/> DataMismatch,
/// §24 review V45-sentinel). Conjunct 1 — the FILE STATUS / RELATIVE KEY / LINAGE data items of an external file
/// connector SHALL BE external data items — is the descriptor's own <see cref="ExternalDescriptor.NonExternalItem"/> field,
/// set by the codegen when a present item is not external. Two non-external describers used to compare EQUAL
/// (<c>"!" == "!"</c>) and silently pass; either describer's mark is a violation regardless of equality. Conjunct 2 (same
/// corresponding item) is the equality of the reference texts, which no reader splits or searches (kb/Work PB5888 review R2-1:
/// a ';'-split found a "!" token inside a record name externalized AS "A;!;B" and raised a false mismatch). A null ref (clause
/// unspecified) is never a violation. The single-program face of conjunct 1 is enforced at COMPILE time (COBOL1624); this covers
/// the cross-compilation / permissive-carried-to-runtime face.
/// </summary>
public class ExternalTableTests
{
    private static ExternalDescriptor File(string? fs = null, string? rk = null, string? ln = null, bool nonExternal = false) =>
        new("file", FileStatusRef: fs, RelativeKeyRef: rk, LinageRef: ln, NonExternalItem: nonExternal);

    [Fact]   // V45-sentinel: BOTH describers name a non-external FILE STATUS item — the both-non-external false negative.
    public void BothNonExternalFileStatus_RaisesDataMismatch()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(fs: "!", nonExternal: true), ExternalChecks.DataMismatch);   // first describer: nothing to compare
        var ex = Assert.Throws<CobolCallException>(() =>
            t.Describe("B", "F", File(fs: "!", nonExternal: true), ExternalChecks.DataMismatch));
        Assert.Equal("EC-EXTERNAL-DATA-MISMATCH", ex.EcName);
    }

    [Fact]   // A non-external LINAGE operand among others; both describers carry the IDENTICAL reference, so equality alone
             // would miss it — the mark must still raise.
    public void NonExternalLinageOperand_RaisesDataMismatch()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(ln: "6:EXT-LN;1:!;2:=2;", nonExternal: true), ExternalChecks.DataMismatch);
        var ex = Assert.Throws<CobolCallException>(() =>
            t.Describe("B", "F", File(ln: "6:EXT-LN;1:!;2:=2;", nonExternal: true), ExternalChecks.DataMismatch));
        Assert.Equal("EC-EXTERNAL-DATA-MISMATCH", ex.EcName);
    }

    [Fact]   // kb/Work PB5888 review R2-1: an external record named AS "A;!;B" puts ';' and '!' inside the key; two describers of
             // that one external item agree and must NOT raise.
    public void KeyHoldingSemicolonAndBang_NoRaise()
    {
        const string key = "5:A;!;B|1:L|||0|4";
        var t = new ExternalTable();
        t.Describe("A", "F", File(ln: $"{key.Length}:{key};"), ExternalChecks.DataMismatch);
        t.Describe("B", "F", File(ln: $"{key.Length}:{key};"), ExternalChecks.DataMismatch);   // no throw
    }

    [Fact]   // Positive control: two all-external, same-identity describers do NOT raise.
    public void MatchingExternalRefs_NoRaise()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(fs: "EXT-ST", ln: "6:EXT-LN;2:=2;"), ExternalChecks.DataMismatch);
        t.Describe("B", "F", File(fs: "EXT-ST", ln: "6:EXT-LN;2:=2;"), ExternalChecks.DataMismatch);   // no throw
    }

    [Fact]   // A null ref (clause unspecified in both) is not a violation — must not spuriously raise.
    public void UnspecifiedRefs_NoRaise()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(), ExternalChecks.DataMismatch);
        t.Describe("B", "F", File(), ExternalChecks.DataMismatch);   // no throw
    }
}
