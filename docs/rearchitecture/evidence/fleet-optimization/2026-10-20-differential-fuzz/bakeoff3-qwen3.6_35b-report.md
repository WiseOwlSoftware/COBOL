# Differential fuzz — triage report

Programs: 20 · judged: 20 · usable: 9 (45% of judged; 4 of them after a repair round) · the model's failures: 11 · GnuCOBOL alone refused (THEY_REJECT leads): 1
Model seconds per program (median): 12

| Class | Count |
|---|---|
| BOTH_REJECT | 11 |
| OUTPUT_DIFFERS | 5 |
| WE_REJECT | 2 |
| THEY_REJECT | 1 |
| AGREE | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 8 programs in 6 signatures

### `OUTPUT_DIFFERS:15.53` × 2 — first: fz00015 (clause 15.53 LOCALE-TIME function)
- programs: fz00015, fz00017
- source: `programs/fz00015.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `LEAPSEC: [12:34:99            ]` | `LEAPSEC: [                    ]`

### `OUTPUT_DIFFERS:15.99` × 2 — first: fz00003 (clause 15.99 WHEN-COMPILED function)
- programs: fz00003, fz00005
- source: `programs/fz00003.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `WHEN-COMPILED: 2026102021382379-0700` | `WHEN-COMPILED: 2026102021382264-0700`
  - line 8: `SECOND: 23` | `SECOND: 22`
  - line 9: `FRACTION: 79` | `FRACTION: 64`

### `OUTPUT_DIFFERS:15.3.1.5` × 1 — first: fz00009 (clause 15.3.1.5 Format arguments to international date and time functions > Permissible values for data associated with ordinal date formats)
- programs: fz00009
- source: `programs/fz00009.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `0` | `000000000`
  - line 2: `0` | `003501001`
  - line 3: `0` | `000000000`
  - line 4: `0` | `011899365`
  - line 5: `0` | `000000000`

### `THEY_REJECT:8.8.4.2.4` × 1 — first: fz00006 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz00006
- source: `programs/fz00006.cob`
- GnuCOBOL: `fz00006.cob:16: error: data item not signed`

### `WE_REJECT:COBOLNET0819` × 1 — first: fz00007 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz00007
- source: `programs/fz00007.cob`
- WiseOwl: `fz00007.cob(45,1): error COBOLNET0819: MOVE … TO WS-RESULT: MOVE is invalid — a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16) (a NUMERIC-typed function is the Noninteger sender, §15.2 item 4 / §8.4.3.2.`

### `WE_REJECT:COBOLNET1664,COBOLNET1740` × 1 — first: fz00016 (clause 15.53 LOCALE-TIME function)
- programs: fz00016
- source: `programs/fz00016.cob`
- WiseOwl: `fz00016.cob(7,1): error COBOLNET1740: data item 'WS-LOCALE': the VALUE alphanumeric literal (5 characters) exceeds the item's 4 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`
