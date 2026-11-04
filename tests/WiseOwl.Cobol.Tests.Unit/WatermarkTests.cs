// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// P6 Step 6 — the completion-phase WATERMARK gate: the construction-time DAG assert guards the declared pass
/// LIST; the watermark guards what actually RAN on a binder — reading a fact before its producing pass is a loud,
/// located error rather than a silent miscompile. ALWAYS-ON (Debug AND Release): the gate was
/// <c>[Conditional("DEBUG")]</c> at first landing and CI's RELEASE test leg stripped the call sites, failing the
/// throw-expecting tests below — the fix keeps the guard in every configuration (DEVLOG 774).
/// </summary>
public sealed class WatermarkTests
{
    [Fact]
    public void Require_BeforeProducingPassRan_Throws()
    {
        var db = new DataBinder();
        var ex = Assert.Throws<InvalidOperationException>(() => db.State.Require(PassPhase.OccursResolved, "test-fact"));
        Assert.Contains("test-fact", ex.Message);
        Assert.Contains("OccursResolved", ex.Message);
    }

    [Fact]
    public void Require_AfterProduced_Passes()
    {
        var db = new DataBinder();
        db.State.MarkProduced(PassPhase.OccursResolved);
        db.State.Require(PassPhase.OccursResolved, "test-fact");   // must not throw (exact)
        db.State.Require(PassPhase.TypesExpanded, "test-fact");    // must not throw (earlier phase)
    }

    /// <summary>The flagged late-fact read point: a <c>CapacityRegisters</c> read before <c>DynamicResolve</c>
    /// produced it trips the gate (the "read a null CapacityRegister" silent-miscompile class, now structural).</summary>
    [Fact]
    public void CapacityRegisters_ReadBeforeOccursResolved_Trips()
    {
        var db = new DataBinder();
        Assert.Throws<InvalidOperationException>(() => _ = db.State.Forest.CapacityRegisters.Count);
    }

    /// <summary>⛔ No <see cref="DataBindResult"/> is made over a binder the group tail has not finished (kb/Work PB2361): the
    /// freeze asks the watermark for the tail's TERMINAL phase, so a unit whose passes have not all run is refused, never
    /// handed to a backend.</summary>
    [Fact]
    public void Freeze_BeforeTheGroupTailFinished_Throws()
    {
        var unit = new BoundUnit { Name = "WMFREEZE", ExternalizedName = "WMFREEZE", ClassName = "WMFREEZE", SourcePosition = 0 };
        var binders = new ElementBinders();
        var data = new DataBinder();
        binders.Add(unit, data);
        data.State.MarkProduced(PassPhase.OccursResolved);
        var ex = Assert.Throws<InvalidOperationException>(() => binders.Freeze([unit], []));
        Assert.Contains(nameof(DataBindResult), ex.Message);
        Assert.Contains(nameof(PassPhase.EditionConformanceChecked), ex.Message);
        Assert.Null(unit.Data);
    }

    /// <summary>End-to-end: a real group bind advances every unit binder's watermark to the manifest's TERMINAL
    /// phase — the gate can never fire on a well-formed compile (the pipeline itself is the proof): every unit carries the
    /// result the freeze makes only at that phase (<see cref="ElementBinders.Freeze"/>).</summary>
    [Fact]
    public void FullBind_AdvancesWatermarkToTerminalPhase()
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_wm_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. WMPROG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                MOVE 7 TO WS-N.
                DISPLAY WS-N.
                STOP RUN.
            """);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var comp = new BinderDriver().Bind(tree!, new EditionContext(2023));
            Assert.All(comp.Units, u => Assert.NotNull(u.Data));
        }
        finally { FileHarness.TryDelete(path); }
    }
}
