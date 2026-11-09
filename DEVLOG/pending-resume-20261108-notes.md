## PB4011 recurs on PB4745's report; PB4885 records why wave 20261937 group E stopped

The resume unit after campaign PB4812's wave 20261937 found the PB4745 branch (worktree-wf_1501f6ad-7e3-25) stranded as a
DECISION for about 8 hours. Its review round 2 was CLEAN and its gate GREEN at the tip, but the finisher's comma-separated
header made `reports.sha_field` read a predecessor's head out of a parenthetical aside (kb/Work PB4011). The unit rewrote
the scratch report's header with the template's ` · ` separator, and the branch now reads WAITING TO LAND. PB4011 records
the recurrence and measures two consumer-side regex fixes over the 1,399 reports on disk. Neither is safe on its own, so
the producer-side check (fix item 2) stays the fix.

PB4885 now records why group E made no commits: the 1985 conformance statement is generated from the 1985 inventory
(PB4886 step 4, PB4997), which does not exist yet. The note is now blocked on PB4997 as well as PB4850.
