CobolSharp IL/Bytecode Generation Design
=======================================

High-level goals
----------------
- Transform the fully-resolved semantic model into a clean, deterministic IL/bytecode representation.
- Support COBOL-85 → COBOL-2023 features: OO, generics, JSON/XML, file I/O, PERFORM graphs, data descriptions, COPY/REPLACE, and dialect overlays.
- Produce IL that is:
  - Verifiable
  - Optimizable
  - Debuggable
  - Friendly to JIT/AOT backends
- Keep the design modular: each subsystem (data layout, control flow, OO, generics, I/O) is isolated and testable.

Overall pipeline
----------------
1. SemanticModel (from semantic analysis)
2. ILBuilder (root object)
3. TypeEmitter (records, classes, interfaces, typedefs)
4. MethodEmitter (procedures, methods, paragraphs)
5. DataLayoutEngine (01-level structures, PIC/USAGE, OCCURS)
6. ControlFlowEmitter (PERFORM graph → IL basic blocks)
7. ExpressionEmitter (arithmetic, boolean, relational)
8. StatementEmitter (READ/WRITE, CALL/INVOKE, JSON/XML, SORT/MERGE)
9. GenericInstantiator (generic types/methods → specialized IL)
10. ILVerifier (optional)
11. Backend (JIT/AOT/bytecode writer)

Core IL data structures
-----------------------
1. ILModule
   - Name: string
   - Types: List<ILType>
   - EntryPoint: ILMethod?
   - Metadata: version, dialect, debug info

2. ILType
   - Kind: enum (Class, Interface, Record, Array, GenericDefinition, GenericInstance)
   - Name: string
   - Fields: List<ILField>
   - Methods: List<ILMethod>
   - BaseType: ILType?
   - Interfaces: List<ILType>
   - GenericParameters: List<ILGenericParameter>
   - GenericArguments: List<ILType>

3. ILField
   - Name: string
   - Type: ILType
   - Offset: int (for record layout)
   - Attributes: flags (static, readonly, etc.)

4. ILMethod
   - Name: string
   - Parameters: List<ILParameter>
   - ReturnType: ILType
   - Attributes: flags (static, virtual, override, final)
   - GenericParameters: List<ILGenericParameter>
   - Body: ILBasicBlock graph

5. ILBasicBlock
   - Label: string
   - Instructions: List<ILInstruction>
   - Successors: List<ILBasicBlock>

6. ILInstruction
   - OpCode: enum (Load, Store, Add, Call, Branch, Compare, NewObject, etc.)
   - Operand: object (symbol, constant, type, method, field, label)

7. ILGenericParameter
   - Name: string
   - Constraint: ILType?

8. ILParameter
   - Name: string
   - Type: ILType

Data layout (Data Division → IL)
--------------------------------
- Each 01-level group becomes an ILType (Record).
- Each elementary item becomes an ILField.
- PIC/USAGE determines:
  - Storage size
  - Encoding (binary, packed decimal, display)
  - Sign representation
- OCCURS:
  - Fixed OCCURS → IL array field
  - OCCURS DEPENDING ON → IL dynamic array with runtime bounds
- REDEFINES:
  - Multiple ILFields share the same offset
  - ILType tracks aliasing metadata
- RENAMES:
  - ILType stores a synthetic field representing the renamed range

Control flow (PERFORM graph → IL)
---------------------------------
- Semantic analysis produces a control-flow graph (CFG).
- IL generation maps each basic block to an ILBasicBlock.
- PERFORM:
  - PERFORM paragraph → call-like branch to block
  - PERFORM THRU → branch to range start, return at range end
  - PERFORM UNTIL → loop structure with conditional branch
  - PERFORM VARYING → loop with induction variable
- GO TO:
  - Direct branch to target block
- IF/EVALUATE:
  - Conditional branches
  - EVALUATE becomes a decision tree or jump table
- EXIT PERFORM/SECTION/PROGRAM:
  - Branch to appropriate exit block

Expression emission
-------------------
- Arithmetic:
  - ADD/SUBTRACT/MULTIPLY/DIVIDE → IL arithmetic ops
  - Size error → guarded block with overflow detection
- Boolean:
  - Relational ops → Compare + Branch
  - AND/OR/NOT → short-circuit logic
- String ops:
  - STRING → runtime helper calls
  - UNSTRING → runtime helper calls
- Numeric conversions:
  - PIC/USAGE conversions inserted automatically

Statement emission
------------------
1. MOVE
   - Type-aware assignment
   - PIC/USAGE conversion
   - CORRESPONDING → field-by-field assignment

2. READ/WRITE/REWRITE/START/DELETE FILE
   - Calls into runtime FileManager
   - AT END / INVALID KEY → conditional branches

3. CALL
   - Static call → direct IL call
   - Dynamic call → runtime dispatch
   - BY VALUE / BY REFERENCE / BY CONTENT → argument marshalling

4. INVOKE (OO)
   - Instance method → virtual call
   - Static method → direct call
   - SUPER → base call
   - RETURNING → store result

5. JSON/XML
   - Calls into runtime JsonParser/JsonGenerator
   - Calls into runtime XmlParser/XmlGenerator

6. SORT/MERGE
   - Calls into runtime SortEngine/MergeEngine
   - Input/output procedures → IL callbacks

7. STOP/GOBACK/EXIT
   - STOP RUN → return from entry point
   - GOBACK → return from current program/method
   - EXIT PERFORM → branch to loop exit

OO and generics emission
------------------------
1. Classes
   - ILType with fields and methods
   - Base class and interfaces encoded in metadata

2. Methods
   - ILMethod with ILBasicBlock graph
   - Virtual/override encoded in method table

3. Object creation
   - NEWOBJECT instruction
   - Constructor call

4. Generics
   - Generic definitions → ILType with generic parameters
   - Generic instantiation → ILType with concrete arguments
   - Method specialization → ILMethod with substituted types

Runtime integration
-------------------
CobolSharp runtime provides:
- FileManager (indexed/relative/sequential)
- SortEngine/MergeEngine
- JsonParser/JsonGenerator
- XmlParser/XmlGenerator
- String/UNSTRING helpers
- Packed decimal arithmetic
- Collating sequences
- Date/time functions
- Exception model

Debugging and metadata
----------------------
- IL includes:
  - Source line mappings (from preprocessor)
  - Symbol names
  - Paragraph/section labels
  - File/copybook origin info
- Enables:
  - Breakpoints
  - Step-through debugging
  - Variable inspection

Verification and optimization
-----------------------------
Optional ILVerifier:
- Type correctness
- Stack correctness
- Control-flow validity
- No unreachable blocks (optional)

Optimizations:
- Dead code elimination
- Constant folding
- Loop simplification
- Inline small paragraphs
- Remove redundant MOVE statements
- Strength reduction for arithmetic

Summary
-------
CobolSharp's IL/bytecode generation design:
- Converts the semantic model into a clean ILModule.
- Emits ILTypes for programs, classes, records, and generics.
- Emits ILMethods with full control-flow graphs.
- Handles PIC/USAGE, OCCURS, REDEFINES, RENAMES.
- Supports OO, generics, JSON/XML, file I/O, SORT/MERGE.
- Integrates with a runtime library for complex operations.
- Produces verifiable, optimizable IL suitable for JIT/AOT backends.
