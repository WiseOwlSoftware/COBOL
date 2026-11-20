## 2026-11-20: a second occurrence, NC207A, in a gate run whose diff touched only the listing

Gate run `000001-20261120T041808Z` of branch `listing-step-pb5941` (head 824196c9c891; the diff touches the listing CLI's
standard-output path, its tests and docs, and no runtime, code generator or NIST harness file) reported Conformance RED on
one row of 28,195: `NistDifferentialTests_P1.NistProgram_MatchesGolden(testName: "NC207A")`, a HARNESS NON-OBSERVATION.
Attempt 1: TimedOut, 120000 ms, 0.4 s of processor time used, host 100 % busy; attempt 2: SurvivedKill, process 56516
still alive 5000 ms after its kill, 0.4 s used, host 100 % busy.

It is the same shape as the pb739 occurrence above: a process that uses almost no processor for 120 s and outlives its kill,
during a host-wide stall. The run's observation log shows eight programs timing out together between 04:36:05 and 04:37:33
(NC207A, IX109A, NC205A, g85_bf36_01, IX201A, ST119A, doc_a1_17_call_other_effects_2002, NC122A), each with 0.2 to 1.0 s of
processor time, and every one but NC207A recovered on its retry (04:38:42 to 04:39:33). NC122A also survived its kill and
exited while the retry waited. The write-through append probes in the same attempts took up to 2994 ms (median 1 to 6 ms),
so the volume was stalling as well as the processor. Another lane's gate ran on the same host at the time.

So the pattern is not specific to file-sharing cases: NC207A is a nucleus program with no shared file. The common factor is
a host where the processor and the run volume are both saturated, and a child that, once starved, does not exit within
5 s of its kill. That moves the first candidate site from `FileConnector`'s shared-file wait toward the kill itself
(`tests/_shared/ProcessObservation.cs`, `ProcessObserver`: what the child is blocked in when the kill lands, for example
process start-up or image load on a stalled volume) and toward how many gates the host admits at once. The to-do above
stands: capture the stacks of the surviving process the next time, and fix the root, never a longer guard.

The finisher's re-check at the merged head (ab67be9106, Unit, RepoDrift and Conformance started together by hand, so the
host was saturated by its own three test hosts) reproduced the class without NC207A: Conformance passed 28,191 of 28,195 and
non-observed four NIST rows (SQ217A, IC114A, SQ116A, SQ204A), each TimedOut at 120000 ms with 0.9 to 1.0 s of processor
time on a host 95 to 97 % busy. NC207A passed in that run. The five rows run alone at the same tree, host 29 % busy,
passed 5 of 5. So the red follows host load, not the program: three suites at once on one host is enough to produce it,
and the gate's single slot does not protect a run from the other lanes' gates on the same machine.
