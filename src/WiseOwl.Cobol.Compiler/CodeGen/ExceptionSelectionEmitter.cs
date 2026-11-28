// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.CodeGen.Emit;
using WiseOwl.Cobol.Compiler.CodeGen.Roslyn;
using WiseOwl.Cobol.Compiler.CodeGen.Verbs;

namespace WiseOwl.Cobol.Compiler.CodeGen;

using static WiseOwl.Cobol.Compiler.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE ONE RENDERER OF THE §2023:14.9.49.4 GR3 PER-TIER MATCH TEST over the raised exception-name <c>__ec</c> and its file
/// key <c>__f</c> (null for a condition with no file). Two selectors use it: the USE Format-3 selector <c>__EcDispatch</c>
/// (<see cref="EcEmitter.EmitDispatchSelector"/>) and the Format-3 PERFORM's WHEN matcher (<c>ControlFlowEmitter</c>), because
/// §2023:14.9.28.4 GR17 makes the WHEN match the USE rule: "The rules for determining a match are specified in General rules 3a
/// to 3g of the USE statement". Each used to write the tier tests itself, the second "mirroring" the first by a comment, and
/// the two already disagreed on a name the catalog does not resolve (kb/Work PB2723). The TIER is the bound operand's
/// (<see cref="BoundExceptionSelector.Tier"/>, decided at bind from the catalog level); this type renders it. The Format-1
/// file-name and open-mode tiers of a USE declarative are the I-O hook's switch (<see cref="UseTierEmitter"/>), which runs
/// only for an I-O condition and so needs no exception-name test; its open-mode switch selects on
/// <see cref="OpenModeSelected"/>, the same expression the WHEN open-mode arms test.
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

    /// <summary>The C# expression of the ONE open mode the GR3 b) tier selects on for the file key <c>__f</c>, given the
    /// <c>FileOpenMode</c> ordinals <paramref name="declaredModes"/> (a bit mask) the selector has a procedure or WHEN phrase
    /// for: the mode the file IS open in when one is declared for it, else the mode the executing OPEN or CLOSE is scoped to
    /// (<c>FileConnector.UseModeView</c>). A file is in a mode's scope when it is "open in the input mode or in the process of
    /// being opened in the input mode" (§2023:14.9.49.4 GR6, b) to e) alike), so an OPEN refused '41' on a file open in
    /// another mode meets two modes; where both are declared the text names no order and the open mode's runs, as GnuCOBOL's
    /// does (kb/Work PB5878, docs/conformance/3/D-USE-OPEN-MODE-BOTH.md). Both selectors of the tier use this one expression,
    /// the USE Format-1 switch (<see cref="UseTierEmitter"/>) and a Format-3 PERFORM's WHEN EXCEPTION INPUT | OUTPUT | I-O |
    /// EXTEND arms (<see cref="OpenModeTest"/>), because §2023:14.9.28.4 GR17 makes the WHEN match the USE rule (kb/Work
    /// PB2723 review R1-1: the WHEN arms once tested the attempted OR the opened mode and so ran the first-written phrase).</summary>
    public static string OpenModeSelected(int declaredModes) => RuntimeApi.FileUseModeOf("__f", declaredModes);

    /// <summary>The bit mask of <c>FileOpenMode</c> ordinals <see cref="OpenModeSelected"/> takes.</summary>
    public static int DeclaredModes(IEnumerable<int> modeIndexes) => modeIndexes.Aggregate(0, (mask, mi) => mask | 1 << mi);

    /// <summary>The C# test of a WHEN phrase's GR3 b) open-mode tier for the <c>FileOpenMode</c> ordinal
    /// <paramref name="modeIndex"/>, among the PERFORM's open-mode phrases <paramref name="declaredModes"/>: an EC-I-O
    /// condition of a file whose selected mode (<see cref="OpenModeSelected"/>) is that mode. <c>__f</c> is null for a
    /// non-I-O condition or a file-less EC-I-O RAISE.</summary>
    public static string OpenModeTest(int modeIndex, int declaredModes) =>
        $"ExceptionCatalog.IsIoName(__ec) && __f is not null && {OpenModeSelected(declaredModes)} == {modeIndex}";

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
