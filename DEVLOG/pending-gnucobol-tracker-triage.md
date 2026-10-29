## PB3838 tier 6: every GnuCOBOL record checked against the tracker and re-run on the development line

The owner asked for our GnuCOBOL records to be triaged against the bugs already reported upstream before any is filed.
All 78 `upstream` notes now carry the result in their `## Upstream state`.

**The tracker.** A script harvested all 1,801 tickets of GnuCOBOL's three SourceForge trackers (1,178 bugs, 508 feature
requests, 115 patches) through the Allura REST API, serially, after parallel requests hit SourceForge's rate limit.
Four read-only matchers compared every note with the tickets. An independent skeptic re-read each claimed duplicate and
overturned four of them. Of the 78 notes, 21 are already reported, so they become `upstream_status: reported` with
`upstream_ref` naming the ticket; 28 have related tickets only, and 28 have none. Most of the 21 are long-standing bugs
or feature requests, where another report would add nothing. PB3876's ticket, bugs #1103, was closed as invalid,
although the maintainer later conceded in the thread that the parse is wrong.

**The development line.** GnuCOBOL's 3.x development line, 3.3-dev (SVN r5751, from the project's CI source snapshot of
2026-10-29), was built under WSL with Berkeley DB. It needed no autotools, because the snapshot ships `configure`.
Four read-only re-runners replayed every note's repro on 3.2.0 as the control and then on 3.3-dev. Results:
- 74 still present.
- 3 changed but still wrong. PB3874's SET to NULL is now an ordinary error, but the object-reference relation still
  crashes. PB3891's FUNCTION ORD of a national item now aborts as not implemented. PB3900's edited floating-point MOVE
  still fails, in a renamed routine.
- 1 fixed: PB3899, the MOVE with high-order truncation into a numeric-edited item. It is now `fixed-upstream` and
  retired.

The owner agreed the filing plan:
1. Comment upstream only where our record adds something.
2. Group families into one report each.
3. Introduce the work to the maintainers once before any ticket.
4. File in small batches, every text approved by the owner.

PB3838 records it.

Two intermittent Windows self-test failures bounced the night-8 notes pull request (#755) out of the merge queue twice:
PB4046 (new, `test_orchestrate_adopt.ps1`'s restart scenario) and PB4008 (open, the dead-pid arm of `impact_lane.py`'s
self-test), which gains that run as a witness.
