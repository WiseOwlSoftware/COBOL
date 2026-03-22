# COBOL-85 Feature Coverage Audit: Divisions, Data Description, Numeric Behavior

Audited: 2026-03-22 | Branch: nist-phase-d

## 1. Divisions / Sections

| Feature | Status | Where | Quality | Notes |
|---|---|---|---|---|
| IDENTIFICATION DIVISION | Implemented | Parser: `CobolParserCore.g4` (identificationDivision); Compilation.cs:`ExtractProgramId` | Spec-true | PROGRAM-ID extracted; other paragraphs parsed but not semantically used |
| ENVIRONMENT DIVISION | Implemented | Parser: `CobolParserCore.g4` (environmentDivision); SemanticBuilder.cs | Spec-true | Container recognized; delegates to CONFIGURATION and INPUT-OUTPUT |
| CONFIGURATION SECTION | Implemented | Parser: `CobolParserCore.g4` (configurationSection); SemanticBuilder.cs:`VisitSpecialNamesParagraph` | Spec-true | SOURCE-COMPUTER, OBJECT-COMPUTER parsed; SPECIAL-NAMES: CURRENCY SIGN, DECIMAL-POINT IS COMMA, implementor switches all handled |
| INPUT-OUTPUT SECTION | Implemented | Parser: `CobolParserCore.g4` (inputOutputSection); SemanticBuilder.cs (lines 213-265) | Spec-true | FILE-CONTROL with SELECT/ASSIGN/ORGANIZATION/ACCESS/STATUS/KEY fully bound to FileSymbol |
| DATA DIVISION | Implemented | Parser: `CobolParserCore.g4` (dataDivision) | Spec-true | Container for all data sections |
| FILE SECTION | Implemented | SemanticBuilder.cs:`VisitFileSection`, FD entries linked to FileSymbol | Spec-true | FD records, record descriptors, SELECT/FD consistency validated (CBL0601) |
| WORKING-STORAGE SECTION | Implemented | SemanticBuilder.cs:`VisitWorkingStorageSection`; StorageLayoutComputer.cs | Spec-true | Full layout computation with REDEFINES family tracking |
| LINKAGE SECTION | Implemented | SemanticBuilder.cs:`VisitLinkageSection`; DataSymbol.cs (Area=LinkageSection) | Spec-true | Parsed, symbols created; USING/RETURNING bound |
| LOCAL-STORAGE SECTION | Implemented | SemanticBuilder.cs:`VisitLocalStorageSection` | Spec-true | Parsed and symbols created with LocalStorage area kind |
| PROCEDURE DIVISION | Implemented | SemanticBuilder.cs:`VisitProcedureDivision`; BoundTreeBuilder.cs; Binder.cs | Spec-true | USING/RETURNING clauses, sections, paragraphs, full statement binding |

## 2. Data Description

