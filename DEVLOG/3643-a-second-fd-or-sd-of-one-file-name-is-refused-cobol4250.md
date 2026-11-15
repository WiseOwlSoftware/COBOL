## Entry 3643 — 2026-11-14 19:51 PST — A second FD or SD of one file-name is refused (COBOL4250)

kb/Work PB5541. Re-probe on the Debug build: two `FD FA.` entries under one SELECT compiled clean (exit 0, no diagnostic) at --std 2023. The file-name is declared once, by the SELECT (`DeclareUserWord(FileName)`; a second SELECT is COBOL2634), so the FD was never a second declaration and nothing counted a repeat.

Fix: `FileControlBinder.DescriptionRepeated` asks `FileModel.HasFd` before either arm of `BindFileSection` attaches (the FD arm and the SD arm, so FD+FD, SD+SD and FD+SD all draw it) and reports COBOL4250 under ISO §2023:8.4.2.1 (cite.py --check OK): a file-name has no qualifier, so no reference could identify one of two descriptions. The first description stands and the repeat is neither attached nor has its clauses read. The COBOL4250 descriptor text and doc now name the file-name case. No new code was needed.

Golden: `conformance-suite/wiseowl/diagnostics/pb5541-fd-declared-twice.json` with its program under `cases/_unassigned`, refused at 85, 2002, 2014 and 2023 (verified by running the CLI at each). Rows closed: none (the note claimed none).

