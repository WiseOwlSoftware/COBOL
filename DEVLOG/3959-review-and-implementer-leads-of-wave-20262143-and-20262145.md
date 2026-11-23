## Entry 3959 — 2026-11-22 22:59 PST — Review and implementer leads of wave 20262143 and 20262145 filed as kb/Work notes

Filed from three reports in E:/COBOL-coord/scratch/reports (w20262143e-PB6096, w20262143g-PB6072, w20262145c-PB3221); no code changed.

- PB6113 (open): `SEARCH E AT END MOVE 1 TO.` draws the bare COBOL0001; `GuessCobolIntent` names no missing receiver inside an AT END phrase.
- PB6114 (open): PB6096's hint 0c' reads a section header after a completed MOVE/ADD/SUBTRACT CORRESPONDING as a second receiver at every edition without areas, suppressing COBOL0307.
- PB6115 (open, process-only): the `Rules2014` and `OccursBanCitationAt` comments misquote §2014:12.4.4.11.2 SR1.
- PB6116 (open): two floating strings of different extended character-1 bind and render `[ -0005]`; wrong answer, under-rejects, §2023:13.18.40.3 SR27.
- Finding beside PB3221 (`kb/Work/PB3221/2026-11-23-harm-flags.md`): its harm flags contradict its Landed section; the correct flags are stated there.

Ids PB6113 through PB6116 used; none returned.

