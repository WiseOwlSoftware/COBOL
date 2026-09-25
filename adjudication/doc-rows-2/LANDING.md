# doc-rows-2 — LANDING (PB1522 step 3, the last 51 Annex A.1 determinations)

Landed 2026-09-25 by the doc-rows-2 lander. Writers d1–d7 wrote a §7 row + a staged inventory record per item; a refuter attacked every item; the lander applied each overturned item's correction (`build.py`), wrote the rows into `docs/CONFORMANCE.md` §7 (`apply.py`, `apply210.py`, `fixtokens.py`, `preamble.py`, `fix121.py`), recorded the 51 records in ONE batch (`batch-doc-rows-2.json`, `record.out`: 51 of 51 accepted, GAP 1247 → 1241) and registered the result in kb/Work (`notes.py`, `notes2.py`). Owner decisions taken during the round: **R47** (a record written to a sequential/report file with no CODE-SET clause is Latin-1; a character above U+00FF is refused with an I-O status) and **R48** (STOP RUN / a fatal EC in a hosted program ends the run unit only).

**Verdicts:** DIVERGES 24, PARTIAL 14, CONFORMS 11, NOT-IMPLEMENTED 1, DOCUMENTED-NON-SUPPORT 1. §7 now discharges **182 of 182** documentation obligations (`audit_annex_a1.py`).

## Lander decisions beyond the refuters' corrections (review these)

- **Item 89 reconciled with item 14.** The d1 writer+refuter determined for CALL (item 14) that a separately compiled module is LOCATED only once `ProgramTable.ProbeSiblingModule` has loaded it and run its registrar, so the checked resource set is empty and a load failure is EC-PROGRAM-NOT-FOUND (CONFORMS); the d3 writer+refuter determined the opposite for a function (item 89: a module found on disk is located, a load failure is EC-PROGRAM-RESOURCES, DIVERGES). One lookup cannot carry two meanings of 'located'. ISO leaves the locating mechanics to the implementor, so CLAUDE.md rule 1 goes to GnuCOBOL (libcob/call.c:270-277 `set_resolve_error`: NOT-FOUND for both on a dlopen/dlsym failure; no EC-PROGRAM-RESOURCES raise site): item 89 was rewritten to item 14's determination and recorded CONFORMS (test-needed); item 102 (INVOKE) keeps DIVERGES and cross-references both. PB1422 releases 14 and 89.

- **Item 99 recorded DOCUMENTED-NON-SUPPORT, not DIVERGES.** Its row opens 'Not provided.' on an A.1-optional item, which the derived-verdict selector `a1-optional-not-provided` (owner, PB280 Q1) fixes at DOCUMENTED-NON-SUPPORT (`DerivedVerdictDriftTests.NoOptionalNotProvidedA1Row_Diverges`). The EC-IMP-suffix under-rejection stays PB1531's.

- **Item 40** follows GnuCOBOL's DIRECTORY order too (working directory, then `--copy` dirs; the source file's own directory is not searched) per the orchestrator's rule-1 decision — PB1355's implementer must measure the golden fan-out first (the working directory becomes load-bearing).

- **Item 210 (FUNCTION-POINTER)** reworded to the DOC-A.1-56 posture after a probe (`probe-fp210.cob.txt`): a FUNCTION-POINTER leaf of a STRONG group counts 8 positions and images as 8 spaces, NULL or set (LENGTH 13 for X(3)+FP+X(2)).

- **Item 164** keeps the refuter's corrected row and states that a relative/indexed file's in-memory store is not an input-output area (the refuter asked that the organization scope not be extended unverified).

- **Item 146** arm (c) now uses item 114's host delimiter (the writer's 'two-byte CR LF' contradicted DOC-A.1-114), and the byte-per-position dependency cites R47 (decided) instead of an open owner question.

- `DerivedVerdictDriftTests`: the 'optional, undetermined' anchor moved 67 → 85 and the 'conditional, undetermined' anchor (36) retired — every conditional item that can arise now has a row.

## Other §7 / §3 edits in the same change set

