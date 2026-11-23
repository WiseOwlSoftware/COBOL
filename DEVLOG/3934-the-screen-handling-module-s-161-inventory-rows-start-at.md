## Entry 3934 — 2026-11-22 15:35 PST — The screen-handling module's 161 inventory rows start at 2002, not 85

PB3319. The screen-section clause rows (AUTO, BACKGROUND-COLOR, BELL, BLANK, BLINK, ERASE, FOREGROUND-COLOR, FULL, HIGHLIGHT, LOWLIGHT, REQUIRED, REVERSE-VIDEO, SECURE) carried `editions` 85,2002,2014,2023, while PB3173's evidence puts screen handling at 2002 (SCREEN first reserved at 2002; §2002:F.2 item 1 extends ACCEPT to screen items).

Re-probe on this build: 161 DOCUMENTED-NON-SUPPORT rows naming Annex A.4.2 started at 85: the whole module, not only the listed clauses (UNDERLINE, FROM, TO, USING, the screen-specific rules of COLUMN, LINE, OCCURS, USAGE, VALUE and level-number, the screen section, the screen description entry, CURSOR and CRT STATUS, 9.2.3, and the ACCEPT, DISPLAY, SET and INITIALIZE screen rules). Sibling sweep: the module is selected by its own notes (DOCUMENTED-NON-SUPPORT, band starting 85, notes naming A.4.2), which separates it exactly from the 13 other DOCUMENTED-NON-SUPPORT rows at 85 (alignment, non-COBOL calls, STANDARD-1 tape, reel swap, CURRENT-DATE) and from the 13 CONFORMS rows that name A.4.2 but state rules shared with data and report items. Every other A.4.N module's rows already start at 2002.

Fix: `adjudication/pb3319-editions/decide.py` checks its evidence (reserved-words.json, `end-accept-2002`, two `cite.py --check`, five already-2002 siblings) and writes the `record_verdicts` batch; applied, 161 store files, GAP 4 to 4. Structural check `SpecTraceabilityInventoryDriftTests.EveryScreenModuleRule_StartsNoEarlierThanTheScreenFacility` reads the facility's edition from `ReservedWords`, with planted cases for a refused 85 start, a 2002 start, a CONFORMS row and A.4.21.

No verdict or state changed, so no row closes (`closes_rows: []`).

