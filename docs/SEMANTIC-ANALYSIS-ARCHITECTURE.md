CobolSharp Semantic Analysis Architecture
========================================

> **STATUS BANNER (2026-06-07).** This is the **LIVE canonical** design reference for the
> CobolSharp semantic analyzer / symbol-table / type-system subsystem. **Implementation status:
> ~80–90% implemented** — the analyzer is real and in active use (see `src/CobolSharp.Compiler/Semantics/`:
> `SemanticBuilder`, `SymbolTable`, `Scope`, `ReferenceResolver`, `PicUsageResolver`, `TypeSystem`,
> `ArithmeticTypeSystem`, `ProcedureGraph`, `RecordClassification`, plus the `Bound/Binding/` binder
> decomposition and `FlowAnalysis/`). The OO, generics, and JSON/XML *passes* described below are
> partially design-only — OO grammar is done but its semantic/emit is pending; JSON/XML PARSE/GENERATE
> lowering is design-only (Phase C). Verify any specific pass against `src/` before stating it is complete.
>
> **Stack: .NET 10 / C# 14.** Backend is **CIL-only via Mono.Cecil** — there is **no custom VM / no
> bytecode interpreter** (any "bytecode" phrasing below is legacy wording for CIL emission; a Roslyn C#
> backend is a future additive Stage-5 option with Cecil as the oracle).
>
> **Already-decomposed (do NOT treat as god classes):** the bound-tree build is split across **9 binders**
> under `Bound/Binding/` (the M004 `BoundTreeBuilder`→binders refactor); IR is `IrExpression` (M001);
> CIL emission is 11 emitters (M003). Per project doctrine the **Binder produces bound nodes only, never
> IR — lowering turns bound nodes into IR.**
>
> **Data-model note:** the typed-native data model (char→`string`, numeric→`long`/`decimal`,
> groups→`record struct`, OCCURS→`T[]`, pointers→**`ManagedPointer`**) is gated behind `EnableTypedFields`
> (default OFF). The `RecordClassificationPass` classifier (`RecordClassification.cs`, wired into the Binder)
> decides per-record whether typed-native or the byte/StorageBlock fallback applies; the byte engine is being
> islanded. There is **one** pointer carrier, `ManagedPointer` (no 8-byte handle, no PointerRegistry).
>
> **Plan SSOT:** `docs/MASTER_PLAN.md`. Doctrine: `PROMPT.md`. Companion LIVE rule docs:
> `docs/CATEGORY-RULES.md` (data-category compatibility) and `docs/SCOPE-RULES.md` (name resolution / scoping).
> The per-feature *behavior* "constitution" lives in the separate doc
> `docs/CobolSharp COBOL Semantic Rules & Edge‑Case Behavior Specification.md`.

High-level goals
----------------
- Build a precise, navigable semantic model over the parsed COBOL AST.
- Support COBOL-85 → COBOL-2023: OO, generics, JSON/XML, files, COPY/REPLACE, dialect overlays.
- Enable downstream phases: flow analysis, optimization, IL/bytecode generation, and IDE/LSP features.
- Keep the design modular: each concern (symbols, types, flow, files, generics, OO) is a separate subsystem.

Overall pipeline
----------------
1. Preprocessor
   - Expands COPY/REPLACE and pseudo-text.
   - Produces a normalized source stream with origin tracking (file, line, column, copybook).
2. Parser
   - Produces a concrete AST (parse tree) using the ANTLR grammar we've built.
3. AST-to-Semantic Model Binder
   - Walks the parse tree and builds:
     - Symbol tables
     - Type graph
     - Data description tree
     - Procedure/method model
     - File model
4. Semantic Passes
   - Name resolution
   - Type checking
   - Data description validation (PIC/USAGE/OCCURS/REDEFINES)
   - Control-flow and PERFORM graph
   - OO and generics binding
   - File and I/O semantics
5. IR/IL Generation
   - Consumes the semantic model, not the raw AST.

Core data structures
--------------------
1. SemanticModel (root)
   - Programs: List<ProgramSymbol>
   - Classes: List<ClassSymbol>
   - Interfaces: List<InterfaceSymbol>
   - Copybooks: List<CopybookSymbol>
   - GlobalDiagnostics: List<Diagnostic>

