The branch review of 2026-11-16 (round 1, finding R1-1) asked for an accepting witness of each exempting arm of `VersionConformancePass.DeclarativeExecutableFromRange`.
Five fixed-form cases at 85 now pin them, under `conformance-suite/cases/XI.4.2@1985-RELEASE-STATEMENT/`:
- `pb5768_release_in_declarative_reached_by_call_85`: the range reaches the GLOBAL USE only through `CALL "SUBP"`.
- `pb5768_release_in_declarative_open_mode_use_85`: `USE AFTER STANDARD ERROR PROCEDURE ON INPUT`.
- `pb5768_release_in_declarative_nested_io_85`: the OPEN stands inside an IF.
- `pb5768_release_in_declarative_performed_io_85`: the OPEN is in a paragraph the input procedure PERFORMs.
- `pb5768_release_in_debugging_declarative_85`: a USE FOR DEBUGGING section on a paragraph of the input procedure.
Each matches its .out on the branch build.
With its arm disabled one at a time (a temporary local edit, restored from git), each compiles with COBOL7081 on the declarative RELEASE.
The script is `E:\COBOL-coord\scratch\finish-wf_df842f2c-278-1\gen\revert_arms.py`, and its runs are under `runs-revert\` beside it.
