## PB3723: the two merge-queue conflicts of wave 20261107, resolved before they bounced, measured

The loop's enqueue unit ran the merge queue's entries in order through `git merge-tree`. Two loop branches would
conflict with an entry ahead of them: #580 (500-26, group D) behind #579 (500-24, H), and #586 (500-20, E) behind
#576 (500-21, C). One conflict finisher per branch merged the queued predecessor's head before GitHub removed the
entry, gated it GREEN on both hosts and had its merge re-reviewed CLEAN.

kb/Work/PB3723 now records each conflict's site, its resolution, whether a driver could have resolved it, and the
finisher's minutes:

- **D/H** is revise/revise in DESIGN-test-build-ci.md (10 min), not the insert/insert the simulated merge suggested.
- **E/C** is three edits that do not overlap, inside one 4 KB single-line row of MANDATORY-PRACTICES.md (12 min). A
  sentence-level merge within the line would have resolved it.

The note also gains a fix direction: the landing survey should run the same queue simulation every tick, so a
foreseen conflict gets its finisher while the predecessor is still queued (a `merge_conflict` removal has no failed
run for anything to read, PB3772). No code changed.
