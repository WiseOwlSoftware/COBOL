## PB3528: two split branches that share a file no longer deadlock the planner

**What.** Since this morning every wave planned nothing. The two branches wave 1141 left unfinished (PB3338's and
PB3493's) both change `docs/CONFORMANCE.md`. The planner reads an idle unfinished branch as running work, so each
branch's finisher waited on the other branch, and eight more notes on that file waited behind both.

**Fix.** A branch that another group of the same wave is finishing is planned work, not a hold. The group that meets it
now follows that group as its successor, so the first finisher goes ahead and the second merges its branch afterwards.
A branch nothing in the wave finishes still holds, as before.

**Witness.** The planner's self-test plans two split branches sharing a file: both finishers are planned, one after
the other. It failed before the fix and all 332 checks pass after it. On the live state, a dry run now plans six groups
(the two finishers first) where it planned none.
