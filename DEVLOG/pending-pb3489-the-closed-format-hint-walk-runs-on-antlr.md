## PB3489: the closed-format hint walk runs on ANTLR's own simulator; a nested-function syntax error no longer hangs

**What.** Last night's differential fuzz run found the compiler hanging on a typo: `MOVE FUNCTION DATE-OF-INTEGER(FUNCTION
INTEGER-PART(FUNCTION NUMVAL(FUNCTION SUBSTRING(A FROM 1 FOR 4)))) TO N`. One or two calls deep the compiler reported
COBOL0001 near 'FROM' at once; three calls deep it ran past 90 seconds.

**Cause.** Not the grammar. A stack sample showed the parse had already failed at FROM; the time was in
`CobolErrorStrategy.ClosedFormatResidue`, the hint that names a closed general format when a reserved word stands where
a clause list's error production would be. It re-simulated the tokens the failed prediction read with a closure of its
own and one linked call stack per path, and every function argument reaches a function-identifier along many paths, so
the stacks multiplied per nesting level: 257, 19,585 and 320,041 configurations at depths 0 to 2.

**Fix.** The first repair interned the stacks into a graph of the compiler's own. The owner asked why we were
duplicating what ANTLR already has, and the walk now IS ANTLR's: `ResidueWalk` subclasses `ParserATNSimulator` and runs
its full-context `Closure`, its `PredictionContext` stack graph seeded from the parser's real invocation stack, and its
merge; the answer is read off the final configuration set. The compiler's closure and its stack records are deleted.
Two shortcuts were refused for stated reasons: the exception's `DeadEndConfigs` (SLL follows a rule's end into every
caller) and `ComputeReachSet` (it skips the closure once one alternative remains).

**Witness.** `ResidueWalkNestingTests` measures the compiling thread's allocation at depths 0–2, never the clock
(PB1590). The first version counted configurations, and the branch review showed why that cannot work: ANTLR merges
configurations per state, so a count never sees the stacks that multiplied. Allocation does, measured WARM: the second
review round showed that a cold compile also fills the parser's process-wide shared DFA cache, so its bytes depended on
which compiles ran earlier in the test process (the second level's ratio to the first ran from 0.006 to 13.5 by the
schedule, and the correct walk could fail). Each depth is now the least of three compiles after a discarded one at
that depth, identical whatever ran before or alongside: the fixed walk allocates 1,308,848, 1,773,072 and 2,773,584
bytes (the second level 2.16 times the first), the old per-path walk 1.29, 3.98 and 58.2 MB (20.1 times). The bound,
six, is the geometric mean of the two rounded down; the test went red on the old walk at 54,200,048 against 2,690,280.
It also checks the diagnostic at depths 1, 3 and 5. The repros,
the fuzz program and the sibling sweep (subscripts, reference modification, parentheses, IF, DISPLAY, CALL at depth 6)
answer in under a second.

**Found on the way.** The sweep turned up a different hang, on LEGAL source: four nested function calls in any EVALUATE
operand never finish, inside ANTLR's own prediction of `evaluateSubject` and the WHEN object. Filed as PB3504 with the
measurements; it needs a grammar redesign of the EVALUATE operand rules.
