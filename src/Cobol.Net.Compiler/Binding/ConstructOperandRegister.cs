// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>What <see cref="ConstructOperandRegister{TKey}.Register"/> saw for one operand.</summary>
internal enum ConstructOperand
{
    /// <summary>New to the enclosing scope — bind it.</summary>
    First,

    /// <summary>Already registered by THIS construct. Either the operand is written twice inside one construct
    /// — no rule forbidding it "in more than one &lt;construct&gt;" is violated — or the construct's first
    /// occurrence of it already produced the <see cref="Duplicate"/> verdict. Either way the operand is already
    /// accounted for: bind it ONCE, diagnose it ONCE.</summary>
    Repeated,

    /// <summary>The first occurrence in this construct of an operand an EARLIER construct of the same scope
    /// already specified — the rule is violated, and this is the one site to say so.</summary>
    Duplicate,
}

/// <summary>
/// ONE duplicate-operand screen for every rule whose boundary is a CONSTRUCT (a statement, a clause) and whose
/// subject is an OPERAND written inside it — "the same X shall not be specified in more than one Y".
/// <para>The recurring defect this type exists to make impossible is a <c>HashSet</c> screen declared OUTSIDE the
/// loop over the constructs while <c>Add</c> runs inside the per-operand loop: that screens per OPERAND across
/// all constructs at once, so ONE construct naming the same operand twice is rejected by a diagnostic whose own
/// text ("in more than one …") the program falsifies. The standard's general formats put the repeat inside one
/// construct beyond reach of these rules: per ISO §5.2.7 an ellipsis makes the preceding brace/bracket group
/// repeatable ("the ellipsis applies to the portion of the format between the determined pair of delimiters")
/// and no rule adds a distinctness requirement to the repetition, so one construct MAY write an operand twice —
/// and one construct is never "more than one".</para>
/// <para>Each key therefore remembers the ORDINAL of the construct that MOST RECENTLY registered it, and
/// <see cref="EndConstruct"/> is a counter bump: the same ordinal ⇒ this construct has already registered the
/// operand (a repeat inside one construct — legal and already bound — or a second occurrence of a violation this
/// construct has already been told about), an earlier ordinal ⇒ the repeat across constructs the rule forbids.
/// Remembering the LAST rather than the FIRST construct is what keeps ONE violation to ONE diagnostic: under a
/// first-seen ordinal BOTH writes of <c>USE … ON F F</c> in a second USE statement — and both of
/// <c>COLLATING SEQUENCE OF K K</c> in a second clause — compare against the ordinal of the construct that FIRST
/// specified the key, so the rule is reported TWICE for one violation. That is a measurement, not a reading:
/// flipping <see cref="Register"/> back to first-seen turns all seven
/// <c>conformance:*_DiagnosedOnce</c> rows red at once
/// (<c>ExceptionConditionConformanceTests.UseF1_SameFileRepeatedInTheSecondStatement_DiagnosedOnce</c> ×4 editions,
/// <c>FileCollatingSequenceSpecTests.KeyLevel_SameKeyRepeatedInTheSecondClause_DiagnosedOnce</c> ×3) — fired red
/// on 2026-09-05 before this line was written.</para>
/// <para>Consumers (add the next one here — this list is the reason the type is shared, not private):</para>
/// <list type="bullet">
///   <item><description><c>ProcedureTableBuilder</c> — ISO §14.9.49.3 SR7 (open modes: "may each be specified
///     only once in the declaratives portion of a given procedure division"), SR8 ("The same file-name shall not
///     appear in more than one USE AFTER EXCEPTION statement within the same procedure division"), SR9 (report
///     group) and SR14 (the exception-name/file-name pair); the construct is the USE STATEMENT (kb/Work
///     PB364).</description></item>
///   <item><description><c>DataBinder.ResolveFileCollating</c> — ISO §12.4.5.7.3 SR8 ("Neither data-name-1 nor
///     record-key-name-1 shall be specified in more than one COLLATING SEQUENCE clause"); the construct is the
///     Format-2 COLLATING SEQUENCE CLAUSE, whose §12.4.5.7.2 figure writes
///     <c>OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3</c> (kb/Work PB703).</description></item>
/// </list>
/// </summary>
/// <typeparam name="TKey">The operand identity the rule compares — a name, an index, a model reference.</typeparam>
internal sealed class ConstructOperandRegister<TKey>(IEqualityComparer<TKey>? comparer = null)
    where TKey : notnull
{
    private readonly Dictionary<TKey, int> _lastSeenIn = new(comparer);   // key → most recent construct ordinal
    private int _construct;

    /// <summary>Register one operand of the construct being bound.</summary>
    public ConstructOperand Register(TKey key)
    {
        if (!_lastSeenIn.TryGetValue(key, out int seenIn))
        {
            _lastSeenIn.Add(key, _construct);
            return ConstructOperand.First;
        }
        if (seenIn == _construct) return ConstructOperand.Repeated;
        _lastSeenIn[key] = _construct;           // this construct has now been told; a third write stays silent
        return ConstructOperand.Duplicate;
    }

    /// <summary>End the construct — the next operand registered belongs to the next one.</summary>
    public void EndConstruct() => _construct++;
}
