// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ WHICH SIZE ERROR CONDITIONS A STATEMENT TAKES (kb/Work PB4092, PB4093) — the EC-SIZE names whose kernels have an
/// unchecked twin that CONTINUES, so the choice between the two is per name and never per family.
/// §14.6.13.1.1: "If checking for an exception condition is enabled and an exception status indicator is set as a
/// result of an exception detected during the execution of a statement, the associated exception condition is
/// raised", and its NOTE 3: "An exception condition is set to exist only if checking for that condition is enabled".
/// A condition whose checking is not enabled takes §14.6.13.1.3 item 8's not-enabled disposition, which
/// docs/CONFORMANCE.md DOC-A.1-70 documents: the unchecked kernel's value, execution continuing.
/// <para>A statement WITH a SIZE ERROR phrase takes <see cref="All"/>: §14.7.5 enables checking of "the arithmetic
/// operations that take place in developing and storing the result", whichever condition they meet. A statement
/// without one takes the names its <c>&gt;&gt;TURN</c> (or PERFORM WHEN) enables. The other EC-SIZE names have no
/// bit because their kernels have no unchecked twin that continues: EC-SIZE-ZERO-DIVIDE and EC-SIZE-EXPONENTIATION
/// are raised by every kernel, checked or not (a quotient or a power that cannot be formed has no value to store), so
/// their not-enabled disposition is the run unit's termination and the statement simply does not catch them.</para>
/// </summary>
[Flags]
public enum SizeChecks
{
    /// <summary>No size error condition is taken: every kernel is the unchecked one.</summary>
    None = 0,

    /// <summary>EC-SIZE-OVERFLOW (§14.7.5 no-phrase rule 3): an operation on an intermediate data item whose nonzero
    /// result is farther from zero than the intermediate allows — the binary64 operators' infinity, the scaled
    /// <see cref="System.Int128"/> carrier's sum past its engine.</summary>
    Overflow = 1,

    /// <summary>EC-SIZE-UNDERFLOW (§14.7.5 no-phrase rule 3): an operation on an intermediate data item whose nonzero
    /// result is nearer to zero than the intermediate allows — the binary64 operators' rounding of a nonzero product
    /// or quotient to zero.</summary>
    Underflow = 2,

    /// <summary>EC-SIZE-TRUNCATION (§14.7.5 no-phrase rule 4): a result further from zero than its resultant
    /// identifier permits, which the CHECKED landing refuses and the unchecked one stores as its low-order digits.</summary>
    Truncation = 4,

    /// <summary>The names an intermediate OPERATION raises (the checked operators of <c>CobolFloat</c> and
    /// <c>CobolNum</c>).</summary>
    Range = Overflow | Underflow,

    /// <summary>Every name: a statement with a SIZE ERROR phrase.</summary>
    All = Range | Truncation,
}
