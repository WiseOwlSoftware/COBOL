# Section 4a: Validator and Diagnostic Gaps Analysis

## 1. Existing Validators

### BoundTreeValidator (static, post-binding)
**File**: `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeValidator.cs`
Runs after BoundTreeBuilder and ProcedureGraph, before IR lowering. Walks every
statement recursively and enforces expression-type and file-organization rules.

| Method | Checks | Diagnostics |
|---|---|---|
| `ValidatePerform` | THRU ordering, TIMES integer, UNTIL boolean, VARYING chain types | CBL2302-2308 |
| `ValidateIf` | Condition must be boolean | CBL2401 |
| `ValidateEvaluate` | Subject/WHEN type compat, missing WHEN OTHER, TRUE WHEN boolean | CBL2501-2503 |
| `ValidateRead` | NEXT on RANDOM, KEY on non-indexed, KEY not record key | CBL1701-1703 |
| `ValidateOpen` | EXTEND only on sequential | CBL0701 |
| `ValidateWrite` | FROM source compat with record | CBL1801 |
| `ValidateRewrite` | Not allowed on sequential, FROM compat | CBL1901-1902 |
| `ValidateDelete` | Not allowed on sequential | CBL2001 |
| `ValidateStart` | Not allowed on sequential, KEY operand check | CBL1601, CBL1603 |
| `ValidateReturn` | Always errors (no sort/merge support) | CBL2101 |
| `ValidateCall` | Dynamic CALL warning | CBL3310 |

### SymbolValidator (static, pre-binding)
**File**: `src/CobolSharp.Compiler/Semantics/SymbolValidator.cs`
Three passes over the semantic model after scope tree is populated.

| Pass | Checks | Diagnostics |
|---|---|---|
| `ValidateScopeRejections` | Duplicate data/condition/section/paragraph/file names, cross-type collisions | CBL3101-3104, CBL3107 |
| `ValidateRedefines` | Level-number match, no REDEFINES of 66/88 | CBL3112-3113 |
| `ValidateLinkageSection` | No VALUE on linkage items, no REDEFINES on 01-level linkage | CBL3110-3111 |

### DataItemClassifier (static, pre-binding)
**File**: `src/CobolSharp.Compiler/Semantics/DataItemClassifier.cs`
Validates data item consistency after type resolution and storage layout.

| Method | Checks | Diagnostics |
|---|---|---|
| `ValidateOccurs` | OCCURS not on 01/77, key subordination, key not group | CBL0801, CBL1103-1104 |
| `ValidateBlankWhenZero` | Only on numeric DISPLAY items | CBL0802 |
| `ValidateJustified` | Only on alphanumeric elementary items | CBL0803 |
| `ValidateValueClause` | VALUE clause compatibility | CBL1001-1004 |

### ParagraphValidator (static, pre-binding)
**File**: `src/CobolSharp.Compiler/Semantics/ParagraphValidator.cs`
Detects "phantom paragraphs" whose names match COBOL keywords (ADVANCING, GIVING,
ROUNDED, etc.), indicating grammar parse failures. Emits ad-hoc "SEM" warnings (no CBL code).

### FileStatusValidator (static, pre-binding)
**File**: `src/CobolSharp.Compiler/Semantics/FileStatusValidator.cs`
Validates FILE STATUS data-name: exists, alphanumeric >= 2 chars, not group, not
REDEFINES/RENAMES. Diagnostics: CBL3201-3204.

### ProcedureGraph (static, post-binding)
**File**: `src/CobolSharp.Compiler/Semantics/ProcedureGraph.cs`
Flow analysis on bound program: unreachable paragraphs, cross-section fall-through,
PERFORM cycles. Diagnostics: CBL3001-3004.

### ReportWriterValidator (stub)
**File**: `src/CobolSharp.Compiler/Semantics/ReportWriterValidator.cs`
Empty stub. Descriptors CBL3401-3406 defined but not emitted.

## 2. Diagnostic Catalog

### Data Division (CBL08xx-11xx)
| Code | Sev | Meaning |
|---|---|---|
| CBL0801 | Error | OCCURS not allowed on level 01/77 |
| CBL0802 | Error | BLANK WHEN ZERO only on numeric DISPLAY |
| CBL0803 | Error | JUSTIFIED only on alphanumeric elementary |
| CBL0901-0905 | Error | MOVE legality (category, CORR groups, figurative, condition target) |
| CBL1001-1004 | Warn/Err | VALUE clause (group, incompatible, extra, condition value) |
| CBL1101-1105 | Err/Warn | OCCURS / DEPENDING ON / SEARCH on non-table |
| CBL1103-1104 | Error | OCCURS key subordination and group check |

