// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The run-unit EXTERNAL file-connector conformance check (ISO §14.8.4.2, <see cref="ExternalTable"/> DataMismatch,
/// §24 review V45-sentinel). Conjunct 1 — the FILE STATUS / RELATIVE KEY / LINAGE data items of an external file
/// connector SHALL BE external data items — is encoded at codegen as the "!" sentinel on a describer ref when a
/// present item is not external. Two non-external describers used to compare EQUAL (<c>"!" == "!"</c>) and silently
/// pass; the fix treats any "!" (a whole ";"-token, so an embedded LINAGE token counts) as a violation regardless of
/// equality. Conjunct 2 (same corresponding item) is the existing inequality comparison. A null ref (clause
/// unspecified) is never a violation. The single-program face of conjunct 1 is enforced at COMPILE time (COBOLNET1624);
/// this covers the cross-compilation / permissive-carried-to-runtime face.
/// </summary>
public class ExternalTableTests
{
    private static ExternalDescriptor File(string? fs = null, string? rk = null, string? ln = null) =>
        new("file", FileStatusRef: fs, RelativeKeyRef: rk, LinageRef: ln);

    [Fact]   // V45-sentinel: BOTH describers carry "!" for FILE STATUS — the both-non-external false negative.
    public void BothNonExternalFileStatus_RaisesDataMismatch()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(fs: "!"), ExternalChecks.DataMismatch);   // first describer: nothing to compare
        var ex = Assert.Throws<CobolCallException>(() =>
            t.Describe("B", "F", File(fs: "!"), ExternalChecks.DataMismatch));
        Assert.Equal("EC-EXTERNAL-DATA-MISMATCH", ex.EcName);
    }

    [Fact]   // The "!" is a whole ";"-token inside a LINAGE ref; both describers carry the IDENTICAL non-external ref,
             // so the old `!=` compared them EQUAL and missed it — the embedded-token detection must still raise.
    public void EmbeddedNonExternalLinageToken_RaisesDataMismatch()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(ln: "EXT-LN;!;=2"), ExternalChecks.DataMismatch);
        var ex = Assert.Throws<CobolCallException>(() =>
            t.Describe("B", "F", File(ln: "EXT-LN;!;=2"), ExternalChecks.DataMismatch));
        Assert.Equal("EC-EXTERNAL-DATA-MISMATCH", ex.EcName);
    }

    [Fact]   // Positive control: two all-external, same-identity describers do NOT raise (a "!"-free identity/literal).
    public void MatchingExternalRefs_NoRaise()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(fs: "EXT-ST", ln: "EXT-LN;=2"), ExternalChecks.DataMismatch);
        t.Describe("B", "F", File(fs: "EXT-ST", ln: "EXT-LN;=2"), ExternalChecks.DataMismatch);   // no throw
    }

    [Fact]   // A null ref (clause unspecified in both) is not a violation — must not spuriously raise.
    public void UnspecifiedRefs_NoRaise()
    {
        var t = new ExternalTable();
        t.Describe("A", "F", File(), ExternalChecks.DataMismatch);
        t.Describe("B", "F", File(), ExternalChecks.DataMismatch);   // no throw
    }
}
