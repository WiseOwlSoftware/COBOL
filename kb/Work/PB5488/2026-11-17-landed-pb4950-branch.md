**2026-11-17: landed (branch w20262102c, kb/Work PB4950).** COBOL6764 now quotes the rule word for word:
`RD 'R1': SUM addend 'A' names the entry 'A', a SOURCE or VALUE entry of the report section: "If identifier-1 is defined in the
Report Section, identifier-1 must reference a sum counter" (ISO §1985:XIII.3.19.3 SR1)` (rerun of
`E:/COBOL-coord/scratch/my-20261115/triage85/g1/p5488.cob` at `--std 85`; `cite.py --check` on the quotation OK).
