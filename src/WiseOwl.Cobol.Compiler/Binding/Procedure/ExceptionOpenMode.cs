// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>The open-mode operand of the §2023:14.9.49.4 GR3b tier, written as USE AFTER EXCEPTION ON INPUT|OUTPUT|I-O|EXTEND
/// (Format 1, <c>useOnTarget</c>) and as PERFORM ... WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND (Format 3, <c>performWhenModeList</c>),
/// bound to ONE representation, the runtime <see cref="FileOpenMode"/> ordinal that <c>BoundDeclarative.ModeIndex</c> and
/// <c>BoundExceptionMatch.ModeIndex</c> carry (kb/Work PB2723: the keyword set used to map to a string at one site and to the
/// ordinal at the other, with a second mapping back in the emitter).</summary>
internal static class ExceptionOpenMode
{
    /// <summary>The ordinal of the one mode keyword present, or null when the operand is a file-name list.</summary>
    public static int? IndexOf(ITerminalNode? input, ITerminalNode? output, ITerminalNode? io, ITerminalNode? extend) =>
        input is not null ? (int)FileOpenMode.Input
        : output is not null ? (int)FileOpenMode.Output
        : io is not null ? (int)FileOpenMode.IO
        : extend is not null ? (int)FileOpenMode.Extend
        : null;
}
