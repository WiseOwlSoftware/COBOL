# Differential fuzz — triage report

Programs: 40 · judged by GnuCOBOL: 40 · GnuCOBOL accepted: 16 (40% of judged; 13 of them after a repair round)
Model seconds per program (median): 72

| Class | Count |
|---|---|
| GNU_REJECTS | 24 |
| WE_REJECT | 9 |
| AGREE | 4 |
| OUTPUT_DIFFERS | 3 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 12 programs in 5 signatures

### `WE_REJECT:COBOLNET2993` × 6 — first: fz00003 (clause 8.8.1.2 Native, standard-binary, and standard-decimal arithmetic)
- programs: fz00003, fz00005, fz00016, fz00027, fz00028, fz00029
- source: `programs/fz00003.cob`
- WiseOwl: `fz00003.cob(71,32): error COBOLNET2993: the unary arithmetic operator '-' is not immediately followed by a separator space or a left parenthesis — ISO §8.7.1 requires it to be immediately preceded and followed by a separator space or a left parenthesis; write a space after it`

### `OUTPUT_DIFFERS:8.8.1.5.4` × 2 — first: fz00030 (clause 8.8.1.5.4 Exponentiation in standard-decimal arithmetic)
- programs: fz00030, fz00032
- source: `programs/fz00030.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `0 ** 1 = 000000000000{` | `0 ** 1 = +0000000.000000`
  - line 5: `0 ** 2 = 000000000000{` | `0 ** 2 = +0000000.000000`
  - line 6: `0 ** 3 = 000000000000{` | `0 ** 3 = +0000000.000000`
  - line 7: `<end>` | `0 ** -1 = +0000000.000000`
  - line 8: `<end>` | `0 ** -2 = +0000000.000000`

### `WE_REJECT:COBOLNET1625` × 2 — first: fz00020 (clause 8.8.1.4.4 Exponentiation in standard-binary arithmetic)
- programs: fz00020, fz00031
- source: `programs/fz00020.cob`
- WiseOwl: `fz00020.cob(24,8): error COBOLNET1625: data item 'WS-TEST15': the numeric literal -0.000001 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `OUTPUT_DIFFERS:8.8.1.4.4` × 1 — first: fz00019 (clause 8.8.1.4.4 Exponentiation in standard-binary arithmetic)
- programs: fz00019
- source: `programs/fz00019.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `2.0 ** 0.0 = 00000000010000000000` | `2.0 ** 0.0 = 0000000001.0000000000`
  - line 2: `2.0 ** 1.0 = 00000000020000000000` | `2.0 ** 1.0 = 0000000002.0000000000`
  - line 3: `2.0 ** 2.0 = 00000000040000000000` | `2.0 ** 2.0 = 0000000004.0000000000`
  - line 4: `2.0 ** 3.0 = 00000000080000000000` | `2.0 ** 3.0 = 0000000008.0000000000`
  - line 5: `2.0 ** 4.0 = 00000000160000000000` | `2.0 ** 4.0 = 0000000016.0000000000`

### `WE_REJECT:COBOLNET0801` × 1 — first: fz00026 (clause 8.8.1.5.2 Standard-decimal intermediate data item)
- programs: fz00026
- source: `programs/fz00026.cob`
- WiseOwl: `fz00026.cob(9,8): error COBOLNET0801: data item 'WS-TEST-VAR' (PICTURE 9(34)) has 34 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`