2. Symbol hierarchy
   - Symbol (base)
     - Name: string
     - Kind: enum (Program, Class, Method, Data, File, Paragraph, Section, ConditionName, TypeParameter, Typedef, Copybook, etc.)
     - DeclaringNode: AST node reference
     - ContainingSymbol: Symbol?
     - Children: List<Symbol>
   - ProgramSymbol
     - Divisions: references to division-level models
     - EntryPoints: main, alternate entries
   - ClassSymbol / InterfaceSymbol
     - Methods: List<MethodSymbol>
     - Fields: List<DataSymbol>
     - BaseClass: ClassSymbol?
     - Interfaces: List<InterfaceSymbol>
     - GenericParameters: List<TypeParameterSymbol>
   - MethodSymbol
     - Parameters: List<ParameterSymbol>
     - ReturnType: TypeSymbol
     - IsStatic / IsOverride / IsFinal / Access modifiers
     - GenericParameters: List<TypeParameterSymbol>
   - DataSymbol
     - LevelNumber: int
     - Pic: PicDescriptor?
     - Usage: UsageKind
     - Occurs: OccursDescriptor?
     - Redefines: DataSymbol?
     - Renames: RenamesDescriptor?
     - ConditionValues: List<ConditionValue> (for 88-levels)
     - Type: TypeSymbol (resolved)
   - FileSymbol
     - Organization, AccessMode, RecordKey, AlternateKeys, FileStatus
     - AssociatedRecordSymbols
   - ParagraphSymbol / SectionSymbol
     - ContainingProgram
     - CalledBy: references from PERFORM/GO TO
   - TypeParameterSymbol
     - Constraint: TypeSymbol? (OF constraint)
   - TypedefSymbol
     - UnderlyingType: TypeSymbol
     - IsGeneric: bool
     - GenericParameters: List<TypeParameterSymbol>

3. Type system
   - TypeSymbol (base)
     - Name: string
     - Kind: enum (Intrinsic, Record, Class, Interface, Array, GenericDefinition, GenericInstance, Typedef, Unknown, Error)
   - IntrinsicTypeSymbol
     - Examples: Integer, Decimal, String, Boolean, Date, Time, Binary, Comp-3, etc.
   - RecordTypeSymbol
     - Backed by a DataSymbol tree (01-level structure).
   - ClassTypeSymbol / InterfaceTypeSymbol
     - Backed by ClassSymbol / InterfaceSymbol.
   - ArrayTypeSymbol
     - ElementType: TypeSymbol
     - Bounds: OccursDescriptor
   - GenericTypeDefinitionSymbol
     - GenericParameters: List<TypeParameterSymbol>
     - UnderlyingType: TypeSymbol (record/class/interface)
   - GenericTypeInstanceSymbol
     - Definition: GenericTypeDefinitionSymbol
     - TypeArguments: List<TypeSymbol>
   - TypedefTypeSymbol
     - AliasOf: TypeSymbol

4. Data description tree
   - For each program/class:
     - Root: synthetic DataRootSymbol
     - Children: DataSymbol representing 01/77/66/88 levels.
   - Each DataSymbol:
     - Parent: DataSymbol?
     - Children: List<DataSymbol>
     - LevelNumber: int
     - Pic/Usage/Occurs/Redefines/Value/Sign/Sync/Justified/BlankWhenZero
   - This tree is the canonical representation of COBOL's data division.

Semantic passes
---------------

Pass 0: Preprocessor mapping
----------------------------
- Build a mapping from expanded tokens back to original source locations (file, line, column, copybook).
- Attach this mapping to AST nodes for accurate diagnostics.
- Model COPY/REPLACE as CopybookSymbol entries with expansion metadata.

Pass 1: Symbol discovery
------------------------
- Walk the AST and create top-level symbols:
  - ProgramSymbol for each IDENTIFICATION DIVISION PROGRAM-ID.
  - ClassSymbol / InterfaceSymbol for each CLASS-ID / INTERFACE-ID.
  - CopybookSymbol for each COPY target (optional, if you want semantic info for copybooks).
- Inside each program/class:
  - Build SectionSymbol and ParagraphSymbol from Procedure Division.
  - Build FileSymbol from File Section.
  - Build DataSymbol tree from all data divisions (File, Working-Storage, Local-Storage, Linkage, Class-Data, Object-Data).
  - Build MethodSymbol from METHOD-ID declarations.
  - Build TypedefSymbol from TYPEDEF (including GENERIC).

Pass 2: Name resolution
-----------------------
- Build scoped symbol tables:
  - Program scope: files, data items, paragraphs, sections, typedefs.
  - Class scope: methods, fields, typedefs, nested types.
  - Method/procedure scope: parameters, local storage, inline declarations.
- Resolve:
  - Identifiers in expressions and statements to DataSymbol, FileSymbol, ParagraphSymbol, SectionSymbol, MethodSymbol, TypedefSymbol.
  - INVOKE targets to ClassSymbol, InterfaceSymbol, or DataSymbol (object reference).
  - CALL targets to ProgramSymbol or external entry descriptors.
- Handle shadowing and qualification:
  - Qualified names: A OF B, B OF C, etc.
  - Section/paragraph qualification: PARA-1 IN SECTION-1.

