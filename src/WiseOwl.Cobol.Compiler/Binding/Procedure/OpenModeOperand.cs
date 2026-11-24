// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>The open mode INPUT | OUTPUT | I-O | EXTEND, the one grammar rule <c>openMode</c> that three operands reference
/// (kb/Work PB6139): the OPEN statement's mode (§2023:14.9.27.2), USE AFTER EXCEPTION ON INPUT|OUTPUT|I-O|EXTEND (Format 1,
/// <c>useOnTarget</c>) and PERFORM ... WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND (Format 3, <c>performWhenModeList</c>). The
/// keywords are read here and nowhere else; the two exception operands, the §2023:14.9.49.4 GR3b tier, carry the mode as the
/// runtime <see cref="FileOpenMode"/> ordinal that <c>BoundDeclarative.ModeIndex</c> and <c>BoundExceptionMatch.ModeIndex</c>
/// hold (kb/Work PB2723: the keyword set used to map to a string at one site and to the ordinal at the other, with a second
/// mapping back in the emitter).</summary>
internal static class OpenModeOperand
{
    /// <summary>The mode <paramref name="mode"/> writes.</summary>
    public static BoundOpenMode Of(CobolParserCore.OpenModeContext mode) => mode switch
    {
        _ when mode.INPUT() is not null => BoundOpenMode.Input,
        _ when mode.OUTPUT() is not null => BoundOpenMode.Output,
        _ when mode.I_O() is not null => BoundOpenMode.IO,
        _ when mode.EXTEND() is not null => BoundOpenMode.Extend,
        _ => throw new InvalidOperationException($"openMode '{mode.GetText()}' is none of INPUT, OUTPUT, I-O, EXTEND"),
    };

    /// <summary>The <see cref="FileOpenMode"/> ordinal of an exception operand's open mode, or null when no
    /// <paramref name="mode"/> was written (the operand is a file-name list).</summary>
    public static int? ExceptionModeIndex(CobolParserCore.OpenModeContext? mode) => mode is null ? null : (int)(Of(mode) switch
    {
        BoundOpenMode.Input => FileOpenMode.Input,
        BoundOpenMode.Output => FileOpenMode.Output,
        BoundOpenMode.IO => FileOpenMode.IO,
        BoundOpenMode.Extend => FileOpenMode.Extend,
        var other => throw new InvalidOperationException($"open mode {other} has no FileOpenMode"),
    });
}
