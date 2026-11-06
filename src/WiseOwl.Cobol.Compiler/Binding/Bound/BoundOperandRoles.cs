// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ A BOUND NODE THAT HOLDS OPERANDS ITS SOURCE STATEMENT NEVER WROTE (X3.23-1985 USE FOR DEBUGGING general rule 5: a debugging section
/// runs for a statement that "explicitly references identifier-1"; docs/CONTROL_FLOW_DESIGN.md D20). The binder lowers a written
/// statement into a richer one — it expands MOVE CORRESPONDING into its pairs, it makes the RELATIVE KEY item the key of a START that
/// wrote no KEY phrase, it sends the record area through the implicit move of READ INTO, it reads a condition-name's conditional
/// variable — and each of those operands is in the tree because the statement MEANS it, not because the program SAID it.
/// <para><see cref="BoundReferences"/> reads every operand a node stores and leaves out exactly the ones a node names here, by
/// identity (the node, place or operand object itself), so the answer is the written statement's however the lowering grows.</para>
/// </summary>
public interface IBoundImplicitOperands
{
    /// <summary>The operand objects (a place, an operand node, a pair list) the node holds but its statement did not write.</summary>
    IEnumerable<object> ImplicitOperands { get; }
}

/// <summary>
/// A statement leaf that UNCONDITIONALLY transfers control out of the statement sequence it stands in (GO TO, EXIT PARAGRAPH, EXIT SECTION,
/// EXIT PERFORM, NEXT SENTENCE, STOP RUN, GOBACK, EXIT METHOD): the emitter's <c>Visit</c> for it returns <c>true</c>. A debugging section a
/// statement enclosing it owes AFTER that statement (X3.23-1985 USE FOR DEBUGGING general rules 5 d and 6 c) runs before the transfer
/// (<c>DebugTriggerEmitter.LandBeforeTransfer</c>), because no code after the transfer is reached.
/// </summary>
public interface IBoundTransfersControl;

/// <summary>A READ or RETURN statement, which delivers a record and then, only when it did, moves it to the INTO receiver. The INTO phrase
/// is conditionally executed (X3.23-1985 USE FOR DEBUGGING general rules 5 and 6: "If identifier-1 is specified in a phrase that is not
/// executed or evaluated, the associated debugging section is not executed"), so the planner treats it as a moment of its own.</summary>
public interface IBoundIntoPhrase
{
    /// <summary>The implicit move of the record area into the INTO receiver, or null when the statement wrote no INTO phrase.</summary>
    BoundMove? IntoMove { get; }
}
