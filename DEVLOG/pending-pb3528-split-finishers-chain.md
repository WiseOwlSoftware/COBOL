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

**Linux.** The first landing attempt stopped on the Linux gate: the same check named the other finisher the successor.
The planner reads the reports oldest first, by wave and then modification time, and the test's two reports share
both. The remaining tie fell to the order the directory lists files in, which is the name on Windows (NTFS) and a hash
order on Linux (ext4). That order also decides a note's newest report and which finisher goes first, so the plan
itself depended on the host. The reports are now ordered by name after those two keys. A new check reads the reports
through a reversed listing and expects the same order: it fails without the fix on Windows too, and 333 of 333 checks
pass with it. Another place where a file's modification time picks a record, the architecture review's choice of its
newest performance baseline, is filed as PB3532.
