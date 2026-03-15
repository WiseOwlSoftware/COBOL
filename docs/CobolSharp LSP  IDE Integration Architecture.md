CobolSharp LSP / IDE Integration Architecture
=============================================

High-level goals
----------------
- Provide a modern, language-server–driven development experience for COBOL.
- Support all major IDE features:
  - Syntax highlighting
  - Semantic highlighting
  - Code completion
  - Go to definition / find references
  - Hover information
  - Signature help
  - Document symbols / outline
  - Rename refactoring
  - Diagnostics (syntax + semantic)
  - Code actions (quick fixes)
  - Formatting
  - Workspace-wide symbol search
- Integrate tightly with:
  - Preprocessor (COPY/REPLACE)
  - Semantic model
  - IL generation pipeline (optional)
- Maintain performance on large COBOL codebases with thousands of copybooks.

Overall architecture
--------------------
The LSP server is a standalone process:

  cobolsharp-lsp.exe

It communicates with IDEs via the Language Server Protocol (JSON-RPC).

Core components:
- DocumentManager
- PreprocessorService
- LexerService
- ParserService
- SemanticService
- Indexer
- WorkspaceSymbolTable
- DiagnosticsEngine
- CompletionEngine
- HoverEngine
- DefinitionEngine
- ReferenceEngine
- RenameEngine
- CodeActionEngine
- FormattingEngine

Document lifecycle
------------------
1. User opens a COBOL file in the IDE.
2. LSP server receives:
   - textDocument/didOpen
   - textDocument/didChange
3. DocumentManager stores:
   - Raw text
   - Preprocessed text
   - Token stream
   - Parse tree
   - Semantic model
   - Diagnostics

Incremental pipeline
--------------------
On each edit:
1. PreprocessorService:
   - Re-expands COPY/REPLACE incrementally.
   - Maintains mapping from expanded text → original source.
   - Only reprocesses affected regions.

2. LexerService:
   - Tokenizes expanded text.
   - Uses lexer modes (default, comment, copy, pseudo-text).
   - Produces token stream with source mapping.

3. ParserService:
   - Parses token stream into AST.
   - Uses error recovery to keep tree usable even if incomplete.

4. SemanticService:
   - Performs incremental semantic analysis:
     - Symbol discovery
     - Name resolution
     - Type binding
     - Data description validation
     - Control-flow graph
     - OO/generics binding
     - File I/O semantics
   - Only re-analyzes affected symbols.

5. DiagnosticsEngine:
   - Collects syntax + semantic diagnostics.
   - Sends textDocument/publishDiagnostics.

Workspace indexing
------------------
WorkspaceSymbolTable:
- Stores all symbols across the workspace:
  - Programs
  - Classes
  - Methods
  - Data items
  - Copybooks
  - Typedefs
- Supports:
  - Workspace-wide search
  - Cross-file references
  - Global rename
  - IntelliSense across copybooks

Indexer:
- Runs in background.
- Updates index when files change.
- Tracks dependencies:
  - COPY relationships
  - CALL relationships
  - INVOKE relationships
  - PERFORM relationships

IDE features
------------

1. Syntax highlighting
   - Based on token types.
   - Enhanced by semantic classification (e.g., data item vs paragraph vs method).

2. Semantic highlighting
   - Colors based on symbol kind:
     - Data item
     - File
     - Paragraph
     - Method
     - Class
     - Generic parameter
     - Condition name (88-level)
   - Highlights redefinitions and renames.

3. Completion
   - Context-aware:
     - After MOVE → data items
     - After CALL → program names
     - After INVOKE → methods
     - Inside TYPE → typedefs and generic types
     - Inside JSON/XML → data items
   - Uses semantic model + workspace index.

4. Hover information
   - Shows:
     - Symbol kind
     - PIC/USAGE
     - OCCURS
     - REDEFINES
     - Typedef/generic info
     - Method signature
     - File organization
   - Includes source location of definition.

5. Go to definition
   - Resolves:
     - Data items
     - Paragraphs/sections
     - Methods
     - Classes/interfaces
     - Typedefs
     - COPY targets
   - Uses semantic model + index.

6. Find references
   - Cross-file search.
   - Includes:
     - PERFORM references
     - CALL references
     - INVOKE references
     - Data item usage
     - Condition name usage

7. Rename refactoring
   - Symbol-aware rename:
     - Data items
     - Paragraphs
     - Methods
     - Classes
     - Typedefs
   - Updates all references across workspace.
   - Respects COPY boundaries (optional: update copybooks).

8. Code actions
   - Quick fixes:
     - Add missing COPY
     - Add missing TYPEDEF
     - Suggest REDEFINES corrections
     - Suggest OCCURS DEPENDING ON fixes
     - Insert END-IF, END-PERFORM, END-EVALUATE, etc.
   - Refactorings:
     - Extract paragraph
     - Inline paragraph
     - Convert GO TO to PERFORM (where safe)

9. Formatting
   - Aligns:
     - PIC clauses
     - VALUE clauses
     - OCCURS
     - REDEFINES
   - Indents:
     - IF/ELSE/END-IF
     - PERFORM/END-PERFORM
     - EVALUATE/WHEN/END-EVALUATE
   - Optional fixed-form column rules.

10. Signature help
    - For CALL/INVOKE:
      - Shows parameter list
      - Highlights current argument
    - For JSON/XML:
      - Shows required clauses

11. Document symbols / outline
    - Programs
    - Classes
    - Methods
    - Sections
    - Paragraphs
    - 01-level data items
    - COPY inclusions

Performance strategy
--------------------
- Incremental parsing and semantic analysis.
- Preprocessor caching for copybooks.
- Workspace index stored in memory + on-disk cache.
- Parallel background indexing.
- Debounced updates (e.g., 150ms after last keystroke).
- Lazy evaluation of expensive features (e.g., full CFG).

Debugging integration
---------------------
Optional:
- Provide debug adapter protocol (DAP) integration.
- Map IL instructions back to COBOL source.
- Support:
  - Breakpoints
  - Step in/out/over
  - Variable inspection
  - Watch expressions
  - Call stack view

Testing strategy
----------------
- Unit tests for each LSP feature.
- Golden tests for diagnostics.
- Stress tests with large COBOL codebases.
- COPY/REPLACE expansion tests.
- Semantic model consistency tests.

Summary
-------
CobolSharp’s LSP/IDE integration architecture:
- Provides a modern, responsive, incremental language server.
- Integrates deeply with the preprocessor, parser, and semantic model.
- Supports all major IDE features: completion, hover, go-to-definition, rename, diagnostics, formatting.
- Scales to large COBOL codebases with thousands of copybooks.
- Enables a first-class development experience for COBOL-2023.