Pass 3: Type binding
--------------------
- For each DataSymbol:
  - Derive TypeSymbol from PIC/USAGE/TYPE/TYPEDEF/GENERIC.
  - For OCCURS, create ArrayTypeSymbol or repeated record type.
  - For REDEFINES, ensure compatible storage size and build aliasing relationships.
- For expressions:
  - Infer types of arithmetic expressions, relational expressions, boolean conditions.
  - Apply numeric promotion rules (COMP, COMP-3, DISPLAY, BINARY).
  - Validate MOVE, ADD, SUBTRACT, MULTIPLY, DIVIDE, STRING, UNSTRING type compatibility.
- For OO:
  - Bind class and interface types.
  - Bind object references (SELF, SUPER, NULL).
- For generics:
  - Bind generic type arguments to GenericTypeDefinitionSymbol.
  - Instantiate GenericTypeInstanceSymbol where needed.

Pass 4: Data description validation
-----------------------------------
- PIC/USAGE validation:
  - Ensure PIC is compatible with USAGE.
  - Validate sign, BLANK WHEN ZERO, JUSTIFIED, SYNCHRONIZED.
- OCCURS validation:
  - Check OCCURS with DEPENDING ON: DEPENDING item type and range.
  - Validate nested OCCURS and maximum table sizes.
- REDEFINES validation:
  - Ensure same parent group.
  - Validate storage size compatibility.
- RENAMES (66) validation:
  - Ensure THRU range is valid and contiguous.
- 88-level validation:
  - Ensure condition values are compatible with base item type.
  - Build ConditionName descriptors for fast evaluation.

Pass 5: Control-flow and PERFORM graph
--------------------------------------
- Build a control-flow graph (CFG) per program/method:
  - Nodes: basic blocks (sequences of statements).
  - Edges: control transfers (IF, EVALUATE, PERFORM, GO TO, EXIT, STOP, GOBACK).
- PERFORM analysis:
  - Map PERFORM targets to ParagraphSymbol/SectionSymbol.
  - Detect PERFORM THRU ranges and build subgraphs.
- GO TO analysis:
  - Map GO TO targets to ParagraphSymbol/SectionSymbol.
  - Optionally flag unstructured control flow for diagnostics.
- Exception/condition flow:
  - Model ON EXCEPTION, INVALID KEY, AT END, NOT ON EXCEPTION, NOT INVALID KEY, NOT AT END as guarded edges in the CFG.

Pass 6: File and I/O semantics
------------------------------
- For each FileSymbol:
  - Validate ORGANIZATION, ACCESS MODE, RECORD KEY, ALTERNATE KEY, FILE STATUS.
- For each I/O statement:
  - OPEN/CLOSE:
    - Ensure file is declared and mode is compatible.
  - READ/WRITE/REWRITE/DELETE/START/RETURN/RELEASE:
    - Validate file organization and access mode.
    - Validate record names and keys.
    - Validate AT END / INVALID KEY usage.
- For SORT/MERGE:
  - Validate USING/GIVING vs INPUT/OUTPUT PROCEDURE exclusivity.
  - Validate key definitions and collating sequence.

Pass 7: OO and method semantics
-------------------------------
- Class hierarchy:
  - Resolve base classes and interfaces.
  - Detect cycles and illegal inheritance.
- Method resolution:
  - For INVOKE:
    - Resolve target type (class/interface/object).
    - Resolve method by name and parameter types (including generics).
    - Apply override rules and access control.
- Visibility and access:
  - Enforce PUBLIC/PROTECTED/PRIVATE semantics (dialect-dependent).
- Static vs instance:
  - Validate STATIC methods and fields usage.
  - Ensure instance members are not accessed without an object reference.

Pass 8: Generics semantics
--------------------------
- Generic definitions:
  - Validate TYPEDEF GENERIC and generic methods:
    - Unique type parameter names.
    - Valid constraints (OF type).
- Generic instantiation:
  - For genericTypeSpecifier and INVOKE/CALL with type arguments:
    - Check arity (number of type arguments).
    - Check constraints (type arguments satisfy OF constraints).
  - Create or reuse GenericTypeInstanceSymbol.
- Substitution:
  - Substitute type parameters with concrete types in:
    - DataSymbol types
    - Method parameter and return types
    - Nested generic instances

Pass 9: JSON/XML semantics
--------------------------
- JSON PARSE/GENERATE:
  - Validate source/target types (string/binary for JSON text, group items for data).
  - Validate WITH DETAIL and SUPPRESS SPACES usage.
- XML PARSE/GENERATE:
  - Validate source/target types.
  - Validate PROCESSING PROCEDURE and COUNT IN usage.
- Attach schema-like metadata if you choose to model JSON/XML structures more deeply.

Diagnostics and reporting
-------------------------
- Each pass can emit diagnostics:
  - Severity: Info, Warning, Error.
  - Code: e.g., CS0001 (undefined symbol), CS0100 (type mismatch), etc.
  - Location: mapped back through preprocessor to original file/copybook.
