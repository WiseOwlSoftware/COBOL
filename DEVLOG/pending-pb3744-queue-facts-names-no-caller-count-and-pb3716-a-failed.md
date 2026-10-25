## PB3744, PB3716: queue_facts.py names no caller count, and a failed bounce download no longer deletes the previous results

**PB3744 re-probe.** The module docstring of `scripts/orchestrator/queue_facts.py` opened with "Two readers use it and
nothing else asks GitHub", then counted "a third caller" (`fetch_bounce_results`, PB3644) and "a fourth" (`live_heads`,
PB3769). `enqueue.driver_file_holders` (PB3642), and through it `land_lane.driver_files_free` on every exit-16 wait
tick, asks `read` too and was named nowhere. **Fix.** The paragraph says the module is the one reader, names what each
operation answers and points to the grep that finds the callers, so no count can rot again. A sweep for the count in
`docs/` and `scripts/` found no second copy (DESIGN-orchestrator-loop.md section 4.9 names no number).

**PB3716 re-probe.** The note was written against the pre-landing A3 branch, whose message said "dest left as it was".
On the landed code the message already read "dest is empty", so the wording no longer reproduced; the root did.
`fetch_bounce_results` ran `shutil.rmtree(dest)` before `gh run download`, so a transient failure (network, rate limit)
destroyed the previous bounce results, and a download that died half-way left partial artifacts, with no provenance
marker, in `dest` under a message that called it empty. **Fix.** The download lands in the sibling `<dest>.fetching`;
`_swap_in` renames the old `dest` aside, moves the whole staging directory into place and restores the old one if the
move fails. A failed download therefore leaves `dest` exactly as it was, and the line names what it still holds
(`_holds`: the run its marker names). This is the contract the docstring and DESIGN-test-build-ci.md already stated
("left as it was when GitHub cannot be asked"); the design doc now says the download failure belongs to it. The one
earlier self-test arm that asserted the opposite (the directory emptied on a failed download) is replaced.

**Tests.** Four self-test arms in `queue_facts.py --self-test`: a failed, partial download over a populated directory
leaves every file, the marker and no staging; the same over a never-fetched directory leaves nothing and says so; a newer
bounce replaces the older one whole; a swap whose rename fails restores the old directory. **Sweep.** The other
`shutil.rmtree` sites in `scripts/`: `capture_oracle` rebuilds its output directory by contract (nothing earlier is
promised), `compare_oracle` already copies after success; no other cache is deleted before its fetch.

**Register.** PB3744 and PB3716 landed; neither claims an inventory row (tooling).

**Self-review.** Architecture: one staging-and-swap pair beside the one GitHub reader, no second copy of the rule.
Code: the outer `except OSError` still reports "left as it was", true now because the swap restores. Performance: one
extra rename per successful fetch, none on the hot path of the re-gate (the marker short-circuits first).
Duplication: the marker prefix is written in one f-string and read by `_holds` and the already-fetched check; a drift
between them fails the "already in" and "run 800" arms.