### Statement Validation (CBL12xx-26xx)
| Code | Sev | Meaning |
|---|---|---|
| CBL1201-1205 | Error | SEARCH / SEARCH ALL operand rules |
| CBL1301-1304 | Error | STRING operand types |
| CBL1401-1406 | Error | UNSTRING operand types |
| CBL1501-1503 | Error | INSPECT operand types |
| CBL1601-1605 | Error | START organization / KEY rules |
| CBL1701-1704 | Error | READ access/KEY/INTO rules |
| CBL1801-1803 | Error | WRITE FROM / ADVANCING rules |
| CBL1901-1902 | Error | REWRITE organization / FROM |
| CBL2001 | Error | DELETE organization |
| CBL2101-2102 | Error | RETURN sort/merge / INTO |
| CBL2201 | Error | RELEASE sort/merge record |
| CBL2301-2308 | Err/Warn | PERFORM (target, THRU, TIMES, UNTIL, VARYING) |
| CBL2401-2402 | Error | IF condition / comparison operands |
| CBL2501-2503 | Err/Warn | EVALUATE subject/WHEN types, missing WHEN OTHER |
| CBL2601-2605 | Error | Arithmetic operand/result numeric, ROUNDED, SIZE ERROR, DIVIDE remainder |

### Flow and Symbols (CBL30xx-35xx)
| Code | Sev | Meaning |
|---|---|---|
| CBL3001-3004 | Warning | Unreachable paragraph, cross-section fall-through, PERFORM cycles |
| CBL3101-3114 | Error | Duplicate names, GLOBAL, shadowing, USING/RETURNING linkage, REDEFINES rules |
| CBL3201-3206 | Err/Warn | FILE STATUS data-name validation |
| CBL3301-3305, 3310 | Err/Warn | CALL argument count/mode/type, RETURNING, dynamic CALL |
| CBL3401-3406 | Err/Warn | Report Writer (defined but never emitted) |
| CBL3501-3502 | Err/Warn | Strict COBOL-85 mode |

### Infrastructure (CBL06xx-07xx)
| Code | Sev | Meaning |
|---|---|---|
| CBL0601 | Warning | FD without matching SELECT |
| CBL0701 | Error | OPEN EXTEND on non-sequential file |

## 3. Missing Validation Gaps

### 3.1 Symbol / Binding Gaps

**Gap: USING parameter not in LINKAGE SECTION**
- COBOL-85 rule: PROCEDURE DIVISION USING parameters must reference 01/77 items in LINKAGE SECTION.
- Descriptor CBL3108 defined, never emitted.
- Owner: `SymbolValidator.ValidateLinkageSection` or new pass in `SemanticBuilder`.
- Suggested ID: CBL3108 (already defined).

**Gap: RETURNING item not in LINKAGE SECTION**
- COBOL-85 rule: RETURNING data-name must be in LINKAGE SECTION.
- Descriptor CBL3109 defined, never emitted.
- Owner: `SymbolValidator.ValidateLinkageSection`.
- Suggested ID: CBL3109 (already defined).

**Gap: REDEFINES target subordinate to OCCURS**
- COBOL-85 rule (13.16.42.2): REDEFINES subject must not be subordinate to an item with OCCURS.
- Descriptor CBL3114 defined, never emitted.
- Owner: `SymbolValidator.ValidateRedefines`.
- Suggested ID: CBL3114 (already defined).

**Gap: GLOBAL not validated**
- Descriptors CBL3105 (GLOBAL not allowed) and CBL3106 (LOCAL shadows GLOBAL) defined, never emitted.
- Owner: `SymbolValidator` (new pass).
- Suggested IDs: CBL3105, CBL3106 (already defined).

**Gap: Qualified name ambiguity not detected**
- COBOL-85 rule: An unqualified reference must resolve to exactly one item; otherwise the compiler must error.
- Currently, the first match wins silently in `ReferenceResolver`.
- Owner: `ReferenceResolver`.
- Suggested ID: CBL3115 ("Ambiguous reference '{0}' matches multiple data items").

### 3.2 Control-Flow Gaps

**Gap: GO TO without target in non-ALTER context** — **RESOLVED**
- Bare GO TO now handled in `BoundTreeBuilder.BindGoTo`: emits CBL3605 (error in COBOL-2002+)
  or CBL3606 (warning in 85/Default). Binder maps bare GO TO to alter slots when ALTER-referenced,
  or emits `IrReturnConst(-1)` (STOP) when not.

**Gap: EXIT SECTION / EXIT PARAGRAPH outside scope**
- COBOL-85 rule: EXIT SECTION only valid inside a section; EXIT PARAGRAPH only inside a paragraph.
- Currently no check; the binder sets `_sectionExitReturnIndex` to null and silently falls through.
- Owner: `BoundTreeValidator`.
- Suggested ID: CBL3006 ("EXIT SECTION used outside a section").

**Gap: PERFORM THRU crossing section boundaries**
- COBOL-85 rule (6.16.4): PERFORM THRU range must not cross section boundaries.
- CBL2302 checks ordering but not section containment.
- Owner: `BoundTreeValidator.ValidatePerform`.
- Suggested ID: CBL2309 ("PERFORM THRU crosses section boundary").

### 3.3 File I/O Gaps

