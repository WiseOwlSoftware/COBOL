## 2026-11-17 - which files, and the size: semgrep cannot parse two very large source files (the wave 20262097 group C reviewer's lead R1-7)

Filed by notes batch 22 of the Mythos lane (a clerk). The brief listed this lead as its own defect note; kb/Work PB5502 (open) already holds the failure (`verify.py` answers `FAIL (2 unreadable files unnamed)` on one run and PASS on another), so this is a dated file beside it. The reviewer's words, unchanged (`E:/COBOL-coord/scratch/my-20261115/review-w20262097-c.jsonl`, finding R1-7, severity info, round 1 of the review of the PB5884 branch):

> LEAD, not caused by this branch: verify.py check 2 fails because semgrep cannot parse whole files that WHOLE_FILE_UNREADABLE does not name. On the branch, unreadable_files() returned 6 whole-file failures, 2 unnamed: src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs and src/WiseOwl.Cobol.Compiler/Binding/IntrinsicArgumentRules.cs. origin/main VersionConformancePass.cs alone also fails (Syntax error at line 1), so it is pre-existing. Not encoding (UTF-8, the error is the whole-file give-up at line 1) but semgrep C# parser abandoning large files (311 KB and 218 KB); the 2 vs 3 count across the implementer runs points to a parse budget that varies with load.

> verify.py fails on any host for main, so its check that no rule count increases is never reached and a regression in a rule count goes unmeasured. Log: E:/COBOL-coord/scratch/my-20261115/w20262097/review-w20262097-c/semgrep-unreadable.txt.

The reviewer's fix: "A kb/Work note: name the two files in WHOLE_FILE_UNREADABLE with their first failing member (prefix bisect), or remove the load-dependence by raising semgrep --timeout for the parse."

**Sizes on main at a05cc533dc** (`wc -c`): `src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs` 307404 bytes; `src/WiseOwl.Cobol.Compiler/Binding/IntrinsicArgumentRules.cs` 178008 bytes (the reviewer measured about 218 KB; the file was reworked and shrank, kb/Work PB5891). Whether semgrep still gives up on the second was not re-run.

**Where else it was seen:** `E:/COBOL-coord/scratch/my-20261115/FINDING-semgrep-verify-unreadable.md` (wave 20262062a, kb/Work PB5822: the same two files plus `DiagnosticCatalog.cs`, which is kb/Work PB4617 and PB4970).
