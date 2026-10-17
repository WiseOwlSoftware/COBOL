## Entry 2156 — 2026-10-17 14:03 PDT — PB3342-PB3344 filed: the PB1784 branch's leads (COBOLNET4213 refuses legal source; two guard holes); PB3131 gains an instance

The enqueue unit's step-5 clerk filed the leads from branch `worktree-wf_9e52adb3-fa2-7` (PB1784 + PB745, PR #188), report `w1108a-PB1784-report.md`.

PB3342: COBOLNET4213 `OverrideNamedLikeItsClass` refuses a COBOL class that overrides an inherited method named like the class, a restriction inherited from C# (CS0542). Site: `OoClassTable.cs` `MarkRoster` and the descriptor in `DiagnosticCatalog.cs`, both on PR #188's branch. The lead is not re-verified and the spec citation is marked "to verify".

PB3343: `DeclinedFacilityAnnexDriftTests` arm 3 iterates only descriptors that already carry an annex, so a code the CONFORMANCE registers name without an annex or descriptor is never checked (the PB745 shape).

PB3344: `NegativeGoldenHeaderCodeDriftTests` does not see goldens that pin only the message text, so a code reassignment can leave one asserting a stale meaning.

PB3131 gained a recurrence paragraph: a test rename with no inventory edit sent `EveryTestRef_ResolvesToARealTest` to leg 2 in two gate runs of the PB1784 branch.

