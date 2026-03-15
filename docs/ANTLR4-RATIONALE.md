# Why ANTLR4 for COBOL — Design Rationale

## Why naïve ANTLR grammars fail for COBOL

Most failed attempts run into the same traps:

### 1. Context-sensitive keywords

COBOL keywords can be:
- data names in some contexts
- procedure names in others
- type names in others
- and sometimes both

Example: `END` can be:
- a terminator (END-READ)
- an imperative statement (COBOL-85)
- a data name

ANTLR4 can handle this — but only if you don't treat keywords as fixed tokens with rigid precedence.

### 2. Column-sensitive lexical rules (fixed format)

COBOL's fixed-format rules (Area A/B, sequence numbers, indicator columns) are lexical, not syntactic. ANTLR4 can handle them, but only if you preprocess or use a custom lexer mode.

### 3. The "everything is optional" problem

COBOL statements often have dozens of optional clauses. A naïve grammar becomes ambiguous or exponential.

Solution: factor the grammar into layered, modular files.

### 4. Paired terminators (END-IF, END-READ, END-PERFORM)

These are not ambiguous — but they require structured productions, not flat ones.

### 5. COPY / REPLACE / compiler directives

These are not part of the grammar. They must be handled by a preprocessor before ANTLR sees the source.

### 6. Free-format vs fixed-format

ANTLR4 can handle both, but not with the same lexer rules. You need two lexer modes or a preprocessing pass.

---

## Why ANTLR4 is practical — if you use the right architecture

### 1. Preprocessor layer (mandatory)

Handles:
- COPY
- REPLACE
- compiler directives
- fixed-format column rules
- line continuation
- comment indicators
- source normalization

ANTLR4 should never see raw COBOL source.

### 2. Layered grammar (Option 4)

ANTLR4 handles:
- procedural core
- OO extensions
- generics
- JSON/XML
- exception handling
- type definitions

Each layer is a separate grammar file or grammar fragment.

### 3. Dialect overlays

ANTLR4 supports this via:
- grammar imports
- parser options
- semantic predicates

This is how you support:
- COBOL-85
- COBOL-2002
- COBOL-2014
- COBOL-2023

...without contaminating the core grammar.

### 4. Semantic phase

Handles the genuinely context-sensitive parts:
- resolving ambiguous identifiers
- determining whether a token is a data name or a paragraph name
- validating paired terminators
- resolving generics
- type inference
- OO dispatch resolution

ANTLR4 is not meant to do this — and shouldn't.

---

## The key insight

The assumption that "ANTLR4 can't describe COBOL" implicitly assumes:

> "ANTLR must do everything — lexing, parsing, context resolution, dialect selection, and semantic validation."

That's not how real compilers work.

**ANTLR4 is a parser generator, not a full compiler front-end.**

COBOL is absolutely within its domain.

---

## Why ANTLR4 is the right choice for CobolSharp

CobolSharp is building a production-quality compiler targeting .NET. ANTLR4 gives you:

- a clean parse tree
- listener/visitor support
- grammar modularization
- excellent error recovery
- good performance
- easy integration with C#

And with the layered grammar approach, it becomes not just practical — but ideal.

---

## Previous approach: hand-written recursive descent

The hand-written parser (Parser.cs, ~4200 lines across 9 partial class files) suffered from
systematic drift between the grammar specification and the implementation. This was documented
as a recurring failure in DEVLOG entries 011, 013, 016, 017, 020, 022, and 023.

Root cause: **the grammar and the parser were separate artifacts**. Every bug existed because
they drifted apart, and every fix required manually checking the grammar reference — which
was repeatedly not done.

With ANTLR4, **the grammar IS the parser**. There is no drift to manage.

---

## COBOL-85 compatibility: bare END as imperative

In COBOL-85, `END` was a valid standalone imperative statement (a degenerate no-op).
NIST test programs write `READ file RECORD END` which is `AT END END` — the AT END
clause with `END` as the minimal imperative.

COBOL-2023 removed standalone `END` as an imperative — it is now only a terminator
keyword prefix (END-READ, END-IF, etc.).

CobolSharp accepts this via the `dialect85Imperative` rule in `CobolDialect.g4`,
gated by the `is85()` semantic predicate. This is the same approach used by IBM,
Micro Focus, GnuCOBOL, and Fujitsu: older syntax is supported under compatibility
modes, not by contaminating the core grammar.
