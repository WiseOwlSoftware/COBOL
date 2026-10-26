## The leftover leads of waves 20261500 to 20261503, filed

Seven branches landed late on 2026-10-25, after the lead sweeps that filed the neighbouring waves, so their reports' leads were
never filed: w20261500c (PB3677), w20261501b (PB498), w20261501e (PB1601), w20261501f (PB935), w20261502a (PB3769), w20261503a
(PB3637) and w20261503b (PB3727). Each lead was re-read on main first; PB3122 and the PB3802-PB3812 notes (PR #646, now on
main) were grepped and none covers these mechanisms. Twelve notes are filed, one per mechanism.

PB3814: the reader side of the bin/obj race (about a dozen Unit drift sweeps walk tests/ with AllDirectories; PB3677 fixed only
the writer). PB3815: about 15 inline `Usage.Display or Usage.National` spellings beside `UsageFamilies.IsDisplayOrNational`.
PB3816: a VersionConformancePass doc comment that still calls the 8,191 concatenation cap edition-invariant. PB3817: INITIALIZE
REPLACING an unrestricted program-pointer into a restricted one compiles clean (GR25, under_rejects, silent). PB3818: the pb817
golden header names a negative golden that does not exist. PB3819: six drift tests each split build-and-test.yml into jobs their own way.
PB3820: the supersede job's checkout step is not continue-on-error, so housekeeping can fail ci-gate. PB3821: the live-tree
listing and `scrubbed_env` are written more than once. PB3822: the standard_streams COPY rule matches one physical line.
PB3823: the ensure_utf8 idempotence claim. PB3824: is_drivers_merge judges a merge made under the old drivers by the new ones.
PB3825: "a new driver lands alone" is design prose with no check.

Corrected in place: PB3637 said DESIGN-orchestrator-loop.md 16.10 is not on main; it is, so the sentence carries a dated
correction. Not filed: PB3727's stale "two corrections" count (its note already says three; the stamped DEVLOG entry is left
alone), the design-doc gap for merged_tree (3.17.2 step 2 names it on main now), PB3677's X(24) CALL observation (nonconforming
source, the report leaves it) and PB1601's "coverage" lead (the report says not filed). The sparse-checkout import lead is PB3787
and PB3810. Unused ids: PB3826, PB3827, PB3828.
