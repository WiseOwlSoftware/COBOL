# Section 2B: Control Flow, Statements, and Expressions Coverage

Audit date: 2026-03-22

## 1. Control Flow

| Feature | Status | Where | Quality | Notes |
|---------|--------|-------|---------|-------|
| PERFORM (inline) | Implemented | BoundNodes:BoundPerformStatement (InlineStatements), Binder:LowerPerform, CilEmitter | Spec-true | InlineStatements list holds body; lowered to loop/branch IR |
| PERFORM (out-of-line) | Implemented | BoundNodes:BoundPerformStatement (Target), IR:IrPerform, Binder:LowerPerformSimple, CilEmitter:EmitPerform | Spec-true | Single-paragraph call via IrPerform |
| PERFORM THRU | Implemented | BoundNodes:BoundPerformStatement (ThruTarget), IR:IrPerformThru, Binder:LowerPerformSimple, CilEmitter:EmitPerformThru | Spec-true | Dynamic dispatch loop over paragraph index range |
| PERFORM VARYING | Implemented | BoundNodes:BoundPerformVarying (with Next for AFTER), Binder:LowerPerformVarying | Spec-true | Nested AFTER clauses supported via linked BoundPerformVarying.Next |
| PERFORM UNTIL | Implemented | BoundNodes:BoundPerformStatement (UntilCondition), Binder:LowerPerform | Spec-true | UNTIL condition checked at top of loop; TIMES also supported via TimesExpression |
| GO TO | Implemented | BoundNodes:BoundGoToStatement, Binder:LowerGoTo, ProcedureGraph | Spec-true | Lowers to IrReturnConst with paragraph index |
| GO TO DEPENDING ON | Implemented | BoundNodes:BoundGoToStatement (DependingOn, multiple Targets), IR:IrGoToDepending, CilEmitter:EmitGoToDepending | Spec-true | Switch-based dispatch on selector value |
| ALTER | Implemented | BoundNodes:`BoundAlterStatement`/`BoundAlterEntry`; Binder:`LowerAlter`+alter slots; IR:`IrAlter`/`IrReturnAlterable`; CilEmitter:alter table; CBL3601-3606 | Spec-true | Version-aware: error in COBOL-2002+ (CBL3601), warning+support in 85/Default (CBL3602). Runtime alter indirection table. Bare GO TO supported. Zero overhead for non-ALTER programs |
| Fall-through rules | Implemented | Binder.cs (pc = myIndex+1), ProcedureGraph (cross-section detection) | Spec-true | Paragraph methods return next index; cross-section fall-through emits CBL3002 |
| Section/paragraph entry/exit | Implemented | SemanticModel (SectionSymbol, section-paragraph mapping), BoundExitSectionStatement, BoundExitParagraphStatement, Binder:LowerExitSection/LowerExitParagraph | Spec-true | EXIT SECTION jumps to first paragraph after section; EXIT PARAGRAPH jumps to paragraph end block |

## 2. Statements

