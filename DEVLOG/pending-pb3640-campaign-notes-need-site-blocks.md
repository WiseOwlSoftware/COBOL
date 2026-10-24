## PB3640 part 1: a campaign's notes need a site block whatever their id; PB3509 retired as PB3508's duplicate

Wave 20261031 group C (PB3509, PB3640). The wave stopped on the fleet's stop file after part 1; the rest of PB3640 is
named in its note (Progress) for a finisher, and this entry is extended by whoever lands the rest.

**PB3509 (retired).** Re-probed on a build of main at 300fd78bd: the defect it describes (a carrier's DONE report
supersedes the carried branch's FINDINGS report in the planner's `newest`) is PB3508's, filed from the same wave-1143/1144
witness and landed before this note was picked up. `plan_wave.add_closed_notes` already gives no report a note that a
contained unlanded report branch also closes. Emptying that `carried` set turns PB3508's check 6b'''00 red with exactly
this note's symptom, and restoring it turns it green; the planner's self-test is 382/382 on the unmutated tree. The sibling
sweep (every reader of `Report.notes` after `add_closed_notes`) found each reader on the corrected value. Retired with a
`closes_rows_reason`; no code change.

**PB3640 part 1: complete site blocks.** The partition cannot know what a note changes unless the note says so, and the
register required a site block only of notes filed at PB3443 or later (`SITES_REQUIRED_FROM`). Wave 20261030 ran PB3628
beside PB3275 and PB3310, three PB1957 campaign notes, two of them unsited, and PB3628 and PB3275 both rewrote one rule of
`prune_worktrees.lifecycle`. `work.sites_problems` now requires the block of every open non-decision note that sits in a
cluster some note's `tier:` names (`work.campaign_leads`), whatever its id: the register has no list of running campaigns
and must not grow one, and a `tier:` entry is the owner's word that a campaign's ready notes are ranked and planned. The
rule refused the 11 open PB1957 notes with no block (PB2665, PB3113, PB3118, PB3135, PB3157, PB3367, PB3386, PB3389,
PB3404, PB3409, PB3413); each now declares its sites. `test_plan_wave.py` pins the rule next to the PB3263 cases
(tiered note, untiered member of a tiered cluster, a cluster no note tiers, a decision, a terminal note, a `none`
declaration, and `campaign_leads` with malformed entries). The planner's self-test is 385/385.
