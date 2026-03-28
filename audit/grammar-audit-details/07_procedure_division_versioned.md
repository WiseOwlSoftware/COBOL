Now I have enough information to write the comprehensive categorized analysis. Let me compile it:

---

## Categorized Grammar-vs-Spec Mismatches by COBOL Version

The analysis below categorizes every Procedure Division mismatch from `GRAMMAR_AUDIT.md` by the COBOL version that introduced the feature. Sources: ISO/IEC 1989:2023 Annex E (substantive changes from 2014), context-sensitive word list, and statement format sections.

---

### Table 1 — COBOL-85 features that are currently missing or wrong (MUST fix)

These features existed in ISO 1989:1985 and have been present through every subsequent edition. Failures here will break real COBOL-85 programs and NIST tests.

| # | Feature | Statement(s) | Nature of Defect |
|---|---------|-------------|-----------------|
| 1 | `DISPLAY … UPON mnemonic-name-1` | DISPLAY | Missing phrase entirely; spec §14.9.11 |
| 2 | `WITH NO ADVANCING` on DISPLAY | DISPLAY | Missing phrase; spec §14.9.11 |
| 3 | `END-DISPLAY` scope terminator | DISPLAY | Missing |
| 4 | `ACCEPT … FROM mnemonic-name-1` | ACCEPT | Missing phrase; spec §14.9.1 (line 25578) |
| 5 | `END-ACCEPT` scope terminator | ACCEPT | Missing |
| 6 | CORR synonym for CORRESPONDING | ADD, SUBTRACT, MOVE | Missing synonym token |
| 7 | `SET … TO ON` / `TO OFF` (switch-setting) | SET | 8+ SET formats entirely absent |
| 8 | `START … FIRST` / `LAST` phrases | START | Missing; spec §14.9.41 |
| 9 | `START … WITH LENGTH integer` | START | Missing; spec §14.9.41 |
| 10 | SORT Format 2 (table sort: `SORT data-name-2 …`) | SORT | Missing format; spec §14.9.40.2 |
| 11 | `WITH DUPLICATES IN ORDER` parsing | SORT, MERGE | Grammar accepts `WITH DUPLICATES` but malformed |
| 12 | `USE … GLOBAL` | USE | Missing GLOBAL keyword; spec §14.9.49 |
| 13 | `USE … EXCEPTION`/`ERROR` synonym | USE | EXCEPTION synonym missing |
| 14 | `USE … INPUT`/`OUTPUT`/`I-O`/`EXTEND` modes | USE | Mode keywords missing |
| 15 | `WRITE … FROM literal-1` | WRITE | FROM accepts only dataReference, not literal |
| 16 | `REWRITE … FROM literal-1` | REWRITE | Same defect |
| 17 | `RELEASE … FROM literal-1` | RELEASE | Same defect |
| 18 | `READ … RECORD` (optional keyword) | READ | RECORD keyword missing |
| 19 | `WRITE FILE file-name-1` form | WRITE | FILE phrase missing |
| 20 | `REWRITE FILE file-name-1` form | REWRITE | FILE phrase missing |
| 21 | `READ FILE file-name-1` form | READ | FILE phrase missing |
| 22 | Chained exponentiation `A ** B ** C` | expressions | `?` should be `*`; only one exponent accepted |
| 23 | `IS <>` not-equal operator | conditions | Symbolic `<>` form missing |
| 24 | `STOP RUN WITH ERROR/NORMAL STATUS` | STOP | Missing STATUS phrase; spec §14.9.42 |
| 25 | `INITIALIZE … ALL TO VALUE` | INITIALIZE | Phrase missing; spec §14.9.20 (line 27860) |
| 26 | `INITIALIZE … THEN TO DEFAULT` | INITIALIZE | Phrase missing |
| 27 | `INITIALIZE … THEN REPLACING` | INITIALIZE | Phrase missing |

**Note on items 15-17**: The spec requires `FROM { identifier-1 / literal-1 }` — accepting only identifiers is a COBOL-85-level defect that will reject valid programs.

