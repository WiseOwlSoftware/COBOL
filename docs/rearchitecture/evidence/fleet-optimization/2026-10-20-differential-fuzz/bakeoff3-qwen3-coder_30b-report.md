# Differential fuzz — triage report

Programs: 20 · judged: 19 · usable: 12 (63% of judged; 5 of them after a repair round) · the model's failures: 7 · GnuCOBOL alone refused (THEY_REJECT leads): 0
Model seconds per program (median): 20

| Class | Count |
|---|---|
| BOTH_REJECT | 7 |
| WE_REJECT | 6 |
| OUTPUT_DIFFERS | 3 |
| AGREE | 3 |
| RUNNER_ERROR | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 9 programs in 4 signatures

### `OUTPUT_DIFFERS:15.99` × 3 — first: fz00003 (clause 15.99 WHEN-COMPILED function)
- programs: fz00003, fz00004, fz00005
- source: `programs/fz00003.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `WHEN-COMPILED: 2026102021474893-0700` | `WHEN-COMPILED: 2026102021474781-0700`
  - line 7: `SECOND: 48 VALID: Y` | `SECOND: 47 VALID: Y`
  - line 8: `FRACTIONAL SECOND: 93` | `FRACTIONAL SECOND: 81`

### `WE_REJECT:COBOLNET1664` × 3 — first: fz00015 (clause 15.53 LOCALE-TIME function)
- programs: fz00015, fz00016, fz00017
- source: `programs/fz00015.cob`
- WiseOwl: `fz00015.cob(32,5): error COBOLNET1664: FUNCTION LOCALE-TIME's locale-name-1 'WS-LOCALE-NAME': 'WS-LOCALE-NAME' is not a locale-name declared by a SPECIAL-NAMES LOCALE clause (ISO §15.53.3 r4 — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph); no LOCALE clause is in sco`

### `WE_REJECT:COBOLNET2966` × 2 — first: fz00008 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz00008, fz00009
- source: `programs/fz00008.cob`
- WiseOwl: `fz00008.cob(59,1): error COBOLNET2966: The EXIT statement shall appear in a sentence by itself that shall be the only sentence in the paragraph or in a section without paragraphs (ISO §14.9.14.3 SR1); this one shares its sentence with 9 other statements`

### `WE_REJECT:COBOLNET1625,COBOLNET1627` × 1 — first: fz00018 (clause 15.6 Summary of functions)
- programs: fz00018
- source: `programs/fz00018.cob`
- WiseOwl: `fz00018.cob(22,1): error COBOLNET1625: data item 'WS-RANDOM': the numeric literal 0.1234 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

## Runner errors: 1 (not compiler verdicts)
- fz00007: model: ollama HTTP 500: b'{"error":"prediction aborted, token repeat limit reached"}'