- Diagnostics are attached to:
  - SemanticModel.GlobalDiagnostics
  - Individual Symbol or AST nodes (for IDE navigation).

Integration with IL/bytecode generation
---------------------------------------
- IL generation consumes:
  - Symbol graph (programs, classes, methods, data).
  - Type graph (TypeSymbol hierarchy).
  - Data description tree (for layout and marshalling).
  - CFG (for control flow and exception paths).
  - File model (for runtime I/O bindings).
  - Generics instantiation map (for specialized types/methods).
- The semantic model is the single source of truth; the raw AST is no longer needed for codegen.

Summary
-------
The semantic analysis architecture for CobolSharp is:

- Rooted in a SemanticModel that owns symbols, types, and diagnostics.
- Built in layered passes:
  - Symbol discovery
  - Name resolution
  - Type binding
  - Data description validation
  - Control-flow and PERFORM graph
  - File/I-O semantics
  - OO and generics semantics
  - JSON/XML semantics
- Driven by a preprocessor-aware pipeline that preserves accurate source locations.
- Designed so CIL generation and IDE/LSP features operate on a clean, well-typed, fully-resolved semantic graph rather than the raw parse tree.

============================================================
APPENDIX A — SYMBOL TABLE & ANALYZER DETAIL
============================================================

A.1 Symbol resolution order (name lookup)
-----------------------------------------
For an unqualified reference, resolution walks outward in this order (see `docs/SCOPE-RULES.md` for
the authoritative rules and `ReferenceResolver.cs` for the implementation):
1. Local paragraph scope
2. Enclosing section scope
3. Program scope (data, files, paragraphs/sections, typedefs)
4. Containing program scope (nested-program nesting)
5. COPY-introduced symbols (after preprocessor expansion)

A.2 Constant folding (early-evaluation pass)
--------------------------------------------
A constant-folding step runs during/after type binding to evaluate compile-time-known expressions:
- **Foldable:** numeric literals; arithmetic on literals; boolean expressions over literals;
  `LENGTH OF` a fixed-size item; FUNCTION calls whose arguments are all literals (the deterministic,
  side-effect-free subset only).
- **Non-foldable:** anything touching a data item, an OCCURS subscript, or JSON/XML.
- **Benefits:** smaller/faster emitted CIL, and early detection of compile-time numeric overflow
  (a `SIZE ERROR`-class diagnostic at compile time rather than runtime).

A.3 AST / bound-node annotations consumed by the backend
--------------------------------------------------------
Semantic analysis annotates the bound tree with: the resolved symbol, resolved type information,
any constant-folded value, control-flow metadata, and REDEFINES/OCCURS storage metadata.
Lowering + the CIL emitters then rely on: resolved types, resolved paragraph/section targets,
resolved CALL/INVOKE signatures, folded constants, and storage-layout metadata. (Per doctrine the
Binder emits bound nodes only; IR is produced by the lowering stage, not the Binder.)

A.4 Diagnostic-mapping detail
-----------------------------
Every diagnostic carries a source span mapped through three layers: the **original** source span,
the **expanded** (post-COPY/REPLACE) span, and the originating **copybook file path** — so that errors
in copybook-introduced code point at the copybook, not the expansion site.

A.5 Specific edge-case rulings (analyzer-enforced diagnostics)
--------------------------------------------------------------
These are the analyzer's hard rulings (the runtime/behavior side lives in the separate
"Semantic Rules & Edge-Case Behavior Specification"):
- **Duplicate paragraph names:** error, *unless* the duplicates are in different sections.
- **Paragraph name identical to a section name:** illegal.
- **REDEFINES of an OCCURS DEPENDING ON item:** allowed, but the logical length must be validated.
- **GO TO into the middle of an EVALUATE / IF block:** illegal — structured lowering requires that
  control transfers respect block boundaries. Likewise GO TO into/out of DECLARATIVES is illegal.
- **CALL with too many USING parameters:** error.
- **CALL with too few USING parameters:** warning (COBOL permits omitted trailing parameters).
- **Mixed NATIONAL / alphanumeric in one group operation:** illegal unless an explicit conversion is present.

A.6 Type-checking notes not already stated above
------------------------------------------------
- Mixed numeric USAGE in an arithmetic context promotes to decimal; pure-binary operand sets use
  binary arithmetic (`ArithmeticTypeSystem.cs`).
- Condition-name (88-level) references are boolean; in a numeric context a boolean materializes as 1/0
  and in an alphanumeric context as the configured TRUE/FALSE rendering.
- INVOKE binds against the resolved object/class/interface method signature including the RETURNING type;
  generic type arguments are checked for arity and OF-constraint satisfaction (OO/generics semantic
  binding is partially design-only — see the status banner).
