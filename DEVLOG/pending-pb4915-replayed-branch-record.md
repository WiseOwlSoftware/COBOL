## File PB4915: a replayed branch stays a DECISION until someone files its abandonment

A note-only change. On 2026-11-07, campaign PB4812's planning stopped three times on branches whose commits a FINISH-FIRST
successor had cherry-picked and landed. Each replay edited a line, so the old branch was no longer patch-equivalent to main,
and the branch survey read it, correctly, as a DECISION. The planner held its members, and each stop cost a note-only
landing to record the branch ABANDONED. PB4915 makes the replaying agent record that decision in the same change set as the
replay, and makes the landing delete the recorded branches.
