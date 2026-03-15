CobolSharp COBOL Grammar & Parsing Architecture (CIL‑Only)
==========================================================

Purpose
-------
Define the authoritative grammar and parsing architecture for CobolSharp, covering:
- Grammar structure
- ANTLR integration
- Tokenization modes
- Error recovery
- AST construction
- Dialect gating (85/2002/2014/2023)
- COPY/REPLACE mapping
- Preprocessed source mapping
- Deterministic parse behavior
- CIL‑friendly lowering constraints

This document is the canonical reference for how CobolSharp parses COBOL source.

------------------------------------------------------------
SECTION 1 — GRAMMAR DESIGN PRINCIPLES
------------------------------------------------------------

1.1 Deterministic, unambiguous grammar
--------------------------------------
CobolSharp uses a grammar designed to:
- Avoid shift/reduce conflicts
- Avoid reduce/reduce conflicts
- Avoid ambiguous productions
- Support incremental parsing for LSP
- Support dialect‑specific constructs via feature flags

1.2 ANTLR‑based grammar
-----------------------
CobolSharp uses ANTLR 4 with:
- Lexer grammar (COBOLLexer.g4)
- Parser grammar (COBOLParser.g4)
- Listener/visitor generation disabled (custom AST builder used instead)

1.3 Grammar layering
--------------------
The grammar is layered:

1. Tokens (keywords, identifiers, literals)  
2. Phrases (data description, statements, expressions)  
3. Divisions (IDENTIFICATION, ENVIRONMENT, DATA, PROCEDURE)  
4. Program structure (programs, classes, methods)  

1.4 Grammar must reflect COBOL’s “English‑like” syntax
------------------------------------------------------
Examples:
- ADD A TO B GIVING C
- PERFORM UNTIL X > 10
- IF A > B THEN DISPLAY "OK"

Grammar must preserve:
- Optional keywords
- Optional punctuation
- Free‑form and fixed‑form compatibility

------------------------------------------------------------
SECTION 2 — LEXER ARCHITECTURE
------------------------------------------------------------

2.1 Lexer modes
---------------
CobolSharp uses multiple lexer modes:

DEFAULT_MODE:
- Normal tokenization

COPY_MODE:
- COPY text literal handling
- Pseudo‑text delimiters

COMMENT_MODE:
- *> comments
- Fixed‑form comment columns

STRING_MODE:
- Quoted literals
- Escaped quotes

DIRECTIVE_MODE:
- >>IF, >>DEFINE, >>EVALUATE, etc.

2.2 Fixed‑form handling
-----------------------
Rules:
- Columns 1–6: sequence numbers (ignored)
- Column 7: continuation/comment indicator
- Columns 8–72: source text
- Columns 73–80: ignored

2.3 Free‑form handling
----------------------
Rules:
- No column restrictions
- *> comments allowed anywhere
- Continuation via hyphen at end of line

2.4 Token categories
--------------------
- Keywords
- Identifiers
- Literals (numeric, alphanumeric, national)
- Operators
- Punctuation
- Compiler directives
- COPY/REPLACE pseudo‑text

------------------------------------------------------------
SECTION 3 — PARSER ARCHITECTURE
------------------------------------------------------------

3.1 Parser entry points
-----------------------
programUnit:
- PROGRAM-ID
- CLASS-ID
- INTERFACE-ID

3.2 Division structure
----------------------
IDENTIFICATION DIVISION:
- PROGRAM-ID
- AUTHOR
- INSTALLATION
- DATE-WRITTEN
- SECURITY

ENVIRONMENT DIVISION:
- CONFIGURATION SECTION
- INPUT-OUTPUT SECTION

DATA DIVISION:
- FILE SECTION
- WORKING-STORAGE SECTION
- LOCAL-STORAGE SECTION
- LINKAGE SECTION
- REPORT SECTION (ignored)

PROCEDURE DIVISION:
- Declaratives
- Paragraphs
- Sections
- Statements

3.3 Statement categories
------------------------
- Control flow (IF, EVALUATE, PERFORM, GO TO)
- Arithmetic (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)
- Data movement (MOVE, STRING, UNSTRING, INSPECT)
- File I/O (OPEN, READ, WRITE, REWRITE, DELETE, CLOSE)
- JSON/XML (JSON PARSE, JSON GENERATE, XML PARSE, XML GENERATE)
- OO (INVOKE, NEW, METHOD-ID)
- Interop (CALL "Namespace.Class::Method")

