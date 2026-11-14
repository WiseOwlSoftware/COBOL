**2026-11-13, lead filed by the notes batch 20261113d.** UNVERIFIED LEAD: re-probe before fixing.

Lead R1-4 of the review of PB5478's fix branch (w20262023a-review-r1, `E:/COBOL-coord/scratch/op-20261113/review-w20262023-a.jsonl`, `tests/WiseOwl.Cobol.Tests.Unit/GrammarDiagramGeneratorDriftTests.cs:352`).

Summary, verbatim: "_met is keyed by path, so a file re-created at a path already met is never held again; the event-driven holder took every Created report, and the class summary still says it holds every regular file created under the directory"

Failure scenario, verbatim: "A generator regression that writes a fixed-name handoff file in TEMP once per rule (delete, re-create, re-read): the first instance is held, but if its read-back wins the race with the scan, every later instance at the same path is skipped by _met.Add and the hostile half never touches it, so Generator_RunsClean stays green where the PB5463 holder would have taken each re-creation. Missing a short-lived jar_cache*.tmp created and deleted between two listings is harmless to what the test asserts (exit code, holder errors, output directory); the reused path is the real loss of sensitivity."

Fix, verbatim: "Key met files by file identity (path plus creation time or file id) rather than path alone, or state the limit in the class remarks."
