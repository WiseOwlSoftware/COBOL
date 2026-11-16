## Entry 3789 — 2026-11-16 12:55 PST — Three notes and three finding files from the Mythos lane's rulings on read pass 16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record).

- PB5876 (defect, MAJOR, wrong_answer, under_rejects, silent): at `--std 85` a statement that references a USING operand whose table is DEPENDING ON a Linkage item of a record that is no operand compiles and reads no storage.
- PB5877 (defect, MINOR, under_rejects): READ ... KEY IS a REDEFINES of the record key in the key's own record compiles.
- PB5878 (defect, MINOR, wrong_answer): when an OPEN fails with 41 on a file open in another mode and both modes have a USE procedure, the attempted mode's procedure runs, where the precedence gives the procedure of the mode the file is open in.

Finding files: the rulings of code-read batch 16 beside PB5683, a fifth owed register sentence beside PB5870 (`D-85-SPACE-CLASS`), and mechanism 4 (rejected; SPACE in a THROUGH range) beside PB5796.

