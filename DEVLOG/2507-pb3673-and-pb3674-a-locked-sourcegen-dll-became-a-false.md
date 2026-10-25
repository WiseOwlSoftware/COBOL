## Entry 2507 — 2026-10-24 19:45 PDT — PB3673 and PB3674: a locked SourceGen.dll became a false RuntimeAbi.pin CONFLICT, plus the wave 20261032 leads filed

Wave 20261032's group F (PB3630) came back DONE and reviewed CLEAN. The landing lane's attempt then ended exit 4,
`CONFLICT in src/Cobol.Net.Runtime/RuntimeAbi.pin`, and recorded the branch BLOCKED for a finisher. Neither side had
touched the pin since the merge base. The merge of main was clean. The ABI-pin settle that follows it rebuilds the Unit
tests, and that build failed with MSB3027. The wave unit's own csharp-ls (PID 48964, the LSP server its subagents
shared since 17:47) still held `Cobol.Net.Compiler.SourceGen.dll` in F's `bin`. `merge_drivers.merge` returns a failed
settle in the same shape as a conflicted file, so `enqueue.py` called it a conflict.

PB3673 files the misclassification: a settle that cannot measure needs its own outcome and verdict, and an
environmental build failure should be retried by the lane, not sent to a finisher. PB3674 files the lock. The
code-navigation server keeps a build output loaded for the life of the session, so every later build in a finished
agent's worktree fails. PB3339 recorded this as an operator "trap" and never filed it. Both notes join the PB1957
campaign. The unit stopped its own csharp-ls and re-ran `enqueue.py land` for F instead of dispatching a finisher.

The same landing files the open leads of that wave's implementers and reviewers as PB3675 to PB3687: the PublishHolder
null that hides two causes, the waiting hold's single retry, the Format-1 CALL digits of a STANDARD-DECIMAL function,
the node-shape test for a parenthesized function, stale EC-DATA-CONVERSION commentary, an unwitnessed packed and float
view, status 91 ending the run unit, the GROUP-USAGE screens that skip TYPEDEF trees, the COBOL-85 report SIGN
screen, a design-doc cost table, an over-2-GiB journal test that never leaves byte 70, the RETURNING lane of PB3162 and
a mis-cited rule in PB3664. A staging-race flake was added to PB3453 as a second witness. Leads the reports already
answered as fixed, and the PB3666 sibling that PB3666 already records, were not filed again.

