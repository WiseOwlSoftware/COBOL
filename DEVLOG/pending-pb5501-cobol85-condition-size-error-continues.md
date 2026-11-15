## PB5501: a COBOL-85 size error in a condition continues the program and the relation is false

**The defect.** At `--std 85`, `IF 5 / Z > 1` (Z = 0) ended the run unit with `abnormal run-unit termination:
EC-SIZE-ZERO-DIVIDE (fatal)`, exit 1, re-probed on this tree before the fix. PB5484 had made a COBOL-85 arithmetic statement
continue and left the condition as a lead: §1985:VI.6.4.2 GR3 governs "an arithmetic statement", and a condition has no
resultant and no statement of its own.

**The determination.** §1985:B.2.2 item 45 is general: "If a size error condition occurs and an ON SIZE ERROR phrase has not been
specified, the program will continue", and 1985 has no fatal exception condition. So the program continues. The text gives the
relation no truth value; WiseOwl COBOL's choice (DOC-A.1-70) is that a relation or sign condition whose
arithmetic expression cannot be evaluated is false, its own NOT inside it (`NOT >`, `IS NOT POSITIVE`), so a logical NOT outside it
is true and a sibling leaf of an AND or OR is evaluated as written. GnuCOBOL 3.2.0 (`cobc -std=cobol85` under WSL, run on the
golden and a probe of sixteen more relations) continues too, but its truth values follow no rule: the not-a-number quotient
compares like an unbounded value of the dividend's sign, yet `B / Z * 0 = 0` is true, `M ** 0.5 < 0` is true and `Z ** 0 = 1` is
true because it computes 0 ** 0 as 1 where §1985:VI.6.2.3 GR5a makes it a size error. DOC-A.1-70 now records that, and the
determination stands.

**The fix.** `ConditionRenderer.SizeErrorGuarded` wraps the leaf of `Visit(BoundRelational)` and `Visit(BoundSignCondition)` in
`CobolSizeError.FalseWhenRaised(() => leaf)` when `DialectBehavior.UnphrasedSizeErrorIsFatal` is inactive (the one 85-vs-2002
fact PB5484 added, VCR row 7.71) and an operand reaches an arithmetic operation, as the operand itself or inside a function
argument at any depth (`ArithmeticReach`, a visitor over the generated bound-expression and operand interfaces). The first cut
tested only the operand's own top node, so `IF FUNCTION INTEGER (B / Z) > 1` still ended the run unit (the branch review's R1-1,
re-probed and fixed). A relation none of whose operands reaches one renders as before. EVALUATE WHEN, PERFORM UNTIL and SEARCH WHEN render through the same two leaves. From 2002 nothing
is wrapped. The branch carried PB5484's branch merged in, because that fact was not yet on main; PB5484's review moved the fact from
`EditionInfo` into the `DialectBehavior` registry, and the merge of main re-pointed this consumer at it.

**Goldens.** `pb5501_size_error_in_condition_continues` (85; eight conditions, one a function argument, and a PERFORM UNTIL), `SizeErrorDispositionTests.
Cobol85_SizeErrorInACondition_ContinuesWithTheLeafFalse` (13 rows: four of them function arguments, two rows no-error controls) and its 2002 control
`Cobol2002_SizeErrorInACondition_StaysFatal`. No refusal: 85 is the lowest edition. No inventory row closed.

**Not covered (registered).** A reference modifier's arithmetic expression is evaluated into a temporary before the
statement (`StatementBinder.SubscriptSegments`), outside any condition leaf, so `IF X (B / Z : 1) = "A"` still ends the run
unit at 85, as DOC-A.1-70 says of every 1985 reference-modifier size error; that is a separate mechanism, left as a lead.
