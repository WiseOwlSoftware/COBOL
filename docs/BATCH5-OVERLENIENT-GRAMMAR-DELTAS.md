# Batch 5 OVERLENIENT Grammar Deltas (M412-M426)

Grammar delta proposals for all 15 OVERLENIENT gaps. Each is minimal, LL(*)-safe,
and patch-ready. These must be reviewed by ANTLR and COBOL expert agents before
implementation.

---

## M412 -- ACCEPT/DISPLAY Invalid Clauses

**Problem:** Grammar currently allows stray clauses after ACCEPT/DISPLAY.

**Required behavior:** Only clauses defined in ISO 85 are permitted.

**Grammar delta:**
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

**Rationale:** Removes permissive "tail" productions that accepted arbitrary tokens.

**LL(*) safety:** No new ambiguity; reduces alternatives.

---

## M413 -- IF/ELSE Malformed Conditions

**Problem:** Grammar accepts IF THEN, IF ELSE, and multiple ELSE branches.

**Required behavior:**
- Condition required
- At most one ELSE
- THEN is implicit (COBOL-85)

**Grammar delta:**
```diff
- if-statement
-     : IF condition? statement* (ELSE statement*)* END-IF
+ if-statement
+     : IF condition statement* (ELSE statement*)? END-IF
+     ;
```

**Rationale:** Removes optional condition and repeated ELSE.

**LL(*) safety:** Single ELSE alternative is unambiguous.

---

## M414 -- EVALUATE Malformed WHEN Phrases

**Problem:** Grammar allows WHEN without value, multiple WHEN OTHER, WHEN OTHER in
wrong position.

**Required behavior:**
- WHEN requires at least one condition
- WHEN OTHER appears once, last

**Grammar delta:**
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

**Rationale:** Enforces WHEN OTHER last and unique.

**LL(*) safety:** WHEN OTHER is a distinct keyword; no ambiguity.

---

## M415 -- PERFORM Malformed UNTIL/VARYING

**Problem:** Grammar allows PERFORM UNTIL without condition, VARYING without BY,
VARYING without UNTIL.

**Required behavior:**
- UNTIL requires condition
- VARYING requires BY and UNTIL

**Grammar delta:**
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

**Rationale:** Removes optionality that violates ISO.

**LL(*) safety:** All keywords are fixed; no ambiguity.

---

## M416 -- STRING Malformed INTO/DELIMITED BY

**Problem:** Grammar accepts STRING without INTO or DELIMITED BY.

**Required behavior:**
- INTO required
- DELIMITED BY required for each sending item

**Grammar delta:**
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

**Rationale:** ISO requires INTO and DELIMITED BY.

**LL(*) safety:** No new conflicts.

---

## M417 -- UNSTRING Malformed INTO/DELIMITED BY

**Problem:** Grammar allows UNSTRING without INTO or DELIMITED BY, extra
TALLYING/COUNT phrases.

**Required behavior:**
- INTO required
- DELIMITED BY required

**Grammar delta:**
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

**Rationale:** Matches STRING symmetry.

**LL(*) safety:** Straightforward.

---

## M418 -- INSPECT Malformed TALLYING/REPLACING

**Problem:** Grammar allows mixing TALLYING and REPLACING in invalid ways.

**Required behavior:**
- TALLYING and REPLACING are separate alternatives
- Each has required subclauses

**Grammar delta:**
```diff
- inspect-statement
-     : INSPECT identifier inspect-tail*
+ inspect-statement
+     : INSPECT identifier inspect-tallying
+     | INSPECT identifier inspect-replacing
+     ;
```

**Rationale:** Prevents mixed forms.

**LL(*) safety:** Keyword-driven; no ambiguity.

---

## M419 -- CALL Malformed USING/RETURNING

**Problem:** Grammar allows CALL without USING, invalid RETURNING, duplicate USING.

**Required behavior:**
- USING optional but well-formed if present
- RETURNING must be a data item

