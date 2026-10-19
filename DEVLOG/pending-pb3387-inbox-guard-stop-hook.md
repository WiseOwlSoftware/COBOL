## PB3387: an attended turn no longer ends while its inbox watcher is unarmed, and the process table reads in 0.1 s

On 2026-10-18 the operator's inbox watcher fired at 14:40 on a Mythos message and was never re-armed. The session read
past it, and past the ⛔ "NO watcher armed" line the 15:57 compaction's probe printed. Nine messages waited until
17:14: three landing requests for the reviewed engine branches, five more Mythos messages and the loop's ledger
publish notice. The owner asked: "WHY ARE YOU FAILING TO READ MESSAGES FROM mYTHOS?" PB2814 had made the watcher
state visible everywhere a session reads, but a line to read is not a hold.

`scripts/hooks/inbox_guard.py`, a new Stop hook, asks `mailbox.status` at every attended turn's end and refuses the
stop with the ⛔ line while it reads false, so the session acts on the inbox and arms the watcher first. A loop unit,
a host without the coordination directory and an armed session pass silently, and a turn already continued for a Stop
hook gets a warning instead, so the hook never loops. The check had to be cheap enough to run on every turn:
`coord.process_table` asked PowerShell CIM for `Win32_Process` (`mailbox.py status` took 12.5 s), and now it reads one
Toolhelp32 snapshot plus each command line through `NtQueryInformationProcess` (0.12 s for 395 processes; `status`
0.7 s). `coord.process_name`, which asked `tasklist` (6.5 s), reads the same snapshot. The loop design doc's mailbox
section, `mailbox.py`'s watcher comment and the session-start skill name the hook.
