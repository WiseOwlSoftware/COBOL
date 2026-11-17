## 2026-11-17 - PB2164 (PB2119 delete wave A, pull request 1855) deleted `BoundBoolError.Report`

Filed by notes batch 23 of the Mythos lane (a clerk; nothing re-run). PB2622's body is unchanged.

PB2622 names `BoundBoolError.Report` three times: line 24 (`r2_members`), line 35 (the Sites line, `src/WiseOwl.Cobol.Compiler/Binding/Bound/BoundTree.cs:294,630,723,919`, "BoundBoolError.Report") and line 49 ("Moves or changes"). kb/Work PB2164 (landed 2026-11-17; its landing file `kb/Work/PB2164/2026-11-17-landed.md`) deleted `BoundBoolError.Report` and `BoundBoolError.Unbuilt` from `BoundTree.cs`: "The re-measure ... and a fresh grep across src, tests, tools, scripts and docs found no reference. Behaviour neutral: no emitted text changes."

So the PB2622 wave moves or changes the other three `Report` factories only (`BoundExprError.Report`, `BoundOperandError.Report`, `BoundRejected.Report`), not a factory that no longer exists; the member index (`scripts/arch/member_index.py --note PB2622`) will find `BoundBoolError.Report` unknown until the note's lines are re-recorded.

Source: the reviewer's lead R3-1 (round 3, severity low, reviewer review-DA, branch worktree-agent-ade62048985abcfaa at 2fb9ec860b) in `E:/COBOL-coord/scratch/my-20261115/review-w20262101-a.jsonl`, whose fix line reads "Add kb/Work/PB2622/2026-11-17-boolerror-report-deleted.md saying that PB2164 (PB2119 wave A) deleted BoundBoolError.Report, so the PB2622 wave moves or changes the other three Report factories only." (R2-1 of round 2 asked the same.)
