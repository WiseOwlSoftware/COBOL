## kb/Work PB3263, PB3386: the empty file set reaches the landing check, and a merged SPLIT predecessor reads DECISION

Two leads from the Mythos lane, filed by the operator's clerk. PB3263 (campaign notes get an empty declared file set)
gains its third population, the PB3330 engine's wave 1118. Every group was recorded with zero files, because its code
sites did not exist yet. On 2026-10-18 at 15:28 the landing check stopped `worktree-wf_14a1e62b-326-32`, reviewed
CLEAN, by binding it to two R3 notes' sets computed on the spot, and its message blamed a member_index hole. The Mythos
lane re-declared the sets from each branch's diff by hand. The fix shape gains a fourth part: the landing check treats
an empty recorded set as unknown and names it. PB3386 (new): `prune_worktrees.contained()` carries a DECISION row only
when its report is FINDINGS-stamped, so a SPLIT predecessor that its successor merged still reads DECISION and asks for
a land-or-abandon decision about work already landing through the successor.