DOC-A.1-26 (no-mark Latin-1 fallback + marked-malformed error), -56 (the two duplicate rows merged into one), -62 (the table constant is not the carrier-headroom rule), -65 (run-unit ending → item 167), -76 (→ items 75, 77), -106/-110 ('91' defined: fatal, EC-I-O-IMP; ⚠ PB690), -115 (U+00FF ceiling for an alphanumeric record area, R47; LF delimiter, lone CR is data; ⚠ PB690), -143 (no call-convention-name), -153 (example flagged as today's behaviour; re-derived with PB322 A), -160 (PB690 pointer answered by item 159), -210 (above); §3 >>CALL-CONVENTION / >>DISPLAY bullet, non-COBOL-return bullet, SORT/MERGE fatal-status parenthetical (→ item 103), D-FRA (iv) (→ item 147, FD and SD); the §7 preamble's status and counts. Prose citations use 'item N' (the `DOC-A.1-N` token is a row key only).

## Leads (not landed here)

- PB322 A's landing owes: DOC-A.1-153's example + golden `2002/pb669_lock_visibility_plain_connector`; golden `2002/pb316_open_group_scope`; docs/PHASE4_RECONCILIATION.md:1523; `FileRegistry.ImplementorDefaultSharing` summary; FileLockPosture remark; COBOLNET_FILES_DESIGN.md ~l.701 (recorded in PB322).
- Golden `2023/pb964_ls_plain_write_after_after` moves with PB1540 (host newline).
- PB690 owes `FileStatusCode` '91', the FileStatus.cs 'ONE IMPLEMENTOR-DEFINED I-O STATUS' comment, and the `LineSequentialCharacterSet` U+00FF ceiling (recorded in PB690).
- PB1099 still claims DOC-A.1-161/-162 while PB1086 owns them (Q3 answered by R44) — the registrar may release them.
- Witnesses owed (CONFORMS test-needed): items 14, 89, 116, 164 (RSV/RSV2 shape, PB1587), 218 (checked arm).

## Per item

| Item | Verdict | Owning note | Refuter | What changed at landing |
|---|---|---|---|---|
| 14 | CONFORMS | PB1422 | overturned (citation) | refuter: cross-reference 19→15/16, 'everything acquired while located' overclaim replaced, 'missing dependency' narrowed, GnuCOBOL warning difference stated; lander: items 89/102 cross-reference |
| 20 | PARTIAL | PB1402 | overturned (determination) | refuter: present-tense 'one folding function' claim replaced by today's ordinal-ignore-case; the one Annex C function moved into the ⚠ as PB1402's obligation |
| 23 | PARTIAL | PB1543 | overturned (determination) | refuter: TAB and the line end added to what bounds a text-word; the ⚠ narrowed to the OTHER White_Space characters |
| 25 | PARTIAL | PB1397 | overturned (determination) | refuter + orchestrator: a marked file whose content is malformed is a compile-time error naming the file and byte offset, no fallback; ⚠ covers the marked arm |
| 31 | DIVERGES | PB690 | overturned (citation) | refuter: R48 second section is an orchestrator application, not an owner decision; lander: item 25's Latin-1 fallback referenced |
| 36 | CONFORMS | PB547 | overturned (citation) | refuter: attribution |
| 38 | CONFORMS | PB547 | upheld | writer's row as written |
| 39 | PARTIAL | PB1529 | overturned (verdict) | refuter: ⚠ widened to the native-lane Int128 product wrap (BIG * BIG suspends 0 s) |
| 40 | PARTIAL | PB1355 | overturned (determination) | refuter + orchestrator (rule 1): GnuCOBOL's suffix order ('' .CPY .CBL .COB .cpy .cbl .cob; a name with a period as spelled only) AND directory order (working directory, then --copy dirs); both arms added to the ⚠ and to PB1355 |
| 46 | DIVERGES | PB547 | upheld | writer's row as written |
| 49 | DIVERGES | PB1533 | upheld | writer's row as written |
| 52 | CONFORMS | PB322 | upheld | writer's row as written |
| 53 | DIVERGES | PB1538 | upheld | writer's row as written |
| 54 | DIVERGES | PB807 | upheld | writer's row as written |
| 55 | DIVERGES | PB1538 | upheld | writer's row as written |
| 60 | PARTIAL | PB1410 | overturned (determination) | refuter's corrected row adopted verbatim |
| 63 | CONFORMS | PB1094 | upheld | writer's row as written |
| 66 | DIVERGES | PB1086 | overturned (determination) | refuter's corrected row adopted verbatim |
| 67 | NOT-IMPLEMENTED | PB1086 | overturned (determination) | refuter + orchestrator: trimmed to R44's decision (no binding-identity/version/'nothing else' claims); verdict NOT-IMPLEMENTED under PB1086; the R44-only anchor row is written because a DOC NOT-IMPLEMENTED verdict still needs its anchor |
| 68 | DIVERGES | PB1383 | overturned (citation) | refuter: the false GnuCOBOL attribution for AS literals replaced (GnuCOBOL keeps AS literals verbatim; trimming AS names is COBOL.NET's own one-rule choice); ⚠ adds that no warning exists today |
| 75 | DIVERGES | PB833 | overturned (determination) | refuter: the lock-type table (which cannot deliver Table 19 on Unix) replaced by the guarantee — lock keyed on sharing mode AND open mode; the lock-layout flaw recorded in PB833 |
| 77 | DIVERGES | PB322 | upheld | writer's row as written |
| 81 | PARTIAL | PB1110 | upheld | writer's row as written |
| 89 | CONFORMS | PB1422 (released; CONFORMS) | upheld | LANDER reconciliation with item 14 (one ProbeSiblingModule, one meaning of 'located', GnuCOBOL call.c set_resolve_error): rewritten to the empty checked set, EC-FUNCTION-NOT-FOUND for an unloadable module; verdict DIVERGES -> CONFORMS; released from PB1422 |
| 99 | DOCUMENTED-NON-SUPPORT | PB1531 | upheld | lander: verdict DIVERGES -> DOCUMENTED-NON-SUPPORT (derived-verdict selector a1-optional-not-provided); the defect stays PB1531's |
| 102 | DIVERGES | PB1422 | upheld | refuter nit: header parenthetical; lander: cross-reference reconciled with items 14 and 89 |
| 103 | PARTIAL | PB1512 | upheld | writer's row as written |
| 105 | DIVERGES | PB1541 | upheld | refuter nit: '38' is 85–2014 only |
| 107 | DIVERGES | PB1192 | upheld | refuter nit: the too-many-digits '24' is the sequential-access case |
| 108 | DIVERGES | PB1192 | upheld | writer's row as written |
| 113 | PARTIAL | PB1532 | upheld | writer's row as written |
| 114 | DIVERGES | PB1540 | overturned (citation) | refuter: '06' is §9.1.13.2 item 5, not item 6 |
| 116 | CONFORMS | PB1253 | upheld | writer's row as written |
| 121 | PARTIAL | PB1520 | upheld | lander: the row's Pinned-by cell names the two goldens its record cites |
| 124 | PARTIAL | PB1526 | overturned (citation) | refuter: record-notes GnuCOBOL precedence claim corrected (row unchanged) |
| 131 | DIVERGES | PB322 | upheld | writer's row as written |
| 146 | DIVERGES | PB1276 | overturned (citation) | refuter: FORMAT citation (items 84/85 cannot arise, COBOLNET1705), arm (c) GR22 + '71'; lander: the byte-per-position dependency is now R47's decision, and arm (c)'s delimiter follows item 114 |
| 147 | DIVERGES | PB322 | upheld | writer's row as written |
| 156 | DIVERGES | PB1496 | upheld | writer's row as written |
| 157 | DIVERGES | PB1491 | overturned (determination) | refuter: TAB = next tab stop of 8 (GnuCOBOL tab-width), new defect PB1586 named in the ⚠; lander: Latin-1 fallback of item 25 referenced |
| 159 | DIVERGES | PB690 | upheld | writer's row as written |
| 161 | PARTIAL | PB1086 | overturned (determination) | refuter's corrected row adopted verbatim |
| 162 | PARTIAL | PB1086 | overturned (determination) | refuter's corrected row adopted verbatim |
| 164 | CONFORMS | PB643 | overturned (determination) | refuter's corrected row adopted verbatim; lander: the organization scope the refuter flagged is stated, not extended |
| 167 | DIVERGES | PB1149 | upheld | writer's row as written |
| 168 | DIVERGES | PB1069 | upheld | writer's row as written |
| 192 | CONFORMS | PB1178 | upheld | refuter nit: record test-ref carries the row's fourth test |
| 214 | PARTIAL | PB1527 | upheld | writer's row as written |
| 217 | CONFORMS | PB1527 | upheld | writer's row as written |
| 218 | CONFORMS | PB322 | overturned (contradiction) | refuter: the 'no fatal 9x' clause contradicted DOC-A.1-110 — now names '90' and '91'; notes cite codegen.c |
| 219 | DIVERGES | PB1402 | upheld | refuter: notes' GnuCOBOL claim now cited from scanner.l / call.c |
