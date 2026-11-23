// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE ONE RENDERER OF THE §2023:14.9.49.4 GR3 PER-TIER MATCH TEST over the raised exception-name <c>__ec</c> and its file
/// key <c>__f</c> (null for a condition with no file). Two selectors use it: the USE Format-3 selector <c>__EcDispatch</c>
/// (<see cref="EcEmitter.EmitDispatchSelector"/>) and the Format-3 PERFORM's WHEN matcher (<c>ControlFlowEmitter</c>), because
/// §2023:14.9.28.4 GR17 makes the WHEN match the USE rule: "The rules for determining a match are specified in General rules 3a
/// to 3g of the USE statement". Each used to write the tier tests itself, the second "mirroring" the first by a comment, and
/// the two already disagreed on a name the catalog does not resolve (kb/Work PB2723). The TIER is the bound operand's
/// (<see cref="BoundExceptionSelector.Tier"/>, decided at bind from the catalog level); this type renders it. The Format-1
/// file-name and open-mode tiers of a USE declarative are the I-O hook's switch (<see cref="UseTierEmitter"/>), which runs
/// only for an I-O condition and so needs no exception-name test.
/// </summary>
internal static class ExceptionSelectionEmitter
{
    /// <summary>The C# test that selects <paramref name="s"/>: GR3 a) a bare file-name selects any EC-I-O condition of that
    /// file; c) and e) a level-3 name selects that name; d) and f) a level-2 name selects every level-3 name under it (the
    /// catalog's longest-family-prefix predicate, so the open EC-USER-* names select correctly); g) EC-ALL selects every
    /// condition; c) and d) also require the paired file.</summary>
    public static string Test(BoundExceptionSelector s) => s.Tier switch
    {
        ExceptionSelectionTier.FileName => $"ExceptionCatalog.IsIoName(__ec) && __f == {FileKeyExpr(s.File!)}",
        ExceptionSelectionTier.FileLevel3 => $"__f == {FileKeyExpr(s.File!)} && __ec == {CsLiteral(s.Ec!)}",
        ExceptionSelectionTier.FileLevel2 => $"__f == {FileKeyExpr(s.File!)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(s.Ec!)})",
        ExceptionSelectionTier.Level3 => $"__ec == {CsLiteral(s.Ec!)}",
        ExceptionSelectionTier.Level2 => $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(s.Ec!)})",
        ExceptionSelectionTier.EcAll => "true",
        _ => throw new InvalidOperationException($"a {s.Tier} selector has no exception-name test (§2023:14.9.49.4 GR3)"),
    };

    /// <summary>The C# test of the GR3 b) open-mode tier for the <c>FileOpenMode</c> ordinal <paramref name="modeIndex"/>: an
    /// EC-I-O condition of a file currently open in that mode or being opened in it by the executing OPEN (the being-opened
    /// mode, <c>FileConnector.OpenModeView</c>, or the mode it is open in, <c>FileConnector.OpenedModeView</c>; kb/Work PB5200,
    /// PB5825). <c>__f</c> is null for a non-I-O condition or a file-less EC-I-O RAISE.</summary>
    public static string OpenModeTest(int modeIndex) =>
        $"ExceptionCatalog.IsIoName(__ec) && __f is not null && ({RuntimeApi.FileOpenModeOf("__f")} == {modeIndex} "
        + $"|| {RuntimeApi.FileOpenedModeOf("__f")} == {modeIndex})";

    /// <summary>The generated-source banner of a tier in <c>__EcDispatch</c>.</summary>
    public static string Banner(ExceptionSelectionTier tier) => tier switch
    {
        ExceptionSelectionTier.FileLevel3 => "// GR3c — file-scoped level-3 entries",
        ExceptionSelectionTier.FileLevel2 => "// GR3d — file-scoped level-2 entries",
        ExceptionSelectionTier.Level3 => "// GR3e — level-3 entries",
        ExceptionSelectionTier.Level2 => "// GR3f — level-2 entries",
        ExceptionSelectionTier.EcAll => "// GR3g — the level-1 EC-ALL entry",
        _ => throw new InvalidOperationException($"a USE Format-3 entry has no {tier} tier (§2023:14.9.49.2)"),
    };
}
