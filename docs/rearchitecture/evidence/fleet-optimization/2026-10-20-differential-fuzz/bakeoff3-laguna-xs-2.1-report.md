# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 12 (60% of judged; 6 of them after a repair round) · the model's failures: 8 · GnuCOBOL alone refused (THEY_REJECT leads): 1
Model seconds per program (median): 18

| Class | Count |
|---|---|
| WE_REJECT | 8 |
| BOTH_REJECT | 7 |
| OUTPUT_DIFFERS | 2 |
| THEY_REJECT | 1 |
| NO_PROGRAM | 1 |
| AGREE | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 11 programs in 8 signatures

### `WE_REJECT:COBOLNET1517,COBOLNET1627` × 2 — first: fz00000 (clause 15.3.2 Time formats)
- programs: fz00000, fz00011
- source: `programs/fz00000.cob`
- WiseOwl: `fz00000.cob(23,1): error COBOLNET1627: FUNCTION SECONDS-FROM-FORMATTED-TIME argument-1 is of class numeric; ISO §15.3 requires class alphanumeric, alphanumeric (numeric-edited), national (numeric-edited) or national (§15.79.3 r1/r3). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET1625` × 2 — first: fz00006 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz00006, fz00018
- source: `programs/fz00006.cob`
- WiseOwl: `fz00006.cob(7,1): error COBOLNET1625: data item 'WS-C': the numeric literal 123.456 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1627` × 2 — first: fz00005 (clause 15.99 WHEN-COMPILED function)
- programs: fz00005, fz00009
- source: `programs/fz00005.cob`
- WiseOwl: `fz00005.cob(51,1): error COBOLNET1627: FUNCTION NUMVAL argument-1 is of class numeric; ISO §15.3 requires class alphanumeric, alphanumeric (numeric-edited), national (numeric-edited) or national (§15.67.3 r1). --permissive accepts it as a coercion extension`

### `OUTPUT_DIFFERS:15.3.1.5` × 1 — first: fz00010 (clause 15.3.1.5 Format arguments to international date and time functions > Permissible values for data associated with ordinal date formats)
- programs: fz00010
- source: `programs/fz00010.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LEAP YEAR DAY 1: 0` | `LEAP YEAR DAY 1: 002000000`
  - line 2: `LEAP YEAR DAY 366: 0` | `LEAP YEAR DAY 366: 002000000`
  - line 3: `COMMON YEAR DAY 365: 0` | `COMMON YEAR DAY 365: 002000000`
  - line 4: `INVALID DAY 0: 0` | `INVALID DAY 0: 002000000`
  - line 5: `INVALID DAY 367: 0` | `INVALID DAY 367: 002000000`

### `OUTPUT_DIFFERS:15.3.2` × 1 — first: fz00001 (clause 15.3.2 Time formats)
- programs: fz00001
- source: `programs/fz00001.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `CURRENT SECONDS: 000078151` | `CURRENT SECONDS: 000078149`
  - line 2: `CURRENT SECONDS AGAIN: 000078151` | `CURRENT SECONDS AGAIN: 000078149`
  - line 3: `CURRENT SECONDS THIRD: 000078151` | `CURRENT SECONDS THIRD: 000078149`
  - line 5: `CURRENT SECONDS TRUNC: 000078151` | `CURRENT SECONDS TRUNC: 000078149`
  - line 7: `LOW TIME: 000078151` | `LOW TIME: 000078149`

### `THEY_REJECT:15.99` × 1 — first: fz00003 (clause 15.99 WHEN-COMPILED function)
- programs: fz00003
- source: `programs/fz00003.cob`
- GnuCOBOL: `fz00003.cob:32: error: MOVE of figurative constant SPACE to numeric item used`

### `WE_REJECT:COBOLNET1740` × 1 — first: fz00004 (clause 15.99 WHEN-COMPILED function)
- programs: fz00004
- source: `programs/fz00004.cob`
- WiseOwl: `fz00004.cob(16,1): error COBOLNET1740: data item 'WS-TEST-DATE': the VALUE alphanumeric literal (25 characters) exceeds the item's 21 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET2993` × 1 — first: fz00019 (clause 15.6 Summary of functions)
- programs: fz00019
- source: `programs/fz00019.cob`
- WiseOwl: `fz00019.cob(32,29): error COBOLNET2993: the unary arithmetic operator '-' is not immediately followed by a separator space or a left parenthesis — ISO §8.7.1 requires it to be immediately preceded and followed by a separator space or a left parenthesis; write a space after it`
