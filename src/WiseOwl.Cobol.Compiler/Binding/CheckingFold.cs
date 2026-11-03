// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE PRODUCER of the checking slices (§8.3 (1)): <see cref="UnitChecking"/> for every program unit and
/// every class half, <see cref="MethodChecking"/> for every method, each folded from the group's <see cref="TurnState"/>
/// and <see cref="PropagateState"/> at the element's own lines. <c>BinderDriver</c>'s unit loop runs it for EVERY unit (an
/// outermost program included) and its class loop for both halves and every method.</summary>
internal static class CheckingFold
{
    /// <summary>The checking slice of the program or function unit <paramref name="unit"/>.</summary>
    public static UnitChecking Unit(TurnState turn, PropagateState propagate, Core.ProgramUnitContext unit)
    {
        // §14.8.4.1: the ACTIVATED-element mask is the TurnState folded at the unit's first post-Identification division
        // header line ("which for activated runtime elements shall be before the Environment division").
        int divLine = unit.environmentDivision()?.Start.Line
            ?? unit.dataDivision()?.Start.Line
            ?? unit.procedureDivision()?.Start.Line
            ?? int.MaxValue;
        return new UnitChecking(
            ExternalCheckMask: ExternalMaskAt(turn, divLine),
            // §14.9.4.4 GR3 d) — folded at the unit's PROCEDURE DIVISION HEADER, never at divLine: the
            // before-Environment-division point is §14.8.4.1's rule for the EC-EXTERNAL conditions ALONE, and §7.3.25.4
            // GR6 / GR8 scope a TURN to the "procedure division headers that follow" (kb/Work PB1381).
            ArgMismatchChecking: turn.EnabledAtHeader("EC-PROGRAM-ARG-MISMATCH", unit.procedureDivision(), int.MaxValue),
            ExternalDescribe: ExternalDescribe(turn),
            OdoReferenceChecking: OdoReferenceChecking(turn),
            // §7.3.21.4 GR1/GR3, folded at the unit's first line (§7.3.21.3 SR1: never inside a compilation unit, so a
            // contained program folds to its container's state). Its IDENTIFICATION DIVISION is the first line: a
            // CONTAINED unit's context is the synthetic one BinderDriver.Reparent builds, whose own Start is null.
            AutomaticPropagation: propagate.IsOnAt(unit.identificationDivision().Start.Line));
    }

    /// <summary>The checking slice of a class definition's OBJECT or FACTORY half: the two group-level gates only. The
    /// activated-element mask, the argument-mismatch gate and automatic propagation are each a METHOD's
    /// (<see cref="Method"/>), since the half activates nothing of its own.</summary>
    public static UnitChecking ClassHalf(TurnState turn) => UnitChecking.None with
    {
        ExternalDescribe = ExternalDescribe(turn),
        OdoReferenceChecking = OdoReferenceChecking(turn),
    };

    /// <summary>The checking slice of the method <paramref name="method"/> of the class definition <paramref name="cls"/>.
    /// A PROPERTY accessor SYNTHESIZED from a PROPERTY clause (§13.18.42) has no METHOD-ID in source and so no method
    /// context: its enclosing CLASS-ID line is the query point, since the accessor is part of that definition and has no
    /// source position of its own to disagree with. A written method with no procedure division answers at its METHOD-ID
    /// line (it has no header for a TURN to precede).</summary>
    public static MethodChecking Method(TurnState turn, PropagateState propagate, OoMethodSymbol method, OoClassSymbol cls)
    {
        int idLine = (method.Ctx?.Start ?? cls.Ctx.Start).Line;
        int divLine = method.Ctx is { } mc
            ? (mc.environmentDivision()?.Start ?? mc.dataDivision()?.Start ?? mc.procedureDivision()?.Start ?? mc.Stop).Line
            : cls.Ctx.Start.Line;
        return new MethodChecking(
            OoUniversalChecking: turn.EnabledAtHeader("EC-OO-UNIVERSAL", method.Ctx?.procedureDivision(), idLine),
            ExternalCheckMask: ExternalMaskAt(turn, divLine),
            AutomaticPropagation: propagate.IsOnAt(idLine));
    }

    /// <summary>§14.8.4.1's "enabled in both the activating and activated runtime elements", whose activated half is folded
    /// "before the Environment division": the <see cref="Runtime.ExternalChecks"/> bits of the EC-EXTERNAL conditions the
    /// group's TurnState has enabled at <paramref name="divLine"/>, the element's first division header after its
    /// identification. A program unit and a method both take it, so the two cannot answer the rule differently.</summary>
    private static int ExternalMaskAt(TurnState turn, int divLine) =>
        (turn.Enabled("EC-EXTERNAL-FORMAT-CONFLICT", null, divLine) ? (int)Runtime.ExternalChecks.FormatConflict : 0)
        | (turn.Enabled("EC-EXTERNAL-DATA-MISMATCH", null, divLine) ? (int)Runtime.ExternalChecks.DataMismatch : 0)
        | (turn.Enabled("EC-EXTERNAL-FILE-MISMATCH", null, divLine) ? (int)Runtime.ExternalChecks.FileMismatch : 0);

    /// <summary>Any EC-EXTERNAL enabling event anywhere in the group (the zero-scaffolding gate of <see
    /// cref="UnitChecking.ExternalDescribe"/>).</summary>
    private static bool ExternalDescribe(TurnState turn) => Procedure.EcBinder.ExternalNames.Any(turn.AnyEnabledFor);

    /// <summary>Any EC-BOUND-ODO enabling event anywhere in the group (§13.18.38.4 GR7; kb/Work PB1268).</summary>
    private static bool OdoReferenceChecking(TurnState turn) => turn.AnyEnabledFor("EC-BOUND-ODO");
}
