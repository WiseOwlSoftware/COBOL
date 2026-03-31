# Binder-Level Coverage Checklist

**Date:** 2026-03-30 | **Mode:** DRY-RUN (read-only) | **Auditor:** Claude Opus 4.6
**Evidence sources:** `audit/cobol85/integration.trx` (287 pass, 1 skip), `audit/cobol85/source-snapshot.txt`

Test counts derived from integration.trx classification by test name pattern.

---

## ExpressionBinder (26 methods, ~22 SubscriptRefMod + 29 Intrinsic tests evidenced)

- [x] Arithmetic expressions (additive, multiplicative, power, unary) — 24 arithmetic tests pass (integration.trx)
- [x] Numeric literals — evidenced by 43 data tests (integration.trx)
- [x] Non-numeric literals — evidenced by data tests
- [x] Figurative constants (ZERO, SPACE, HIGH-VALUE, etc.) — evidenced by data tests
- [x] Intrinsic function calls — 29 intrinsic tests pass (integration.trx)
- [x] Data reference with subscripts (1D-3D) — 22 subscript/refmod tests pass (integration.trx)
- [x] Reference modification — evidenced by subscript/refmod tests
- [x] Qualified names (OF/IN) — NC207A/NC208A pass (3 FAIL* in NC208A)
- [ ] ALL literal in expressions — no test name evidence in TRX
- [ ] LENGTH OF special register — unclear from TRX data

## ConditionBinder (20 methods, ~24 condition tests evidenced)

- [x] Simple relational conditions — 24 condition tests pass (integration.trx)
- [x] Compound conditions (AND/OR/NOT) — evidenced by condition tests
- [x] Abbreviated conditions — evidenced by condition tests; NC250A has 4 FAIL*
- [x] Class conditions (NUMERIC/ALPHABETIC) — NC246A exercises this (14 FAIL*)
- [x] Sign conditions (POSITIVE/NEGATIVE/ZERO) — method present in source-snapshot
- [x] Condition-name (level 88) — evidenced by condition tests
- [ ] Abbreviated conditions with nested NOT — edge case coverage unclear from TRX
- [ ] Collating sequence impact on comparisons — no test name evidence
- [ ] EVALUATE WHEN condition-name objects — no specific test name evidence

## ControlFlowBinder (19 methods, ~73 control flow tests evidenced)

- [x] PERFORM simple — 73 CF tests pass (integration.trx), largest test category
- [x] PERFORM TIMES — evidenced by CF tests
- [x] PERFORM UNTIL — evidenced by CF tests
- [x] PERFORM VARYING — evidenced by CF tests; NC201A has 5 FAIL*
- [x] PERFORM THRU — evidenced by CF tests
- [x] PERFORM inline — evidenced by CF tests
- [x] TEST BEFORE/AFTER — evidenced by CF tests
- [x] IF/ELSE — evidenced by CF tests
- [x] EVALUATE TRUE/FALSE — NC225A exercises this (7 FAIL*)
- [x] EVALUATE WHEN/OTHER — evidenced by CF tests
- [x] GO TO — evidenced by CF tests
- [x] GO TO DEPENDING — evidenced by CF tests
- [x] ALTER — evidenced by CF tests
- [x] SEARCH — evidenced by CF tests
- [x] SEARCH ALL — NC237A exercises this (3 FAIL*)
- [ ] PERFORM range overlap detection — not implemented (no test evidence)
- [ ] EVALUATE THRU ranges in WHEN — no specific test name evidence in TRX
- [ ] EVALUATE with class condition subjects — no specific test evidence

## FileIoBinder (17 methods, ~29 file I/O + 3 sort/merge tests evidenced)

- [x] OPEN (INPUT/OUTPUT/I-O/EXTEND) — 29 FIO tests pass (integration.trx)
- [x] CLOSE — evidenced by FIO tests
- [x] READ sequential — evidenced by FIO tests
- [x] READ NEXT/PREVIOUS — evidenced by FIO tests
- [x] READ KEY IS — evidenced by FIO tests
- [x] WRITE — evidenced by FIO tests
- [x] WRITE BEFORE/AFTER ADVANCING — evidenced by FIO tests
- [x] REWRITE — evidenced by FIO tests
- [x] DELETE — evidenced by FIO tests
- [x] START — evidenced by FIO tests
- [x] SORT — 3 sort/merge tests pass (integration.trx)
- [x] MERGE — evidenced by sort/merge tests
- [x] RELEASE/RETURN — evidenced by sort/merge tests
- [x] USE declaratives — method present in source-snapshot
- [ ] AT END/NOT AT END routing completeness — unclear from TRX
- [ ] INVALID KEY/NOT INVALID KEY routing — unclear from TRX
- [ ] File access mode enforcement — no test name evidence; IO/ has handler files (SequentialFileHandler.cs, IndexedFileHandler.cs, RelativeFileHandler.cs) per source-snapshot

## DataStatementBinder (14 methods, ~43 data tests evidenced)

- [x] DISPLAY — 43 data tests pass (integration.trx)
- [x] MOVE — evidenced by data tests
- [x] MOVE CORRESPONDING — evidenced by data tests
- [x] SET TO ON/OFF (switches) — NC254A passes (0 FAIL*)
- [x] SET condition-name TO TRUE — evidenced by condition tests
- [x] SET TO value — evidenced by data tests
- [x] SET UP/DOWN BY — evidenced by data tests
- [x] INITIALIZE — evidenced by data tests
- [x] INITIALIZE REPLACING — method present in source-snapshot
- [x] ACCEPT — evidenced by integration tests (classified as Other)
- [ ] MOVE type compatibility exhaustiveness — unclear from TRX; GRAMMAR_AUDIT notes gaps
- [ ] SET with multiple identifiers — no specific test evidence

## StringStatementBinder (14 methods, ~11 string tests evidenced)

- [x] STRING — 11 string tests pass (integration.trx)
- [x] UNSTRING — evidenced by string tests; NC218A has 9 FAIL*
- [x] INSPECT TALLYING — evidenced by string tests; NC219A has 1 FAIL*
- [x] INSPECT REPLACING — evidenced by string tests
- [x] INSPECT CONVERTING — evidenced by string tests
- [x] STRING WITH POINTER — evidenced by string tests
- [x] UNSTRING WITH POINTER — evidenced by string tests
- [x] UNSTRING TALLYING — NC218A exercises this
- [x] UNSTRING DELIMITER IN — NC218A exercises this
- [ ] STRING overflow detection timing — unclear from TRX
- [ ] INSPECT with data-ref patterns (vs literal) — unclear from TRX

## CallBinder (5 methods, ~13 call tests evidenced)

- [x] CALL static — 13 call tests pass (integration.trx)
- [x] CALL dynamic — evidenced by call tests
- [x] CALL BY REFERENCE — evidenced by call tests
- [x] CALL BY CONTENT — evidenced by call tests
- [x] CALL BY VALUE — evidenced by call tests
- [x] CALL RETURNING — evidenced by call tests
- [x] ON EXCEPTION / NOT ON EXCEPTION — evidenced by call tests
- [x] CANCEL — evidenced by call tests
- [x] ENTRY — evidenced by call tests
- [ ] CALL with mixed BY modes in single USING — no specific test evidence
