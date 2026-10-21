# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 14 (70% of judged; 5 of them after a repair round) · the model's failures: 6 · GnuCOBOL alone refused (THEY_REJECT leads): 0
Model seconds per program (median): 10

| Class | Count |
|---|---|
| AGREE | 7 |
| BOTH_REJECT | 6 |
| WE_REJECT | 4 |
| OUTPUT_DIFFERS | 3 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 7 programs in 6 signatures

### `OUTPUT_DIFFERS:15.77` × 2 — first: fz00003 (clause 15.77 REM function)
- programs: fz00003, fz00004
- source: `programs/fz00003.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `REM(10.50, 3.00) = 00000015{` | `REM(10.50, 3.00) = +0000001.50`
  - line 2: `REM(-10.50, 3.00) = 00000015}` | `REM(-10.50, 3.00) = -0000001.50`
  - line 3: `REM(10.50, -3.00) = 00000015{` | `REM(10.50, -3.00) = +0000001.50`
  - line 4: `REM(-10.50, -3.00) = 00000015}` | `REM(-10.50, -3.00) = -0000001.50`
  - line 5: `REM(1234567, 1000) = 00005670{` | `REM(1234567, 1000) = +0000567.00`

### `OUTPUT_DIFFERS:14.9.20` × 1 — first: fz00010 (clause 14.9.20 INITIALIZE statement)
- programs: fz00010
- source: `programs/fz00010.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `NUM2: [000{]` | `NUM2: [+0000]`
  - line 10: `NUM2: [000{]` | `NUM2: [+0000]`
  - line 18: `NUM2: [000{]` | `NUM2: [+0000]`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET2168` × 1 — first: fz00013 (clause 15.63 MIN function)
- programs: fz00013
- source: `programs/fz00013.cob`
- WiseOwl: `fz00013.cob(11,1): error COBOLNET2168: data item 'WS-INDEX-A': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0809,COBOLNET1694` × 1 — first: fz00014 (clause 15.63 MIN function)
- programs: fz00014
- source: `programs/fz00014.cob`
- WiseOwl: `fz00014.cob(34,1): error COBOLNET0809: a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60.3 SR10 — only a SEARCH or SET statement, a relation condition, an intrinsic-function or inline-method argument, or a procedure-division / CALL / INVOKE USING phrase may reference an index`

### `WE_REJECT:COBOLNET0898,COBOLNET1627` × 1 — first: fz00012 (clause 15.63 MIN function)
- programs: fz00012
- source: `programs/fz00012.cob`
- WiseOwl: `fz00012.cob(10,1): error COBOLNET0898: data item 'WS-NAT': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0900` × 1 — first: fz00019 (clause 14.9.6 CLOSE statement)
- programs: fz00019
- source: `programs/fz00019.cob`
- WiseOwl: `fz00019.cob(7,9): error COBOLNET0900: ORGANIZATION LINE SEQUENTIAL requires COBOL-2023 (targeting COBOL-2014) — the LINE SEQUENTIAL phrase of the ORGANIZATION clause (ISO §12.4.5.10.3 GR2; §9.1.7.2 splits sequential files into record- and line-delimited types)`
