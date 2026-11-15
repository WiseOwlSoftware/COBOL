// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

// The VALIDATE statement's bound node (ISO/IEC 1989:2023 §2023:14.9.50; ISO/IEC 1989:2002 §14.8.46; docs/rearchitecture/DESIGN-validate-facility.md).
// Expanded at BIND time as INITIALIZE is (BoundInitialize): the binder walks the operand's elements and binds each stage's test as a
// BoundCondition, so the emitter renders five passes over one element list and owns no rule of its own.

/// <summary>One <c>VALIDATE identifier-1</c> (§2023:14.9.50.4 GR3: a statement naming several identifiers is the same as one statement per
/// identifier, in order, so the binder emits one node per identifier-1 in a <c>BoundImplicitSeries</c>). <see cref="Written"/> is identifier-1 as
/// the statement wrote it — the one operand the statement explicitly references; <see cref="Steps"/> are "the elements of the operand"
/// (GR2: the operand and every subordinate item except those of class index, object or pointer), in definition order, each carrying the
/// tests of the stages that apply to it (GR4), a table's elements inside the loop over its occurrences (GR7 b).</summary>
public sealed record BoundValidate(Place Written, IReadOnlyList<ValidateStep> Steps) : BoundStatement, IBoundImplicitOperands
{
    /// <summary>Every element of the operand, the loops' bodies included, in definition order.</summary>
    public IEnumerable<ValidateElement> Elements => Flatten(Steps);

    /// <summary>The places of the expansion that the program did not write: every element below <see cref="Written"/>.</summary>
    public IEnumerable<object> ImplicitOperands
    {
        get
        {
            foreach (var element in Elements)
                if (!ReferenceEquals(element.Place, Written)) yield return element.Place;
        }
    }

    private static IEnumerable<ValidateElement> Flatten(IReadOnlyList<ValidateStep> steps)
    {
        foreach (var step in steps)
            switch (step)
            {
                case ValidateElement e: yield return e; break;
                case ValidateLoop l: foreach (var inner in Flatten(l.Body)) yield return inner; break;
            }
    }
}

/// <summary>One step of an expanded VALIDATE operand: an element, or the loop over one table dimension.</summary>
public abstract record ValidateStep;

/// <summary>One element of a VALIDATE operand. <see cref="FormatValid"/> is stage one's test (§2023:14.9.50.4 GR6 a: "Each elementary data item is
/// checked for compatibility with the PICTURE, SIGN, and USAGE clauses, as applicable"), true when the content is valid for the item's description.
/// Only an element with a stage to take part in is bound: an alphanumeric item accepts any content, and a group is checked through its elementary
/// items.</summary>
public sealed record ValidateElement(Place Place, BoundCondition FormatValid) : ValidateStep;

/// <summary>One OCCURS dimension below identifier-1 (§2023:14.9.50.4 GR7 b: "each occurrence of the data item to be processed independently of the
/// other occurrences at all five stages"): <see cref="Body"/> once per occurrence 1..<see cref="Count"/>, the count being the table's current number of
/// occurrences (the fixed count, or the value of the DEPENDING item, GR7 c; <c>OccurrenceCounts.Current</c>). <see cref="Var"/> is the loop counter by
/// its identity (<see cref="MethodLocal"/>): the backend names it.</summary>
public sealed record ValidateLoop(MethodLocal Var, AllCount Count, IReadOnlyList<ValidateStep> Body) : ValidateStep;
