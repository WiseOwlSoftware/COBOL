## Entry 3926 — 2026-11-22 14:28 PST — The code-read landing check refuses any change that un-pins a stored read, whatever the edition's status (PB6056 follow-up)

DEVLOG 3924 landed PB6056's check with a condition: it refused only while the edition read COMPLETE on main. Review lead
R1-1 showed what that costs. 1985 read NOT COMPLETE only for open work notes, so worktree-wf_ef90b3a1-3e3-1 (3 rows)
and -3e3-5 (1 row) would have landed and re-opened four more reads. Decision 2026-11-22 (in kb/Work/PB6056.md): the
check refuses a change that moves the code a stored read of any edition names, whatever that edition's status on main.
A branch carrying the re-read passes. Lead R1-2: the store, schema and rows are now read at the revision the check
judges, never from the working tree. Self-tests case_20 (an edition open on main is still refused) and case_22 (an
uncommitted store edit changes nothing) are added. The check refuses 3e3-1 and 3e3-5 as they stand, each merged with
main. The finisher's gate was GREEN at 3bba3728a4.

