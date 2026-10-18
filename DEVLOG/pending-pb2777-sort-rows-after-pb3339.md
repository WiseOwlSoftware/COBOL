## kb/Work PB2777 re-scoped after PB3339; PB3339's member name corrected

PB3339 landed the bounded-memory SORT and MERGE (pull request #201). That answers the question the R2 analysis PB2777
raised: document a capacity limit, or design a spill path. PB3339 was filed without naming PB2777, and its branch
review caught the duplicate. PB2777 now carries only what remains of its first step: SORT and MERGE rows in
`scripts/arch/perf_baseline.py`, one size in memory and one that spills, against GnuCOBOL at 1x, 2x and 4x. PB3339's title
and defect bullet named the store `SortFileTable.Records`, a member that does not exist; it was `SortStore.Records`.