3.4 Expression grammar
----------------------
Expressions support:
- Arithmetic operators
- Boolean operators
- Relational operators
- Parentheses
- Function calls
- Literals
- Identifiers

Grammar ensures:
- Left‑associativity for arithmetic
- Precedence: NOT > AND > OR
- Precedence: * / > + -

------------------------------------------------------------
SECTION 4 — AST (ABSTRACT SYNTAX TREE)
------------------------------------------------------------

4.1 AST construction
--------------------
CobolSharp uses a custom AST builder:
- No ANTLR parse tree leakage
- AST nodes contain:
  - Node kind
  - Children
  - Source span
  - Dialect flags
  - Semantic hints

4.2 AST node categories
-----------------------
- ProgramNode
- ClassNode
- MethodNode
- DataItemNode
- FileDescriptorNode
- ParagraphNode
- SectionNode
- StatementNode
- ExpressionNode
- ConditionNode
- JsonNode
- XmlNode

4.3 AST invariants
------------------
- No null children
- No ambiguous nodes
- No grammar‑specific artifacts
- All optional constructs represented explicitly

------------------------------------------------------------
SECTION 5 — DIALECT GATING
------------------------------------------------------------

5.1 Dialect flags
-----------------
CobolSharp supports:
- COBOL‑85
- COBOL‑2002
- COBOL‑2014
- COBOL‑2023

Each grammar rule may be gated by:
- @if(dialect >= 2002)
- @if(feature OO)
- @if(feature GENERICS)

5.2 Dialect‑specific constructs
-------------------------------
Examples:
- METHOD-ID (2002+)
- CLASS-ID (2002+)
- GENERIC (2023)
- JSON PARSE (2014+)
- XML PARSE (2014+)

------------------------------------------------------------
SECTION 6 — ERROR RECOVERY
------------------------------------------------------------

6.1 Goals
---------
- Never crash
- Never infinite‑loop
- Produce usable AST
- Preserve as much structure as possible

6.2 Recovery strategies
-----------------------
- Single‑token insertion
- Single‑token deletion
- Synchronization sets (END‑IF, END‑PERFORM, etc.)
- Statement‑level recovery
- Paragraph‑level recovery

6.3 Error nodes
---------------
Inserted when:
- Unexpected token
- Missing keyword
- Missing identifier
- Unterminated string
- Malformed statement

------------------------------------------------------------
SECTION 7 — PREPROCESSOR INTEGRATION
------------------------------------------------------------

7.1 COPY/REPLACE mapping
------------------------
Parser receives:
- Expanded source
- Mapping table (original → expanded)

AST nodes store:
- Original source span
- Expanded source span

7.2 COPY inside COPY
--------------------
Fully supported:
- Nested COPY
- COPY REPLACING inside COPY
- COPY with pseudo‑text

------------------------------------------------------------
SECTION 8 — CIL‑FRIENDLY LOWERING CONSTRAINTS
------------------------------------------------------------

8.1 Structured control flow
---------------------------
Parser must produce AST that can be lowered to:
- Structured loops
- Structured conditionals
- Structured blocks

8.2 No ambiguous PERFORM THRU ranges
------------------------------------
Parser resolves:
- Paragraph ranges
- Section boundaries

8.3 Expression normalization
----------------------------
Parser produces:
- Fully parenthesized AST
- No precedence ambiguity

------------------------------------------------------------
SECTION 9 — TESTING STRATEGY
------------------------------------------------------------

9.1 Unit tests
--------------
- Grammar fragments
- Tokenization
- Error recovery

9.2 Golden tests
----------------
- AST dumps
- Parse trees
- Diagnostics

9.3 Fuzzing
-----------
- Random token streams
- Random COBOL fragments

9.4 Conformance tests
---------------------
- ISO/IEC 1989:2023 grammar tests

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Grammar & Parsing Architecture:
- Defines a deterministic, unambiguous COBOL grammar
- Uses ANTLR with custom AST construction
- Supports all COBOL‑85 → COBOL‑2023 features
- Integrates COPY/REPLACE mapping
- Provides robust error recovery
- Produces CIL‑friendly ASTs
- Forms the foundation of the entire compiler pipeline
