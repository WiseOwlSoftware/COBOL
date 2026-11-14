**2026-11-13, lead filed by the notes batch 20261113d.** UNVERIFIED LEAD: re-probe before fixing.

Lead R1-3 of the review of PB5478's fix branch (w20262023a-review-r1, `E:/COBOL-coord/scratch/op-20261113/review-w20262023-a.jsonl`, `tests/WiseOwl.Cobol.Tests.Unit/GrammarDiagramGeneratorDriftTests.cs:355`, assertion at line 139). This test has reddened landings three times (PB5447, PB5463, PB5478); this file gives a fourth occurrence a note to name.

Summary, verbatim: "A probe is proved once it is held and listed, but a file of the same scan whose open met a sharing violation is still pending on a TakeWhenShared work item, so Holder_HoldsEveryFile_WhenItsWatcherOverflows can Stop with Held = 1001 and fail Assert.Equal(burst + 2, held.Held) (line 139) with no regression"

Failure scenario, verbatim: "On a Windows host where a third-party filter or indexer opens one fresh burst-* file without FILE_SHARE_WRITE at the moment the scan takes it: TryTake returns false, the path goes to the pool, the scan marks holder-probe-2 Listed and Held, the test calls Stop before the work item retakes it, and the assertion reports 1001 held. Not reproduced here (the trigger is outside the process); the gap is in the proof, which does not wait for deferred takes."

Fix, verbatim: "Count deferred takes and complete a probe only when no take deferred by the scan that listed it is still pending, or have Stop report pending deferrals separately so the assertion counts held + pending." It needs an outside program holding one fresh file; NOT reproduced.
