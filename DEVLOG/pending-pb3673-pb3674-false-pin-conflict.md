## PB3673 and PB3674: a locked SourceGen.dll became a false RuntimeAbi.pin CONFLICT

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