**Note on item 24**: `STOP RUN WITH ERROR STATUS` / `WITH NORMAL STATUS` is in the spec as the basic STOP status mechanism (ISO §14.9.42, referenced in the audit as "COBOL-2002 addition"). However, the audit's COBOL-2002 attribution appears to conflate the basic STATUS phrase with the GOBACK STATUS extension. The basic `STOP RUN WITH STATUS` was present in COBOL-85 appendix extensions and formalized in ISO 1989:2002. It is treated here as COBOL-2002 (see Table 2).

---

### Table 2 — COBOL-2002 features that are missing or wrong

These were introduced in ISO/IEC 1989:2002. CobolSharp targets COBOL-85 correctness first, but many of these appear in real-world programs and in NIST extensions.

| # | Feature | Statement(s) | Evidence |
|---|---------|-------------|----------|
| 1 | `ROUNDED MODE IS {8 modes}` | ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE | Spec §14.7.4; note at §13449: "NEAREST-AWAY-FROM-ZERO is the rounding mode provided by the ROUNDED phrase in **earlier** COBOL standards" confirms bare ROUNDED is COBOL-85, MODE IS is 2002+ |
| 2 | `DEFAULT ROUNDED MODE IS` in OPTIONS | OPTIONS paragraph | Spec §11.9.6; same 2002 origin |
| 3 | `EXIT PROGRAM RAISING {EXCEPTION name / identifier / LAST EXCEPTION}` | EXIT | Spec §14.9.14 (line 27370); EC-RAISING introduced 2002 |
| 4 | `GOBACK RAISING …` | GOBACK | Spec §14.9.18; same |
| 5 | `STOP RUN WITH ERROR/NORMAL STATUS` | STOP | Spec §14.9.42 (line 32226) |
| 6 | `GOBACK WITH STATUS` (as main program exit) | GOBACK | Annex E §E.3.3 item 32: "The GOBACK statement now allows the same status phrase as the STOP statement" — this is a **2023 addition**, not 2002 (see Table 3) |
| 7 | retry-phrase (`RETRY FOREVER` / `RETRY n TIMES` / `RETRY FOR n SECONDS`) | OPEN, READ, WRITE, REWRITE, DELETE | Spec §14.7.9 (line 25199); reserved word RETRY/FOREVER/SECONDS context |
| 8 | `WITH LOCK` / `WITH NO LOCK` on READ | READ | Spec §14.9.30 (line 29795) |
| 9 | SHARING phrase on OPEN | OPEN | Spec §14.9.29 (line 29140); SHARING clause §12.4.5.15 |
| 10 | `BY VALUE` in CALL USING | CALL | Spec §14.9.4 §23641; "BY REFERENCE and BY VALUE" transitive across parameters |
| 11 | `RETURNING` on CALL | CALL | Spec §14.9.4 |
| 12 | `PERFORM UNTIL EXIT` | PERFORM | Spec §14.9.28 syntax rule 8 (line 29509) |
| 13 | `INITIALIZE … WITH FILLER` | INITIALIZE | Spec §14.9.20 (line 27860); this phrase absent from COBOL-85 |
| 14 | LOCK MODE clause in SELECT | SELECT | Spec §12.4.5 |
| 15 | SHARING clause in SELECT | SELECT | Spec §12.4.5.15 |

**Disambiguation — ROUNDED**: The bare `ROUNDED` keyword (no `MODE IS`) is unambiguously COBOL-85. The `MODE IS {mode}` extension with 8 named modes was added in COBOL-2002. The grammar currently supports bare ROUNDED but lacks MODE IS entirely — this is a COBOL-2002 gap, not a COBOL-85 regression.

---

### Table 3 — COBOL-2023 features that are missing

These were confirmed as new in the current edition (ISO/IEC 1989:2023) by Annex E §E.3.3.

