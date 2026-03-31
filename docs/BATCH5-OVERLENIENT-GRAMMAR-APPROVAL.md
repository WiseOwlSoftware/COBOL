# Grammar-Change Proposal Packet (M412-M426)

Approved grammar delta proposals for all 15 OVERLENIENT gaps. Each includes problem,
rationale, diff, and approval notes. These proposals must be validated by ANTLR and
COBOL expert agents against the actual current grammar rules before implementation.

---

## M412 -- ACCEPT/DISPLAY Invalid Clauses

- **Problem:** ACCEPT/DISPLAY allow arbitrary trailing clauses (WITH, ON, etc.) via
  permissive "tail" productions.
- **Rationale:** ISO 1985 defines a closed set of clauses; extra forms must be rejected.
- **Diff:**
```diff
- accept-statement
-     : ACCEPT identifier accept-tail?
+ accept-statement
+     : ACCEPT identifier (FROM mnemonic-name)?
+     | ACCEPT screen-name
+     ;

- display-statement
-     : DISPLAY display-operand display-tail?
+ display-statement
+     : DISPLAY display-operand (UPON mnemonic-name)? (WITH NO ADVANCING)?
+     | DISPLAY screen-name
+     ;
```
- **Approval:** Restricts to ISO-defined clauses; no ambiguity introduced.

---

## M413 -- IF/ELSE Malformed Conditions

- **Problem:** IF allows missing condition and multiple ELSE branches.
- **Rationale:** ISO requires a condition and at most one ELSE.
- **Diff:**
```diff
- if-statement
-     : IF condition? statement* (ELSE statement*)* END-IF
+ if-statement
+     : IF condition statement* (ELSE statement*)? END-IF
+     ;
```
- **Approval:** Enforces single ELSE and mandatory condition; THEN remains implicit.

---

## M414 -- EVALUATE Malformed WHEN Phrases

- **Problem:** WHEN may be empty; WHEN OTHER can appear multiple times or not last.
- **Rationale:** ISO requires WHEN with conditions; WHEN OTHER once, last.
- **Diff:**
```diff
- evaluate-when
-     : WHEN when-phrase*
+ evaluate-when
+     : WHEN when-condition+
+     ;

- evaluate-body
-     : evaluate-when* evaluate-when-other? evaluate-when*
+ evaluate-body
+     : evaluate-when* evaluate-when-other?
+     ;
```
- **Approval:** Enforces unique, trailing WHEN OTHER; no new conflicts.

---

## M415 -- PERFORM Malformed UNTIL/VARYING

- **Problem:** PERFORM UNTIL may omit condition; VARYING may omit BY/UNTIL.
- **Rationale:** ISO requires UNTIL condition; VARYING FROM/BY/UNTIL.
- **Diff:**
```diff
- perform-until
-     : UNTIL condition?
+ perform-until
+     : UNTIL condition
+     ;

- perform-varying
-     : VARYING identifier (FROM expression)? (BY expression)? (UNTIL condition)?
+ perform-varying
+     : VARYING identifier FROM expression BY expression UNTIL condition
+     ;
```
- **Approval:** Removes illegal omissions; keywords remain disjoint.

---

## M416 -- STRING Malformed INTO/DELIMITED BY

- **Problem:** STRING may omit INTO and DELIMITED BY.
- **Rationale:** ISO requires INTO and DELIMITED BY for each sending item.
- **Diff:**
```diff
- string-sending
-     : expression (DELIMITED BY expression)?
+ string-sending
+     : expression DELIMITED BY expression
+     ;

- string-statement
-     : STRING string-sending+ (INTO identifier)?
+ string-statement
+     : STRING string-sending+ INTO identifier
+     ;
```
- **Approval:** Aligns with spec and with UNSTRING symmetry.

---

## M417 -- UNSTRING Malformed INTO/DELIMITED BY

- **Problem:** UNSTRING may omit INTO/DELIMITED BY; extra phrases allowed.
- **Rationale:** ISO requires INTO and DELIMITED BY; ON OVERFLOW optional.
- **Diff:**
```diff
- unstring-sending
-     : expression (DELIMITED BY expression)?
+ unstring-sending
+     : expression DELIMITED BY expression
+     ;

- unstring-receiving
-     : INTO identifier*
+ unstring-receiving
+     : INTO identifier+
+     ;
```
- **Approval:** Enforces required INTO and delimiters.

---

## M418 -- INSPECT Malformed TALLYING/REPLACING

- **Problem:** TALLYING and REPLACING can be mixed in invalid ways.
- **Rationale:** ISO treats them as distinct alternatives.
- **Diff:**
```diff
- inspect-statement
-     : INSPECT identifier inspect-tail*
+ inspect-statement
+     : INSPECT identifier inspect-tallying
+     | INSPECT identifier inspect-replacing
+     ;
```
- **Approval:** Prevents mixed forms; keyword-driven.

---

## M419 -- CALL Malformed USING/RETURNING

- **Problem:** CALL allows repeated USING and invalid RETURNING.
- **Rationale:** ISO: optional single USING list; optional RETURNING data item.
- **Diff:**
```diff
- call-statement
-     : CALL expression call-using* call-returning?
+ call-statement
+     : CALL expression (USING call-arg+)? (RETURNING identifier)?
+     ;
```
- **Approval:** Enforces single USING and valid RETURNING target.

