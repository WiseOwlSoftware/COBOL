# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 11 (55% of judged; 7 of them after a repair round) · the model's failures: 9 · GnuCOBOL alone refused (THEY_REJECT leads): 0
Model seconds per program (median): 7

| Class | Count |
|---|---|
| BOTH_REJECT | 9 |
| OUTPUT_DIFFERS | 5 |
| AGREE | 4 |
| WE_REJECT | 2 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 7 programs in 6 signatures

### `OUTPUT_DIFFERS:15.42.4` × 2 — first: fz00000 (clause 15.42.4 Returned value rule)
- programs: fz00000, fz00001
- source: `programs/fz00000.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `IDX 01 ARG 000005{ FRAC 000005{` | `IDX 01 ARG +00000.50 FRAC +00000.50`
  - line 2: `IDX 02 ARG 000005{ FRAC 000005{` | `IDX 02 ARG +00000.50 FRAC +00000.50`
  - line 3: `IDX 03 ARG 000010} FRAC 000000{` | `IDX 03 ARG -00001.00 FRAC +00000.00`
  - line 4: `IDX 04 ARG 000000{ FRAC 000000{` | `IDX 04 ARG +00000.00 FRAC +00000.00`
  - line 5: `IDX 05 ARG 000005{ FRAC 000005{` | `IDX 05 ARG +00000.50 FRAC +00000.50`

### `OUTPUT_DIFFERS:14.9.28` × 1 — first: fz00019 (clause 14.9.28 PERFORM statement)
- programs: fz00019
- source: `programs/fz00019.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `AFTER TIMES 0000002E` | `AFTER TIMES +00000025`
  - line 4: `TEST BEFORE 0000002H` | `TEST BEFORE +00000028`
  - line 5: `TEST AFTER 0000003D` | `TEST AFTER +00000034`

### `OUTPUT_DIFFERS:14.9.8` × 1 — first: fz00016 (clause 14.9.8 COMPUTE statement)
- programs: fz00016
- source: `programs/fz00016.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ADD ZERO: 000000{` | `ADD ZERO: +0000000`
  - line 2: `D*2 OK: 019999H` | `D*2 OK: +0199998`
  - line 3: `NEG DIV: 004999R` | `NEG DIV: -0049999`
  - line 4: `ABS NEG: 001234E` | `ABS NEG: +0012345`
  - line 5: `ROUND/3: 00000411E` | `ROUND/3: +0000041.15`

### `OUTPUT_DIFFERS:8.8.1` × 1 — first: fz00010 (clause 8.8.1 Arithmetic expressions)
- programs: fz00010
- source: `programs/fz00010.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `A+B=000000555F` | `A+B=+0000005556`
  - line 2: `A-B=000001913D` | `A-B=+0000019134`
  - line 3: `A*B=008381020N` | `A*B=-0083810205`
  - line 4: `A/1=000001234E` | `A/1=+0000012345`
  - line 5: `0+1=000000000A` | `0+1=+0000000001`

### `WE_REJECT:COBOLNET1740` × 1 — first: fz00007 (clause 15.3.1.6 Week date formats)
- programs: fz00007
- source: `programs/fz00007.cob`
- WiseOwl: `fz00007.cob(8,1): error COBOLNET1740: data item 'WS-EXT': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET2993` × 1 — first: fz00011 (clause 8.8.1 Arithmetic expressions)
- programs: fz00011
- source: `programs/fz00011.cob`
- WiseOwl: `fz00011.cob(22,33): error COBOLNET2993: the unary arithmetic operator '-' is not immediately followed by a separator space or a left parenthesis — ISO §8.7.1 requires it to be immediately preceded and followed by a separator space or a left parenthesis; write a space after it`
