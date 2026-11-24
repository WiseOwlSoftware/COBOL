// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The DIRECT form of a source element's return to its activator (ISO §2023:14.9.18.4 GR2/GR4, §2023:14.9.14.4 GR3; kb/Work
/// PB6140): the value a dispatch frame returns, instead of throwing <see cref="ProgramReturn"/> or <see cref="MethodReturn"/>,
/// when every frame between it and the activation entry is a dispatch frame that tests for it.
/// <para>A dispatch frame knows that from its <c>__direct</c> argument: the activation entry passes <c>true</c> to the
/// element's top frame, an out-of-line PERFORM or a SORT/MERGE procedure passes its own frame's value on and returns this
/// value when the frame it called did, and every other caller (a USE procedure's <c>__RunUse</c>, a debugging section's
/// <c>__RunDebug</c>) passes <c>false</c>, so a GOBACK below it still throws the signal, whose exception filters and catch
/// clauses on the way are part of the rules those callers implement. Both forms leave the same frames, run the same
/// <c>finally</c> blocks and land at the same activation entry; the direct one costs a compare per frame instead of an
/// exception allocation and a two-pass unwind.</para>
/// A dispatch frame otherwise returns a program counter, which is never negative, so the value cannot be mistaken for one.
/// </summary>
public static class ElementReturn
{
    /// <summary>The element executed its return to the activator: the frame that receives it returns it in turn.</summary>
    public const int Returned = int.MinValue;
}
