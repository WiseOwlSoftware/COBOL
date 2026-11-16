## Entry 3761 — 2026-11-16 06:59 PST — PB5772: a level 66 entry may no longer rename another level 66 entry at --std 85

Wave 20262077 group B (PB5772, PB5801).

**PB5772.** At `--std 85` `66 R2 RENAMES R1` (R1 a level 66 entry) compiled and printed `R2=[ABCD]`, against `§1985:VI.5.11.3 SR4`. Cause: the PB5165 whole-scope candidate arm reaches the record's level 66 alias, and `ResolveRedefinesPass.ScreenRenamesOperand` tested levels 1 and 77 only. It now tests level 66 too (level 88 stays unreachable on both arms). Re-probe after the fix: exit 65 COBOL2740 at 85, COBOL1655 at 2023, and `66 X RENAMES A THRU R1` (the refuter's lead) draws COBOL2740 in place of the PB96 tiling message. Golden: `1985-SR-VI_5_11_3-4` plus its refusal file. No row closed (1985 ids, kb/Work PB5094).

**PB5801.** Discharged: already landed on main (PR 1769) before this wave started; nothing to do.

