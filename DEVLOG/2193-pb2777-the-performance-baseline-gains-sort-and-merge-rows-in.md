## Entry 2193 — 2026-10-18 03:50 PDT — PB2777: the performance baseline gains SORT and MERGE rows, in memory and spilling, and a peak-memory measure

kb/Work PB2777 (re-scoped 2026-10-17 after PB3339's bounded-memory SORT landed) asked for SORT and MERGE rows in the
performance baseline, one size that fits the sort memory limit and one that spills, timed against GnuCOBOL 3.2 at 1x,
2x and 4x, plus the PEAK memory that `scripts/arch/perf_baseline.py` did not measure. Allocated bytes cannot witness
PB3339's bound (review R1-1): a run that keeps every record alive allocates no more than one that frees them.

**The programs.** `tests/Cobol.Net.Benchmarks/Programs/SORTHOT.cob` releases N 80-byte records in scrambled key order
(key = MOD(I * 7919, N), a permutation of 0..N-1 because 7919 is prime to every scaled N) through an INPUT PROCEDURE
and returns them through an OUTPUT PROCEDURE, counting each key that is not the next of 0..N-1. `MERGEHOT.cob` writes
the keys round-robin to four sequential files and merges them the same way. Both witnesses are computed from the
header formula (N = 100,000: count 100,000, key sum N(N-1)/2 = 4,999,950,000, no key out of order), never copied from a
run; both programs are plain COBOL 85, so GnuCOBOL compiles the same source. They join `GeneratedProgramBenchmarks`'
`[Params]`, so BenchmarkDotNet measures them in process too.

**Two memory regimes, derived.** `perf_baseline.py` now stages a case per program and regime (`hot_cases`): a program
with an SD entry runs under the 128M default (both implementations' default, stated so a host's own setting cannot move
it) and as `<program>-SPILL` under 4M, about a sixth of the smallest scale's records by our resident estimate, so
every scale writes several runs and 4x needs more runs than one merge reads at once. The limit goes to both
implementations (`COBOLNET_SORT_MEMORY`, docs/CONFORMANCE.md §3 D-SWK; `COB_SORT_MEMORY`). No hand list names
the sort programs: a new one gets both regimes from its SD entry.

**Peak memory.** `timed()` now returns the child's own peak resident set: POSIX reaps the child with `wait4`, whose
rusage belongs to that one process (`RUSAGE_CHILDREN`'s `ru_maxrss` is the largest of every child so far, so it cannot
be differenced per run), and Windows reads `GetProcessMemoryInfo` through the handle Popen holds. Every whole-process
row (cold compiles, host-OS runs, both implementations under WSL) gains a `peak/` row through one helper
(`process_rows`), compared with the same noise band as a time. The record's new table shows each sort case's peak
above the start-up floor. The self-test gained two cases: one fires on a planted +50 % peak, and one proves the
measure sees a child's own 256 MiB against an idle child. Both pass on Windows and under WSL.
`PerfBaselineSelfTestDriftTests` holds both arm names.

**What the record shows** (`docs/rearchitecture/evidence/perf-baseline/a13ede547992.md`, 2 sessions; the R0 record
`354c79ef3312.md` stays the architecture review's reference): above the floor, ours SORTHOT-SPILL holds 32.8 / 39.3 /
42.1 MiB and MERGEHOT-SPILL 39.5 / 39.4 / 38.2 MiB at 100,000 / 200,000 / 400,000 records, while the in-memory cases
grow from 51.9 to 125.0 MiB and from 57.9 to 133.8 MiB. SEQHOT, which sorts nothing, already sits about 28 MiB above
the floor, so the spill cases' plateau is the file-I/O runtime plus about the 4 MiB limit. SORTHOT-SPILL's small rise
is uncollected garbage, not retention: a probe sorted 1,600,000 records under a 4M limit with
`DOTNET_GCHeapHardLimit` = 20 MiB. Against GnuCOBOL, net of start-up, ours is 2.5x to 6.0x slower at 1x on these
programs (SORTHOT-SPILL 2.5x, MERGEHOT-SPILL 2.7x, SORTHOT 4.6x, MERGEHOT 6.0x) and 1.7x to 3.7x at 4x. The gap
narrows because ours grows 1.55 to 1.73 per doubling of the records where GnuCOBOL grows 1.87 to 2.22.

**A broken leg, fixed.** The probe found the WSL leg dead: since PB2528 the compiler deploys the Windows ReadyToRun
image of the runtime beside a program compiled on Windows, and Linux .NET refuses it (`BadImageFormatException`), so
every `perf_baseline.py` run since 2026-10-07 18:00 would fail its GnuCOBOL comparison. The leg now deploys, beside
each staged program, the runtime the compiler deploys on Linux (`linux_runtime_image`, resolved as
`AssemblyPackager.ResolveRuntimePath` resolves it).

**Self-review.** Every process row now goes through one helper, so none can lack its peak row. A refused Windows
memory query is a measurement failure, not a missing number. A first draft of the record's text said the spilling
peak stays "near the limit"; the measurement showed a plateau above the floor instead, so it now says nearly flat. The
`hot_cases` derivation keeps the witnesses-equal-`[Params]` self-test true. No inventory row: this is the R2 analysis
note's baseline row (closes_rows_reason unchanged).

