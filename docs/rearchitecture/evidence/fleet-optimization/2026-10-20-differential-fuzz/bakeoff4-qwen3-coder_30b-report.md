# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 12 (60% of judged; 8 of them after a repair round) · the model's failures: 8 · GnuCOBOL alone refused (THEY_REJECT leads): 0
Model seconds per program (median): 21

| Class | Count |
|---|---|
| BOTH_REJECT | 8 |
| AGREE | 6 |
| WE_REJECT | 5 |
| OUTPUT_DIFFERS | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 6 programs in 4 signatures

### `WE_REJECT:COBOLNET0898` × 3 — first: fz00012 (clause 15.63 MIN function)
- programs: fz00012, fz00013, fz00014
- source: `programs/fz00012.cob`
- WiseOwl: `fz00012.cob(18,1): error COBOLNET0898: data item 'WS-NAT1': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `OUTPUT_DIFFERS:15.77` × 1 — first: fz00003 (clause 15.77 REM function)
- programs: fz00003
- source: `programs/fz00003.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `1. POSITIVE NUMBERS: REM(17.5, 3.0) = 2.50` | `1. POSITIVE NUMBERS: REM(17.5, 3.0) = 00000002.5`
  - line 4: `2. NEGATIVE DIVIDEND: REM(-10.0, 3.0) = -1.00` | `2. NEGATIVE DIVIDEND: REM(-10.0, 3.0) = -000000001`
  - line 5: `3. NEGATIVE DIVISOR: REM(10.0, -3.0) = 2.50` | `3. NEGATIVE DIVISOR: REM(10.0, -3.0) = 00000002.5`
  - line 6: `4. BOTH NEGATIVE: REM(-10.0, -3.0) = -1.00` | `4. BOTH NEGATIVE: REM(-10.0, -3.0) = -000000001`
  - line 7: `5. DECIMALS: REM(7.125, 2.500) = 2.125` | `5. DECIMALS: REM(7.125, 2.500) = 000002.125`

### `WE_REJECT:COBOLNET1625` × 1 — first: fz00004 (clause 15.77 REM function)
- programs: fz00004
- source: `programs/fz00004.cob`
- WiseOwl: `fz00004.cob(14,1): error COBOLNET1625: data item 'WS-SMALL': the numeric literal 0.00001 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1694` × 1 — first: fz00015 (clause 14.9.39 SET statement)
- programs: fz00015
- source: `programs/fz00015.cob`
- WiseOwl: `fz00015.cob(28,5): error COBOLNET1694: DISPLAY operand 'WS-POINTER1' is of class pointer, which the statement's rules exclude — ISO §14.9.11.3 SR1 (class object/pointer); §13.18.60.3 SR10 (an index data item may be referenced only in SEARCH/SET, a relation condition, an intrinsic/method argument, or`
