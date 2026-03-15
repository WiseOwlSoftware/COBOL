CobolSharp Internal Compiler API Reference Architecture (CIL‑Only)
==================================================================

Purpose
-------
Define the internal API surface of the CobolSharp compiler — the classes, interfaces, data structures, and contracts that form the backbone of the compilation pipeline.  
This document is for maintainers and advanced contributors who need to understand how compiler subsystems communicate.

This is NOT a public API.  
It is a stable internal contract across compiler subsystems.

Top‑level namespaces
--------------------
CobolSharp.Compiler  
  .Preprocessing  
  .Lexing  
  .Parsing  
  .Semantic  
  .IL  
  .Optimization  
  .Backend.CIL  
  .Diagnostics  
  .Metadata  
  .Utilities  

CobolSharp.Runtime  
CobolSharp.LSP  
CobolSharp.Tools  

Core compiler API layers
------------------------
The compiler exposes the following internal APIs:

1. Source API  
2. Preprocessing API  
3. Token API  
4. AST API  
5. Semantic Model API  
6. IL API  
7. Optimization API  
8. Backend API  
9. Diagnostics API  
10. Metadata API  
11. Utility API  

Each layer is described below.

1. Source API
-------------
Interfaces:
- ISourceFile  
- ISourceText  
- ISourceLocation  
- ISourceSpan  

Responsibilities:
- Represent original and preprocessed source
- Provide line/column mapping
- Provide substring extraction
- Provide mapping between original and expanded source

Key types:
- SourceFile  
- SourceText  
- SourceMap (COPY/REPLACE mapping)

2. Preprocessing API
--------------------
Interfaces:
- IPreprocessor  
- ICopybookResolver  
- IReplaceRule  
- IPreprocessedSource  

Key types:
- Preprocessor  
- CopybookResolver  
- ReplaceRule  
- PreprocessedSource  

Responsibilities:
- COPY expansion
- REPLACE processing
- Pseudo‑text handling
- COPY REPLACING
- Source mapping

Outputs:
- PreprocessedSource (ISourceText + mapping)

3. Token API
------------
Interfaces:
- IToken  
- ITokenStream  
- ILexer  

Key types:
- Token  
- TokenStream  
- Lexer  

Token fields:
- TokenKind  
- Lexeme  
- SourceSpan  
- Dialect flags  

Responsibilities:
- Produce token stream from preprocessed source
- Preserve source mapping
- Support fixed/free form

4. AST API
----------
Interfaces:
- IAstNode  
- IAstVisitor  
- IAstRewriter  

Key types:
- ProgramNode  
- DataDivisionNode  
- ProcedureDivisionNode  
- ParagraphNode  
- StatementNode  
- ExpressionNode  
- TypeNode  

Responsibilities:
- Represent COBOL syntax tree
- Support visitors and transformations
- Preserve source spans

5. Semantic Model API
---------------------
Interfaces:
- ISemanticModel  
- ISymbol  
- ITypeSymbol  
- IDataItemSymbol  
- IMethodSymbol  
- IClassSymbol  
- IFileSymbol  

Key types:
- SemanticModel  
- SymbolTable  
- DataItemSymbol  
- MethodSymbol  
- ClassSymbol  
- FileSymbol  
- PerformGraph  
- DataLayoutTree  

Responsibilities:
- Bind AST to symbols
- Resolve types
- Build data layout
- Build PERFORM graph
- Validate semantics
- Produce metadata for IL generation

6. IL API
---------
Interfaces:
- IILModule  
- IILType  
- IILMethod  
- IILBasicBlock  
- IILInstruction  

Key types:
- ILModule  
- ILType  
- ILMethod  
- ILBasicBlock  
- ILInstruction  
- ILOperand  
- ILTypeRef  
- ILMethodRef  

Responsibilities:
- Represent backend‑agnostic IL
- Provide structured control flow
- Provide typed instructions
- Provide metadata for backend

Instruction categories:
- Load/store  
- Arithmetic  
- Branching  
- Object operations  
- Runtime calls  
- String operations  
- File operations  
- JSON/XML operations  

7. Optimization API
-------------------
Interfaces:
- IOptimizationPass  
- IOptimizationPipeline  

Key types:
- OptimizationPipeline  
- ControlFlowSimplificationPass  
- ConstantFoldingPass  
- ConstantPropagationPass  
- CopyPropagationPass  
- DeadCodeEliminationPass  
- RedundantMovePass  
- StrengthReductionPass  
- LoopOptimizationPass  
- BranchOptimizationPass  
- PeepholePass  
- GenericSpecializationPass  

Responsibilities:
- Transform ILModule into optimized ILModule
- Preserve semantics
- Maintain verifiable IL

8. Backend API
--------------
Interfaces:
- ICilBackend  
- ITypeEmitter  
- IMethodEmitter  
- IFieldEmitter  
- IInstructionEmitter  
- IDebugInfoEmitter  

Key types:
- CilBackend  
- TypeEmitter  
- MethodEmitter  
- FieldEmitter  
- InstructionEmitter  
- DebugInfoEmitter  

Responsibilities:
- Lower IL to .NET CIL
- Emit metadata
- Emit PDB debug symbols
- Integrate with runtime library

9. Diagnostics API
------------------
Interfaces:
- IDiagnostic  
- IDiagnosticSink  
- IDiagnosticFormatter  

Key types:
- Diagnostic  
- DiagnosticSink  
- DiagnosticFormatter  
- DiagnosticCode (enum)  

Responsibilities:
- Report errors, warnings, info
- Preserve source spans
- Integrate with LSP

Diagnostic categories:
- Preprocessor  
- Lexer  
- Parser  
- Semantic  
- IL generation  
- Optimization  
- Backend  

10. Metadata API
----------------
Interfaces:
- IMetadataProvider  
- IMetadataTable  
- IMetadataEmitter  

Key types:
- MetadataProvider  
- MetadataTable  
- MetadataEmitter  

Responsibilities:
- Provide metadata for:
  - Types
  - Methods
  - Fields
  - Parameters
  - Attributes
  - Layout
- Feed metadata to CIL backend

11. Utility API
---------------
Key utilities:
- Immutable collections
- Graph utilities (CFG, PERFORM graph)
- Packed decimal helpers
- String slicing utilities
- File path utilities
- Logging utilities
- Reflection helpers (for runtime integration)

Compiler pipeline API
---------------------
The compiler exposes a single orchestrator:

CompilerPipeline
  .LoadSource()
  .Preprocess()
  .Lex()
  .Parse()
  .Analyze()
  .GenerateIL()
  .Optimize()
  .EmitCIL()
  .WriteArtifacts()

Each stage returns immutable results.

Threading model
----------------
- Compiler pipeline: single‑threaded (determinism)
- CIL emission: parallel per method (safe)
- LSP: multi‑threaded request handling
- Tools: multi‑threaded where appropriate

Error handling model
--------------------
- No exceptions for normal errors
- All errors reported via Diagnostics API
- Exceptions reserved for catastrophic failures

Versioning & stability
----------------------
Internal APIs follow:
- Stable contracts across minor versions
- Breaking changes only in major versions
- Migration notes required for maintainers

Summary
-------
The CobolSharp Internal Compiler API:
- Defines the stable internal contracts across all compiler subsystems
- Ensures modularity, maintainability, and deterministic behavior
- Provides a clean separation between AST, semantic model, IL, optimization, and backend
- Enables contributors to extend or modify the compiler safely
- Is fully aligned with the CIL‑only architecture