| Feature | Status | Where | Quality | Notes |
|---------|--------|-------|---------|-------|
| IF/ELSE/END-IF | Implemented | BoundNodes:BoundIfStatement, Binder (case BoundIfStatement), CilEmitter | Spec-true | ThenStatements + optional ElseStatements; lowered to conditional branch IR |
| EVALUATE/WHEN | Implemented | BoundNodes:BoundEvaluateStatement/BoundEvaluateWhen/BoundEvaluateValueCondition/BoundEvaluateRange, Binder, CilEmitter | Spec-true | EVALUATE TRUE, multi-subject, value THRU ranges, WHEN OTHER all supported |
| MOVE (elementary) | Implemented | BoundNodes:BoundMoveStatement, Binder:LowerMove, IR:IrMove/IrMoveStringToField/IrMoveFigurative/IrMoveAllLiteral/IrMoveFieldToField, CilEmitter | Spec-true | Full PIC-aware move with category-based dispatch (numeric, alphanumeric, edited) |
| MOVE (group) | Implemented | Binder:LowerMove, IR:IrMoveFieldToField | Spec-true | Group moves are byte-level copies via IrMoveFieldToField |
| MOVE (CORRESPONDING) | Implemented | BoundNodes:BoundCorrespondingStatement, CorrespondingMatcher, Binder:LowerCorresponding | Spec-true | Pairs computed at bind time; per-pair PIC-aware move |
| ADD | Implemented | BoundNodes:BoundArithmeticStatement (ArithmeticKind.Add), Binder, IR:IrComputeIntoAccumulator/IrMoveAccumulatedToTarget/IrComputeStore, CilEmitter | Spec-true | Unified arithmetic; supports GIVING, ROUNDED, ON SIZE ERROR |
| SUBTRACT | Implemented | BoundNodes:BoundArithmeticStatement (ArithmeticKind.Subtract), Binder | Spec-true | Same unified model as ADD; CORRESPONDING variant via BoundCorrespondingStatement |
| MULTIPLY | Implemented | BoundNodes:BoundArithmeticStatement (ArithmeticKind.Multiply), Binder | Spec-true | BY and GIVING forms; ROUNDED supported |
| DIVIDE | Implemented | BoundNodes:BoundArithmeticStatement (ArithmeticKind.Divide), Binder | Spec-true | INTO/BY/GIVING/REMAINDER; known gap: subscripted operands in DIVIDE GIVING (NC121M) |
| COMPUTE | Implemented | BoundNodes:BoundArithmeticStatement (ArithmeticKind.Compute), IR:IrComputeStore, Binder, CilEmitter | Spec-true | Full arithmetic expression evaluation; ROUNDED and SIZE ERROR supported |
| STRING | Implemented | BoundNodes:BoundStringStatement/BoundStringSending, IR:IrStringStatement/IrStringSending, Binder, CilEmitter | Spec-true | DELIMITED BY SIZE/value, INTO, POINTER, ON OVERFLOW all supported |
| UNSTRING | Implemented | BoundNodes:BoundUnstringStatement/BoundUnstringInto, IR:IrUnstringStatement/IrUnstringInto, Binder, CilEmitter | Spec-true | DELIMITED BY ALL, INTO with COUNT IN/DELIMITER IN, POINTER, TALLYING, ON OVERFLOW |
| INSPECT TALLYING | Implemented | BoundNodes:BoundInspectTallyingItem, IR:IrInspectTally, Binder, CilEmitter | Spec-true | ALL/LEADING/CHARACTERS with BEFORE/AFTER INITIAL regions |
| INSPECT REPLACING | Implemented | BoundNodes:BoundInspectReplacingItem, IR:IrInspectReplace, Binder, CilEmitter | Spec-true | ALL/FIRST/LEADING/CHARACTERS with BEFORE/AFTER INITIAL regions |
| INSPECT CONVERTING | Implemented | BoundNodes:BoundInspectConverting, IR:IrInspectConvert, Binder, CilEmitter | Spec-true | FROM/TO character sets with BEFORE/AFTER INITIAL regions |
| ACCEPT | Implemented | BoundNodes:BoundAcceptStatement (AcceptSourceKind), IR:IrAccept, Binder, CilEmitter | Spec-true | AcceptSourceKind discriminates DATE/TIME/DAY/etc. |
| DISPLAY | Implemented | BoundNodes:BoundDisplayStatement, IR:IrPicDisplay/DisplayOperand, Binder, CilEmitter | Spec-true | Multiple operands; PIC-aware formatting |
| EXIT (plain) | Implemented | BoundNodes:BoundExitStatement, Binder (no-op) | Spec-true | No-op per COBOL-85 spec |
| EXIT PROGRAM | Partially | BoundNodes:BoundExitStatement (no PROGRAM distinction), Binder (no-op) | Likely incorrect | Treated as plain EXIT (no-op); should terminate program execution in called programs |
| EXIT PARAGRAPH | Implemented | BoundNodes:BoundExitParagraphStatement, Binder:LowerExitParagraph | Spec-true | Jumps to paragraph end block |
| EXIT SECTION | Implemented | BoundNodes:BoundExitSectionStatement, Binder:LowerExitSection | Spec-true | Returns index of first paragraph after current section |
| STOP RUN | Implemented | BoundNodes:BoundStopStatement, Binder (IrReturnConst(-1)), CilEmitter | Spec-true | Returns -1 to exit main dispatch loop |
| GOBACK | Implemented | BoundTreeBuilder (mapped to BoundStopStatement), Binder | Spec-true | Desugared to STOP RUN at bind time (line 226); correct for main programs |
| CONTINUE | Implemented | BoundTreeBuilder (mapped to BoundExitStatement, no-op) | Spec-true | Correctly treated as no-op (line 260) |
| INITIALIZE | Implemented | BoundNodes:BoundInitializeStatement/BoundInitializeCategoryReplacement, Binder, CilEmitter | Spec-true | Category-based REPLACING supported; default initialization by category |

## 3. Expressions

| Feature | Status | Where | Quality | Notes |
|---------|--------|-------|---------|-------|
| Arithmetic expressions | Implemented | BoundNodes:BoundBinaryExpression (Add/Subtract/Multiply/Divide/Power/Remainder), BoundTreeBuilder, Binder, CilEmitter | Spec-true | Full operator precedence; parenthesized subexpressions; exponentiation |
| Relational conditions | Implemented | BoundNodes:BoundBinaryExpression (Equal/NotEqual/Less/Greater/etc.), BoundTreeBuilder:BindComparison, Binder, CilEmitter | Spec-true | IS NOT variants; PIC-aware comparison (numeric vs alphanumeric) |
| Class conditions | Implemented | BoundNodes:BoundClassConditionExpression/ClassConditionKind, IR:IrClassCondition, BoundTreeBuilder:BindComparison, CilEmitter:EmitClassCondition | Spec-true | NUMERIC, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER; IS NOT supported |
| Sign conditions | Implemented | BoundNodes:`BoundSignConditionExpression`/`SignConditionKind`; Binder:`LowerSignCondition` (rewrite as comparison vs zero) | Spec-true | IS [NOT] POSITIVE/NEGATIVE/ZERO; lowered as subject > 0, subject < 0, subject = 0 with optional NOT inversion |
| Condition-name conditions (88-level) | Implemented | BoundNodes:BoundConditionNameExpression, ConditionSymbol, Binder:LowerConditionName, CilEmitter | Spec-true | Expands to parent = value1 OR parent = value2; SET TO TRUE supported. Known gap: VALUE THRU in level-88 |
| Combined conditions (AND/OR) | Implemented | BoundTreeBuilder:BindLogicalOr/BindLogicalAnd, BoundNodes:BoundBinaryExpression (And/Or) | Spec-true | Short-circuit evaluation via IR branch chains |
| Negated conditions (NOT) | Implemented | BoundTreeBuilder:`BindUnaryLogical` dispatches all primaryCondition alternatives; wraps in BoundBinaryOperatorKind.Not | Spec-true | General logical NOT: negates comparisons, sign conditions, class conditions, condition-names, parenthesized conditions |