**Gap: I/O on unopened file (static check)**
- COBOL-85 rule: READ/WRITE/REWRITE/DELETE/START require the file to be open in the correct mode.
- No open-state tracking exists in the compiler.
- Owner: New validator pass, or extend `BoundTreeValidator` with flow-sensitive open-state tracking.
- Suggested ID: CBL0702 ("File '{0}' may not be open for {1}").

**Gap: WRITE ADVANCING validation**
- Descriptors CBL1802 (ADVANCING value numeric) and CBL1803 (ADVANCING item integer) are defined but never emitted.
- COBOL-85 rule: ADVANCING operand must be an integer identifier or integer literal.
- Owner: `BoundTreeValidator.ValidateWrite`.
- Suggested IDs: CBL1802, CBL1803 (already defined).

**Gap: READ INTO target validation**
- Descriptor CBL1704 (READ INTO target must be alphanumeric or group) defined, never emitted.
- Owner: `BoundTreeValidator.ValidateRead`.
- Suggested ID: CBL1704 (already defined).

**Gap: START KEY comparison type check**
- Descriptors CBL1602 (KEY must be comparison expression) and CBL1604 (KEY operands incompatible) defined, never emitted.
- Owner: `BoundTreeValidator.ValidateStart`.
- Suggested IDs: CBL1602, CBL1604 (already defined).

**Gap: FILE STATUS not checked between I/O ops**
- Descriptor CBL3206 defined but never emitted. Would require flow-sensitive analysis.
- Owner: New pass or extend `ProcedureGraph`.
- Suggested ID: CBL3206 (already defined).

**Gap: Multiple FILE STATUS on same file**
- Descriptor CBL3205 defined, never emitted.
- Owner: `FileStatusValidator.Validate`.
- Suggested ID: CBL3205 (already defined).

### 3.4 CALL / USING / RETURNING Gaps

**Gap: Argument count mismatch**
- COBOL-85 rule: Number of USING arguments in CALL must match the target program's USING count.
- Descriptor CBL3301 defined, never emitted. Cannot validate until inter-program linkage is implemented.
- Owner: `BoundTreeValidator.ValidateCall` (when target program metadata is available).
- Suggested ID: CBL3301 (already defined).

**Gap: Argument mode validation (BY REFERENCE / BY CONTENT / BY VALUE)**
- COBOL-85 rule: BY REFERENCE requires an identifier (not a literal). BY VALUE requires elementary numeric or pointer.
- Descriptor CBL3302 defined, never emitted.
- Owner: `BoundTreeValidator.ValidateCall`.
- Suggested ID: CBL3302 (already defined). Can be checked now without inter-program linkage: verify that BY REFERENCE arguments are identifiers, not literals.

**Gap: Argument type compatibility**
- Descriptor CBL3303 defined, never emitted. Requires target program parameter metadata.
- Owner: `BoundTreeValidator.ValidateCall`.
- Suggested ID: CBL3303 (already defined).

**Gap: CALL RETURNING type check**
- COBOL-85 rule: RETURNING identifier must be compatible with target's RETURNING type.
- Descriptors CBL3304 and CBL3305 defined, never emitted.
- Owner: `BoundTreeValidator.ValidateCall`.
- Suggested IDs: CBL3304, CBL3305 (already defined).

**Gap: BY REFERENCE literal not allowed**
- COBOL-85 rule (6.8.3.2): BY REFERENCE requires an identifier, not a literal or expression.
- Can be validated now from the bound tree -- check that each BY REFERENCE argument is a `BoundIdentifierExpression`.
- Owner: `BoundTreeValidator.ValidateCall`.
- Suggested ID: CBL3302 (reuse existing).

## 4. Summary

**Defined descriptors never emitted**: CBL1602, CBL1604, CBL1704, CBL1802, CBL1803, CBL2201, CBL2402, CBL3105, CBL3106, CBL3108, CBL3109, CBL3114, CBL3205, CBL3206, CBL3301-3305, CBL3401-3406, CBL3501-3502. Total: 24 dormant descriptors.

**Immediately actionable** (no new infrastructure needed):
1. CBL3302 -- BY REFERENCE argument must be identifier (check in `ValidateCall`)
2. CBL1704 -- READ INTO target type (check in `ValidateRead`)
3. CBL1802/1803 -- WRITE ADVANCING type (check in `ValidateWrite`)
4. CBL3108/3109 -- USING/RETURNING linkage validation (check in `SymbolValidator`)
5. CBL3114 -- REDEFINES subordinate to OCCURS (check in `SymbolValidator.ValidateRedefines`)
6. CBL1602/1604 -- START KEY expression/type (check in `ValidateStart`)

**Requires new infrastructure**:
- CBL0702 (open-state tracking) -- flow-sensitive file state analysis
- CBL3206 (FILE STATUS unchecked) -- flow-sensitive analysis
- CBL3301/3303/3304/3305 -- inter-program metadata for CALL validation
- CBL3401-3406 -- Report Writer codegen
