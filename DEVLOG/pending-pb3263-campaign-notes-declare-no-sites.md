## kb/Work PB3263 widened: 30 of the 42 open SORT and file I/O campaign notes declare no sites, so the planner partitions none of them

Wave 1117 planned eight groups for the PB3353 campaign, and six of them went out with an empty file set: PB320, PB604,
PB671 with PB707, PB675, PB680 and PB700. Their notes predate the `Code site` block that PB3242 made the declared file
set, so `make_dispatch_specs.py` recorded zero files for each and the planner checked them against nothing. The same
run deferred eight other notes for collisions with running work, because those notes do declare their sites.

A census of PB3353's open blockers through `work.note_sites` found 30 of 42 with no site block. PB3263 already named
the mechanism for the PB3160 conformance-suite slices, so it now carries this second population, and its fix shape is
one rule for every campaign: the planner treats an empty declared set as unknown, `work.py check` refuses an open
campaign note with no site block, and the listed notes get their `**Sites:**` paragraphs. The cost of leaving it is a
wasted landing, not a wrong answer: two groups that edit one file both run and the second takes a finisher.
