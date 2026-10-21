# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 12 (60% of judged; 6 of them after a repair round) · the model's failures: 8 · GnuCOBOL alone refused (THEY_REJECT leads): 0
Model seconds per program (median): 18

| Class | Count |
|---|---|
| BOTH_REJECT | 7 |
| OUTPUT_DIFFERS | 6 |
| WE_REJECT | 5 |
| AGREE | 1 |
| NO_PROGRAM | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 11 programs in 8 signatures

### `OUTPUT_DIFFERS:15.23` × 2 — first: fz00001 (clause 15.23 DATE-TO-YYYYMMDD function)
- programs: fz00001, fz00002
- source: `programs/fz00001.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 10: `DATE-TO-YYYYMMDD(20240101, 50, 2024) = 00000000` | `DATE-TO-YYYYMMDD(20240101, 50, 2024) = 39240101`
  - line 11: `TEST-DATE-YYYYMMDD(20240101) = 0` | `TEST-DATE-YYYYMMDD(20240101) = 000000000`
  - line 12: `TEST-DATE-YYYYMMDD(991232) = 1` | `TEST-DATE-YYYYMMDD(991232) = 000000001`

### `OUTPUT_DIFFERS:15.63` × 2 — first: fz00012 (clause 15.63 MIN function)
- programs: fz00012, fz00014
- source: `programs/fz00012.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `MIN OF -2,0,3 IS 00K` | `MIN OF -2,0,3 IS -002`
  - line 3: `MIN OF -2,-2 IS 00K` | `MIN OF -2,-2 IS -002`

### `OUTPUT_DIFFERS:15.77` × 2 — first: fz00003 (clause 15.77 REM function)
- programs: fz00003, fz00005
- source: `programs/fz00003.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `REM(+12345.67,+100) = 0000000456G` | `REM(+12345.67,+100) = +000000045.67`
  - line 2: `REM(-12345.67,-100) = 0000000456P` | `REM(-12345.67,-100) = -000000045.67`
  - line 3: `REM(+12345.67,+7) = 0000000046G` | `REM(+12345.67,+7) = +000000004.67`
  - line 4: `REM(-12345.67,+7) = 0000000046P` | `REM(-12345.67,+7) = -000000004.67`
  - line 5: `REM(+12345.67,0) = 0000000000{` | `REM(+12345.67,0) = +000000000.00`

### `WE_REJECT:COBOL0001` × 1 — first: fz00007 (clause 8.8.4.9 Conditional expressions > Complex conditions)
- programs: fz00007
- source: `programs/fz00007.cob`
- WiseOwl: `fz00007.cob(50,8): error COBOL0001: unexpected 'NOT'`

### `WE_REJECT:COBOLNET0900` × 1 — first: fz00019 (clause 14.9.6 CLOSE statement)
- programs: fz00019
- source: `programs/fz00019.cob`
- WiseOwl: `fz00019.cob(27,5): error COBOLNET0900: DELETE FILE requires COBOL-2023 (targeting COBOL-2014) — the DELETE FILE statement (ISO 2023 §14.9.10 Format 2; Annex E.3.3 item 15)`

### `WE_REJECT:COBOLNET1625` × 1 — first: fz00004 (clause 15.77 REM function)
- programs: fz00004
- source: `programs/fz00004.cob`
- WiseOwl: `fz00004.cob(15,1): error COBOLNET1625: data item 'WS-K': the numeric literal 123456789012345 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1740` × 1 — first: fz00013 (clause 15.63 MIN function)
- programs: fz00013
- source: `programs/fz00013.cob`
- WiseOwl: `fz00013.cob(13,1): error COBOLNET1740: data item 'WS-STR3': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET2326` × 1 — first: fz00010 (clause 14.9.20 INITIALIZE statement)
- programs: fz00010
- source: `programs/fz00010.cob`
- WiseOwl: `fz00010.cob(39,1): error COBOLNET2326: SET 'WS-IDX-DATA' TO '1' violates ISO §14.9.39.3 SR4: "If identifier-1 references a numeric data item, index-name-2 shall be specified"`