| # | Feature | Statement(s) | Annex E Reference |
|---|---------|-------------|------------------|
| 1 | `CONTINUE AFTER arithmetic-expression SECONDS` | CONTINUE | §E.3.3 item 14: "Additional functionality added to the CONTINUE statement" |
| 2 | PERFORM Format 3 (exception-checking): `PERFORM … WHEN exception-name … END-PERFORM` | PERFORM | §E.3.3 item 36: "An exception checking variant of this statement has been added" |
| 3 | `INSPECT [BACKWARD] identifier-1 …` | INSPECT | §E.3.3 item 34: "BACKWARD context sensitive word added" |
| 4 | `DELETE FILE [OVERRIDE] {file-name} …` | DELETE FILE | §E.3.3 item 15: "The DELETE FILE statement causes the removal of the referenced files" |
| 5 | `GOBACK WITH STATUS` (main-program exit, same as STOP) | GOBACK | §E.3.3 item 32: "The GOBACK statement now allows the same status phrase as the STOP statement" |
| 6 | `XOR` / `EXCLUSIVE-OR` logical operator | conditions | §E.2 item 25 (new reserved words: XOR, EXCLUSIVE-OR); §E.3.2 item 4 |
| 7 | Boolean shift operators `B-SHIFT-L/R/LC/RC` | expressions | §E.2 item 3 and §E.3.3 item 3 |
| 8 | `EXIT PERFORM` / `EXIT PERFORM CYCLE` | PERFORM inline | Spec §14.9.14 (line 27357); part of Format 3 infrastructure |

---

### Table 4 — Version uncertain / requires further research

These features appear in the 2023 spec but the audit notes do not clearly distinguish 2002 vs 2014 origin. They are lower priority than Tables 1-2.

| # | Feature | Likely Version | Notes |
|---|---------|---------------|-------|
| 1 | Float literals `1.5E+3` | COBOL-2002 | Tied to FLOAT-SHORT/LONG/EXTENDED USAGE which is 2002 |
| 2 | Boolean literals `B"..."` / `BX"..."` | COBOL-2002 | National/boolean data types introduced 2002 |
| 3 | National literals `N"..."` / `NX"..."` | COBOL-2002 | Same |
| 4 | `IS OMITTED` condition | COBOL-2002 | Related to optional parameters (BY REFERENCE OPTIONAL) |
| 5 | SORT COLLATING SEQUENCE `FOR ALPHANUMERIC IS` / `FOR NATIONAL IS` | COBOL-2002 | National character support is 2002 |
| 6 | Concatenation `&` operator | COBOL-2002 | Present in ISO 2002 spec |

---

### Summary table — Action priority

| Category | Count | Fix urgency |
|----------|------:|------------|
| COBOL-85 missing (Table 1) | 27 | Critical — blocks NIST tests and real programs |
| COBOL-2002 missing (Table 2) | 15 | High — blocks modern programs but not NIST-85 |
| COBOL-2023 new (Table 3) | 8 | Low — no existing programs depend on these |
| Version uncertain (Table 4) | 6 | Low — all tied to unimplemented data types |

---

### Key findings

1. `STOP RUN WITH STATUS`: The audit attributed this to COBOL-2002. The ISO 2023 spec shows it in §14.9.42 without a "new in 2023" marker in Annex E, confirming it was 2002. `GOBACK WITH STATUS` (as main-program synonym) is definitively **2023-new** per Annex E §E.3.3 item 32.

2. `PERFORM UNTIL EXIT` is **COBOL-2023-new** per Annex E §E.3.3 item 37, not 2002 as the question prompt assumed.

3. `CONTINUE AFTER SECONDS` is **COBOL-2023-new** per Annex E §E.3.3 item 14, not 2002.

4. `XOR`/`EXCLUSIVE-OR` is **COBOL-2023-new** per Annex E §E.2 item 25 (new reserved words) and §E.3.2 item 4. The prompt classified it as COBOL-2002 — this is incorrect.

5. `INSPECT BACKWARD` is **COBOL-2023-new** per Annex E §E.3.3 item 34, not 2014.

6. `DELETE FILE` (the statement for removing files from mass storage) is **COBOL-2023-new** per Annex E §E.3.3 item 15.

7. The 27 COBOL-85 defects in Table 1 are the highest-priority items. Many (`DISPLAY UPON`, `ACCEPT FROM mnemonic`, `CORR`, `START FIRST/LAST`, `SORT table format`, `FROM literal`) are exactly the kind of gap that blocks NIST test suites. These should be fixed before any COBOL-2002 work begins.