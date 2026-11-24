# PB6138 addendum (2026-11-24): the note landed with harm flags its own text contradicts

**Source:** `E:/COBOL-coord/scratch/reports/w20262165g-PB6138-report.md`, Review findings R1-1 (LEAD, low, not blocking at round 1).

`kb/Work/PB6138.md` is `status: landed`. Its own Landed paragraph and the DEVLOG establish that the wrong answer is unreachable: a
dynamic-capacity table may not be in the file section (§2023:8.5.1.9.1 SR3, COBOL1526), so `_spaceUnit` never acts on one. The
frontmatter still says `wrong_answer: true` and `silent: true`. The harm flags of a landed note feed the severity views; they should read
`false` (a consistency fix, no program changes behaviour). The change belongs to the next editor of that note's frontmatter
(`work.py` has no setter for harm flags); this addendum records it so the register's ledger is not read as holding a landed silent
wrong answer.