**Grammar delta:**
```diff
- call-statement
-     : CALL expression call-using* call-returning?
+ call-statement
+     : CALL expression (USING call-arg+)? (RETURNING identifier)?
+     ;
```

**Rationale:** Removes repeated USING and invalid RETURNING.

**LL(*) safety:** USING and RETURNING are distinct keywords.

---

## M420 -- SET Malformed TO/UP/DOWN

**Problem:** Grammar allows SET TO without identifier, SET UP/DOWN without index,
multiple directions.

**Required behavior:**
- SET identifier TO value
- SET index UP BY n
- SET index DOWN BY n

**Grammar delta:**
```diff
- set-statement
-     : SET set-target set-tail*
+ set-statement
+     : SET identifier TO expression
+     | SET identifier UP BY expression
+     | SET identifier DOWN BY expression
+     ;
```

**Rationale:** Enforces ISO forms.

**LL(*) safety:** Three disjoint keyword patterns.

---

## M421 -- SEARCH Malformed WHEN/AT END

**Problem:** Grammar allows SEARCH without WHEN, multiple AT END, invalid VARYING.

**Required behavior:**
- WHEN required
- AT END optional, unique
- VARYING only in SEARCH ALL

**Grammar delta:**
```diff
- search-statement
-     : SEARCH identifier search-body
+ search-statement
+     : SEARCH identifier WHEN condition statement* (AT END statement*)?
+     ;
```

**Rationale:** Enforces WHEN and single AT END.

**LL(*) safety:** WHEN and AT END are distinct.

---

## M422 -- SORT Malformed ON ASCENDING/DESCENDING

**Problem:** Grammar allows missing ON ASCENDING/DESCENDING, missing key.

**Required behavior:** ON ASCENDING/DESCENDING KEY data-name+

**Grammar delta:**
```diff
- sort-keys
-     : (ASCENDING|DESCENDING)? KEY identifier*
+ sort-keys
+     : (ASCENDING|DESCENDING) KEY identifier+
+     ;
```

**Rationale:** Enforces required key list.

**LL(*) safety:** ASCENDING/DESCENDING are exclusive.

---

## M423 -- MERGE Malformed ON ASCENDING/DESCENDING

**Problem:** Same issues as SORT.

**Grammar delta:**
```diff
- merge-keys
-     : (ASCENDING|DESCENDING)? KEY identifier*
+ merge-keys
+     : (ASCENDING|DESCENDING) KEY identifier+
+     ;
```

---

## M424 -- WRITE Malformed FROM/ADVANCING

**Problem:** Grammar allows WRITE without FROM, invalid ADVANCING, multiple FROM.

**Required behavior:**
- FROM optional but unique
- ADVANCING must follow ISO form

**Grammar delta:**
```diff
- write-statement
-     : WRITE identifier write-tail*
+ write-statement
+     : WRITE identifier (FROM identifier)? (ADVANCING expression)?
+     ;
```

---

## M425 -- READ Malformed INTO/AT END

**Problem:** Grammar allows READ without INTO, multiple AT END, invalid KEY clause.

**Required behavior:**
- INTO optional but unique
- AT END optional but unique
- KEY clause must follow ISO

**Grammar delta:**
```diff
- read-statement
-     : READ identifier read-tail*
+ read-statement
+     : READ identifier (INTO identifier)? (AT END statement*)?
+     ;
```

---

## M426 -- OPEN/CLOSE Malformed Modes

**Problem:** Grammar allows OPEN without mode, multiple modes, CLOSE with invalid
file list.

**Required behavior:**
- OPEN requires one mode per file
- CLOSE requires file list only

**Grammar delta:**
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

---

## Review Required

All grammar deltas must be examined by ANTLR expert and COBOL expert agents before
implementation, per established process. Each delta must be validated against:

1. The actual current grammar rules (not the simplified pseudocode above)
2. Existing test programs that may rely on the lenient forms
3. NIST test suite for regressions