---

## M420 -- SET Malformed TO/UP/DOWN

- **Problem:** SET allows missing identifiers and mixed directions.
- **Rationale:** ISO defines three distinct forms.
- **Diff:**
```diff
- set-statement
-     : SET set-target set-tail*
+ set-statement
+     : SET identifier TO expression
+     | SET identifier UP BY expression
+     | SET identifier DOWN BY expression
+     ;
```
- **Approval:** Makes each form explicit and exclusive.

---

## M421 -- SEARCH Malformed WHEN/AT END

- **Problem:** SEARCH may omit WHEN; AT END may repeat; VARYING misused.
- **Rationale:** ISO requires WHEN; AT END optional, unique.
- **Diff:**
```diff
- search-statement
-     : SEARCH identifier search-body
+ search-statement
+     : SEARCH identifier WHEN condition statement* (AT END statement*)?
+     ;
```
- **Approval:** Enforces WHEN and single AT END.

---

## M422 -- SORT Malformed ON ASCENDING/DESCENDING

- **Problem:** SORT may omit ON ASC/DESC and keys; mix ASC/DESC.
- **Rationale:** ISO requires ON ASCENDING/DESCENDING KEY data-name+.
- **Diff:**
```diff
- sort-keys
-     : (ASCENDING|DESCENDING)? KEY identifier*
+ sort-keys
+     : (ASCENDING|DESCENDING) KEY identifier+
+     ;
```
- **Approval:** Enforces presence of mode and keys.

---

## M423 -- MERGE Malformed ON ASCENDING/DESCENDING

- **Problem:** Same as SORT.
- **Rationale:** Same as SORT.
- **Diff:**
```diff
- merge-keys
-     : (ASCENDING|DESCENDING)? KEY identifier*
+ merge-keys
+     : (ASCENDING|DESCENDING) KEY identifier+
+     ;
```
- **Approval:** Mirrors SORT behavior.

---

## M424 -- WRITE Malformed FROM/ADVANCING

- **Problem:** WRITE allows missing/duplicate FROM and invalid ADVANCING.
- **Rationale:** ISO: optional single FROM; optional ADVANCING phrase.
- **Diff:**
```diff
- write-statement
-     : WRITE identifier write-tail*
+ write-statement
+     : WRITE identifier (FROM identifier)? (ADVANCING expression)?
+     ;
```
- **Approval:** Normalizes to ISO forms.

---

## M425 -- READ Malformed INTO/AT END

- **Problem:** READ allows missing INTO, multiple AT END, invalid KEY.
- **Rationale:** ISO: optional single INTO; optional single AT END.
- **Diff:**
```diff
- read-statement
-     : READ identifier read-tail*
+ read-statement
+     : READ identifier (INTO identifier)? (AT END statement*)?
+     ;
```
- **Approval:** Enforces unique INTO and AT END.

---

## M426 -- OPEN/CLOSE Malformed Modes

- **Problem:** OPEN allows no mode or multiple modes; CLOSE allows extra forms.
- **Rationale:** ISO: one mode per OPEN; CLOSE file list only.
- **Diff:**
```diff
- open-statement
-     : OPEN open-mode* file-name+
+ open-statement
+     : OPEN open-mode file-name+
+     ;

- close-statement
-     : CLOSE close-tail*
+ close-statement
+     : CLOSE file-name+
+     ;
```
- **Approval:** Tightens to spec; keeps parsing simple.

---

## Conformance Matrix

| Item | Subsystem | Severity | Status | Test File |
|------|-----------|----------|--------|-----------|
| M412 | Grammar | P3 | proposed | M412_OverlenientAcceptDisplayTests |
| M413 | Grammar | P3 | proposed | M413_OverlenientIfElseTests |
| M414 | Grammar | P3 | proposed | M414_OverlenientEvaluateTests |
| M415 | Grammar | P3 | proposed | M415_OverlenientPerformTests |
| M416 | Grammar | P3 | proposed | M416_OverlenientStringTests |
| M417 | Grammar | P3 | proposed | M417_OverlenientUnstringTests |
| M418 | Grammar | P3 | proposed | M418_OverlenientInspectTests |
| M419 | Grammar | P3 | proposed | M419_OverlenientCallTests |
| M420 | Grammar | P3 | proposed | M420_OverlenientSetTests |
| M421 | Grammar | P3 | proposed | M421_OverlenientSearchTests |
| M422 | Grammar | P3 | proposed | M422_OverlenientSortTests |
| M423 | Grammar | P3 | proposed | M423_OverlenientMergeTests |
| M424 | Grammar | P3 | proposed | M424_OverlenientWriteTests |
| M425 | Grammar | P3 | proposed | M425_OverlenientReadTests |
| M426 | Grammar | P3 | proposed | M426_OverlenientOpenCloseTests |

---

## Review Gate

All grammar deltas require validation by ANTLR expert and COBOL expert agents against:

1. The actual current grammar rules (not the simplified pseudocode above)
2. Existing test programs that may rely on the lenient forms
3. NIST test suite for regressions