| Feature | Status | Where | Quality | Notes |
|---|---|---|---|---|
| PIC numeric (9, S, V, P) | Implemented | PicUsageResolver.cs; TypeSystem.cs:`PicLayout`; PicRuntime.cs | Spec-true | Full support: IntegerDigits, FractionDigits, LeadingPScaling, TrailingPScaling, IsSigned |
| PIC alphanumeric (X) | Implemented | PicUsageResolver.cs; CobolCategory.Alphanumeric | Spec-true | |
| PIC edited (Z, *, CR, DB, $, B, 0, /) | Implemented | PicDescriptor.cs:`EditingKind`; PicRuntime.cs:`FormatNumericEdited` | Spec-true | NumericEdited and AlphanumericEdited categories; edit pattern expansion at compile time |
| USAGE DISPLAY | Implemented | UsageKind.Display; FieldSizeCalculator.cs:`ComputeDisplaySize` | Spec-true | Default usage; SIGN IS SEPARATE adds 1 byte |
| USAGE COMP / BINARY | Implemented | UsageKind.Comp/Binary; FieldSizeCalculator.cs:`ComputeBinarySize`; PicRuntime.cs:`DecodeCompBinary`/`EncodeCompBinary` | Spec-true | 2/4/8-byte big-endian; overflow based on PIC digit count per spec |
| USAGE COMP-3 / PACKED-DECIMAL | Implemented | UsageKind.Comp3/PackedDecimal; PicRuntime.cs:`DecodeComp3`/`EncodeComp3` | Spec-true | BCD encoding with trailing sign nibble |
| USAGE COMP-5 | Not implemented | UsageKind enum has no Comp5 entry | N/A | No COMP-5 (native binary) support; COMP-1/COMP-2 (float) are defined in UsageKind |
| SIGN clause | Implemented | SemanticBuilder.cs (lines 365-372); DataSymbol.cs:`ExplicitSignStorage`; SignStorageKind enum | Spec-true | LEADING/TRAILING, SEPARATE CHARACTER; group-level propagation via `PropagateGroupSignClauses` |
| JUSTIFIED clause | Implemented | DataSymbol.cs:`IsJustifiedRight`; PicDescriptor.cs:`IsJustifiedRight`; PicRuntime.cs (line 557) | Spec-true | JUSTIFIED RIGHT: right-justify on MOVE, left-truncate when source > target |
| OCCURS fixed | Implemented | DataSymbol.cs:`OccursInfo`; SemanticBuilder.cs; StorageLayoutComputer.cs | Spec-true | MaxOccurs, INDEXED BY, subscript resolution |
| OCCURS DEPENDING ON | Implemented | OccursInfo.cs:`DependingOnName`/`DependingOnSymbol`; MinOccurs/MaxOccurs | Spec-true | Variable-length tables with resolved DEPENDING ON symbol |
| OCCURS ASCENDING/DESCENDING KEY | Partially | OccursInfo.cs: AscendingKeys/DescendingKeys parsed and stored | Unknown | Keys parsed but not used for SEARCH ALL validation; known gap (NC233A, NC237A, NC238A, NC247A) |
| REDEFINES | Implemented | SemanticBuilder.cs:`ResolveRedefines`; StorageLayoutComputer.cs:`RedefinesFamily`; SymbolValidator.cs | Spec-true | Deferred resolution, family tracking for max extent, level-number validation |
| RENAMES (level 66) | Partially | DataSymbol.cs:`RenamesInfo`; SemanticBuilder.cs (line 602-606) | Unknown | Parsed and symbol created with FromName/ThruName; deferred resolution exists; NOT lowered to IR or emitted in codegen |
| VALUE clause | Implemented | DataSymbol.cs:`InitialValue`/`FigurativeInit`; DataItemClassifier.cs | Spec-true | Literal and figurative constant values; validation on group items |
| 88-level condition names | Implemented | ConditionSymbol (ProgramSymbol.cs); SemanticBuilder.cs (line 438); BoundConditionNameExpression; BoundTreeBuilder.cs | Spec-true | VALUE ranges with THRU; expands to parent=value1 OR parent=value2; single values and ranges both supported |
| VALUE THRU in level-88 | Partially | Grammar: valueRange rule; ConditionValueRange record | Unknown | Known grammar gap for some forms (NC201A, NC250A, NC252A per CLAUDE.md) |
| Alignment / storage semantics | Implemented | FieldSizeCalculator.cs; StorageLayoutComputer.cs; RecordLayoutBuilder.cs | Spec-true | Byte-level layout; DISPLAY/COMP/COMP-3 sizing; REDEFINES overlay; no word-alignment (correct for COBOL-85) |

## 3. Numeric Behavior

| Feature | Status | Where | Quality | Notes |
|---|---|---|---|---|
| DISPLAY numeric encode/decode | Implemented | PicRuntime.cs:`DecodeDisplay`/`EncodeDisplay` | Spec-true | Overpunch sign (leading/trailing), separate sign character |
| COMP/BINARY arithmetic | Implemented | PicRuntime.cs:`DecodeCompBinary`/`EncodeCompBinary` | Spec-true | 2/4/8-byte big-endian; decimal scaling via FractionDigits |
| COMP-3 packed decimal | Implemented | PicRuntime.cs:`DecodeComp3`/`EncodeComp3` | Spec-true | BCD with trailing sign nibble (0xC positive, 0xD negative) |
| COMP-5 native binary | Not implemented | No UsageKind.Comp5 | N/A | Extension not supported |
| ROUNDED phrase | Implemented | BoundArithmeticTarget.IsRounded; Binder.cs (roundingMode parameter); PicRuntime.cs:`ApplyScalingAndRounding` | Spec-true | MidpointRounding.AwayFromZero (roundingMode=1); truncation when roundingMode=0 |
| Truncation | Implemented | PicRuntime.cs:`ApplyScalingAndRounding` (decimal.Truncate path); PicRuntime.cs (line 560: alpha left-truncate for JUSTIFIED) | Spec-true | Numeric: truncate to PIC digit capacity; alphanumeric: right-truncate (or left-truncate for JUSTIFIED RIGHT) |
| Overflow behavior | Implemented | PicRuntime.cs:`WouldOverflow`; ArithmeticStatus.SizeError | Spec-true | Checks PIC digit count for COMP (not binary capacity); divide-by-zero sets SizeError |
| SIZE ERROR handling | Implemented | BoundSizeErrorClause (BoundNodes.cs); BoundTreeBuilder.cs:`BindSizeErrorClause`; Binder.cs:`LowerSizeError`; CilEmitter.cs:`IrLoadSizeError` | Spec-true | ON SIZE ERROR / NOT ON SIZE ERROR: full bind/lower/emit pipeline; branch on ArithmeticStatus.SizeError |
