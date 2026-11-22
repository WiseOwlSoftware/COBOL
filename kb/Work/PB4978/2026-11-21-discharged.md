**2026-11-21, discharged with evidence (wave 20262131 group u).** The note's premise is false: at `--std 85` SEARCH identifier-1 is
not the only whole-table reference. A function argument subscripted ALL references an elementary item under a table as a whole
(§1985:IV.4.3.8.2.3 SR7: "The subscript ALL may be used only when the subscripted identifier is used as a function argument";
§1989:p.A-28: "When ALL is specified as a subscript, the effect is as if each table element associated with that subscript position
were specified"; both `cite.py --check` OK). So any item under a table can be named whole at 85, and from 2002 a table SORT key can
too; the register's width is set before any statement binds (`ProcedureTableBuilder.FinalizeDebug`), so it cannot know which items
a program will name that way. `RecordLayout.AllOccurrencesImageWidth` for every target is the width §1985:XV.3.2.4 GR15 requires
("large enough to contain the data required by the following general rules"), not an over-size.

Probe (`E:/COBOL-coord/scratch/w20262131u/src/dbgall.cob`, this build, `--std 85`): `USE FOR DEBUGGING ON ALL REFERENCES OF F` with
`05 E OCCURS 10. 10 F PIC X(5).` and `MOVE FUNCTION MAX (F (ALL)) TO X`: `DS [AAAAABBBBBCC] 00050` (LENGTH of DEBUG-CONTENTS 50, its
content every occurrence). The narrowing the note proposed would have cut that to 30 and truncated GR24c's "content of the data item
referenced by identifier-1". The golden `conformance-suite/cases/XV.3.2@1985-USE-FOR-DEBUGGING-STATEMENT/pb4978_debug_all_subscript_85`
pins it (positions 1, 6 and 46 of the whole-table image; `run_probe.py --expect`: MATCH), and `docs/conformance/3/D-DEBUG-WHOLE-TABLE.md`
(4) now names the ALL subscript as a whole-table reference. A narrower register would need a pre-scan of the procedure division for
whole-table references before the register is declared, for a memory saving only; not worth a redesign, and not filed.
