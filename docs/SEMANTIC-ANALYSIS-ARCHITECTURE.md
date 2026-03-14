CobolSharp Semantic Analysis Architecture
========================================

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
- Designed so IL/bytecode generation and IDE/LSP features operate on a clean, well-typed, fully-resolved semantic graph rather than the raw parse tree.
