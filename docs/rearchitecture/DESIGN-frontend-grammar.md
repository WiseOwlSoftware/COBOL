# DESIGN — Target Frontend & Grammar Architecture

Status: DESIGN — substantially implemented. The dead-grammar/JSON-XML deletions, the single-sourced
context-sensitive word set, the shared-lowest `Cobol.Net.Editions` registry, and the one `VersionConformancePass`
edition gate are all in the tree; the §8 questions are resolved as decided. Remaining frontend work: completing the
`Cst/` typed-façade migration (§3.7, paced by the binder track), the D10 SUBSCRIPT-mode removal (§9, scheduled for
PHASE 15), and the residual preprocessor namespace / `using Core =` alias cleanup.
Scope: `src/Cobol.Net.Frontend/**` — the ANTLR lexer/parser grammars (`Grammar/**/*.g4`), the hand-written
preprocessor (`Preprocessor/`), the parse-stage machinery (`Parsing/`), the diagnostics model
(`Diagnostics/`), the generated-parser build (`GenerateIfNewer.ps1` + `Invoke-Antlr4CSharp.ps1` + csproj
targets), and the frontend's contract to its ONE consumer, the `Cobol.Net.Compiler` binder.

This document is the decision-complete target for the frontend. It is a companion to
`docs/rearchitecture/DESIGN-edition-framework.md` (the edition/version framework — this doc owns the
parse-time construct-id ANNOTATION and defers the registry to that doc; edition GATING itself lives in the
`VersionConformancePass`, `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`) and to the binder
rearchitecture docs (which own the parse-tree *consumption* side; this doc specifies the façade the binder
consumes).

> **This is a SUPERSET grammar.** It recognises the union of all editions — there are no edition `{isXXXX()}?` gates;
> every construct parses at every `--std`. Each version-gated rule carries a committed-match construct-id ANNOTATION (a
> parse *action*, not a speculative gating predicate — which would be evaluated speculatively during a failing parse),
> while version NUMBERS stay single-sourced in `constructs.json`. Edition gating lives entirely in ONE
> `VersionConformancePass` over the bound tree (parse → edition-agnostic bind → VersionConformancePass → emit-if-clean →
> backend); there is no `ReservedWordEditionHints`. Full design:
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.

---

## 0. Hard invariants this design upholds

1. **Typed-native only** — the frontend produces a *syntactic* parse tree; it never touches storage form. No
   change here can regress the byte invariant. (Unaffected but stated for completeness.)
2. **Spec-first** — every gate, token, and severity cites ISO/IEC 1989:2023 (`specs/ISO_COBOL.md`). The
   canonical construct/edition metadata is `tests/version-matrix/constructs.json` +
   `tests/version-matrix/reserved-words.json`; the frontend must consume that single source, never a copy.
3. **Battery green throughout** — the FULL battery (greenfield conformance + unit + characterization + the
   NIST legacy guard; current counts live in the plan's STATUS banner) stays
   green at *every* step. The migration in §5 is a sequence of individually-green commits; no big-bang except
   the sanctioned G8 namespace rename, which this design brings forward where it is free.
4. **Singular pattern** — ONE source of truth for the context-sensitive word set (today triplicated), ONE
   edition-gating mechanism (the `VersionConformancePass` over the bound tree), ONE diagnostic type end-to-end.
5. **Four editions in one** — ONE superset grammar (no edition predicates; each version-gated rule carries a
   committed-match construct-id annotation), validated by the VERSION TEST MATRIX. Edition gating is the single
   `VersionConformancePass` over the bound tree, not parse-time predicates — the one-grammar approach kept (correct and
   cheap), with both the duplication and the scattered gating removed. *(`DESIGN-version-conformance-pipeline.md`.)*
6. **JSON/XML are non-ISO** — 0 spec occurrences; they are deleted from the live grammar (§4).
7. **⛔ THE REPETITION ARITY IS THE GENERAL FORMAT'S, NEVER A CONVENIENCE.** A rule may parse a SUPERSET of the
   operand *shapes* the standard admits (the parse-wide/bind-narrow doctrine) — it may **not** invent a
   REPETITION the general format does not print. A loop the standard never licensed cannot accept new legal
   source; all it can do is give the parser a second, wrong way to read source that is already legal.
   **Worked example (PB45).** `evaluateWhenGroup : NOT? evaluateWhenItem+` against §14.9.13.2's
   `{ { WHEN selection-object [ ALSO selection-object ] … } … }`, where objects repeat ONLY through ALSO. The
   spare `+` let the parser PEEL a function's argument list off it — `WHEN FUNCTION SQRT(X) > 1` parsed as the
   two objects `FUNCTION SQRT` and `(X) > 1`, because `functionCall`'s argument list is optional and
   `primaryExpression` has a `LPAREN arithmeticExpression RPAREN` alternative. It compiled clean and threw at run
   time. ⚠ **The hazard is ANTLR's first-*viable*-alternative rule, not its first-alternative rule**: where both
   readings are viable the greedy one wins (measured: `DISPLAY`, `STRING`, `INSPECT`, nested function arguments
   and multi-operand `ADD` never peeled), so the peel appears only where the correct reading is NOT viable — which
   is precisely where it is hardest to notice. ⚠ And the binder had invented an uncited semantics ("additional
   items AND in") to give the spare loop a meaning, so the misparse had somewhere to land.
   **The check, when writing or reviewing any `+`/`*` in a statement rule: point at the `…` in the printed general
   format that licenses it, and at the separator it repeats over.** If there is no `…`, there is no loop. A
   PHASE-13 research artifact had already flagged this exact rule as a first-alternative hazard
   (`evidence/PHASE-13-grammar-batch-research.json`) and nothing acted on it — findings live in `kb/Work/`, not in
   evidence files.
8. **⛔ THE OPTIONALITY IS THE GENERAL FORMAT'S TOO — AND WHERE THE GRAMMAR STAYS PERMISSIVE, THE BINDER OWES A
   NAMED SCREEN.** Invariant 7's mirror. ISO §5.2.6.2 makes BRACKETS the only convention that lets a portion of
   a general format be omitted, and §5.2.2 makes an underlined keyword required subject to those conventions —
   so a phrase printed on its own line with no brackets is MANDATORY, and writing it `phrase?` in the grammar
   under-rejects. That is the **falsely-permissive twin of the OCR's falsely-restrictive bias**: the
   transcription's diagrams were lossy toward *rejecting* legal source (CLAUDE.md rule 1), and a grammar written
   from a lossy diagram fails the other way just as silently.
   **The decision (kb/Work PB350, 2026-09-05): such a phrase is enforced at BIND time, not by requiring it in
   the grammar.** `StatementValidation.ScreenOmittedRequiredPhrase` → `DiagnosticCatalog.FormatRequiredPhraseOmitted`
   (COBOLNET1850), the mirror of the forbidden-phrase screen `ScreenForbiddenPhrase`/COBOLNET1720 — one
   descriptor carrying the SHAPE, each call site quoting its own §. Two reasons, and they generalize:
   a bind-time diagnostic NAMES the omitted phrase, the statement and the clause where an ANTLR parse error can
   only report an unexpected token; and the screen can be placed AFTER the binder has normalized the parse
   tree's shape away, so ONE test covers every grammar arm.
   **Worked example (PB350).** §14.9.34.2 prints RETURN's `AT END imperative-statement-1` unbracketed between a
   bracketed `[ NOT AT END … ]` and `[ END-RETURN ]` (rendered from the printed page, folio 708). The grammar
   wrote `returnAtEndPhrase?` AND `returnAtEndPhrase`'s reversed alternative made the AT END half optional too,
   so a RETURN with no AT END compiled at all four editions and, at end of data, control fell THROUGH the
   statement onto a record area §14.9.34.4 GR3 leaves undefined — a loop written on RETURN could never terminate
   from the statement. The screen tests `atEnd is null` after `PhraseBlocks.Split`, which covers BOTH arms;
   testing the phrase NODE would have left the reversed NOT-only arm compiling (the two-arm dispatch with one
   arm fixed). ⚠ `readAtEnd` is NOT the same case: §14.9.30.2 prints READ's AT END / NOT AT END pair inside
   brackets WITH choice indicators, which §5.2.6.4 reads as "zero or more", so there the phrase really is
   optional. The asymmetry is why the two pages are measured separately.
   **The check, when writing or reviewing any `?` on a statement PHRASE rule: point at the brackets in the
   printed general format that license it.** If the line is unbracketed, the phrase is required and owes a
   screen. `RequiredFormatPhraseDriftTests` re-derives the whole set from the transcription — today RETURN's AT
   END (screened) and SEARCH's WHEN (already `+` in the grammar) — so a transcription repair or a new format
   fails until its phrase is screened or adjudicated.

---

## 1. Current problems (grounded in the survey + code)

Evidence is cited to file:line as verified in the tree.

### 1.1 Dead and mislabeled grammar files
Five top-level `.g4` files are **neither generated nor referenced** by any C#, csproj, or build script
(`grep` over `src/**` for their names hits only the files themselves and the `obj/antlr-lib` staging copy):
`Grammar/CobolDialect.g4` (105 loc), `Grammar/CobolParserGenerics.g4` (101), `Grammar/CobolParserJsonXml.g4`
(118), `Grammar/CobolParserOO.g4` (160), `Grammar/CobolPreprocessor.g4` (98). `CobolParserOO.g4` /
`CobolPreprocessor.g4` are stale duplicates of live code (`Core/CobolOO.g4`, the hand-written
`Preprocessor/`). They mislead every reader about what actually compiles and inflate the grammar surface a
maintainer must reason about.

### 1.2 Non-ISO JSON/XML inside a LIVE imported fragment
`Core/CobolExtensionsJsonXml.g4` is imported by `CobolParserCore.g4` (line 17) and its `jsonStatement` /
`xmlStatement` are wired into the `statement` rule under `{is2014()}?` gates
(`CobolParserCore.g4:716-717`). JSON/XML have **0 occurrences in the ISO spec** — a hard-invariant violation
that they sit in a live fragment and a live dispatch arm. The same fragment ALSO holds
`inlineMethodInvocationStatement` (a real 2023 OO construct, gated at `:719`) — so the fragment is
mis-named: it is 90% dead-non-ISO and 10% live-OO.

### 1.3 The context-sensitive word set is triplicated and hand-synced
The set of tokens that are keywords in context but legal user-defined words elsewhere is maintained in
**three** physically separate places that must agree by hand:
- lexer `_dataNameTokens` HashSet — `Core/CobolLexer.g4:30-72` (drives SUBSCRIPT-mode entry);
- parser `cobolWord` rule — `CobolParserCore.g4:26-73` (admits the word in a name slot);
- compiler `ReservedWords` table — `src/Cobol.Net.Compiler/Validation/ReservedWords.Table.cs` (the §8.9
  reserved-word funnel).
The comment at `CobolLexer.g4:20-21` literally instructs the maintainer to hand-mirror the two grammar
copies ("It MUST mirror the parser's cobolWord rule … When adding a token to cobolWord, add it here"). A
silent desync mis-triggers or fails to trigger SUBSCRIPT mode → a wrong-or-missing parse error with no
diagnostic pointing at the cause. **Note the asymmetry:** `ReservedWords.Table.cs` is ALREADY generated from
`tests/version-matrix/reserved-words.json` by `scripts/gen-reserved-words.ps1` (header at
`ReservedWords.Table.cs:1-6`), and a drift test binds them — so the codegen discipline the other two copies
lack already exists and is proven. The fix is to extend that generator, not invent one.

### 1.4 EditionGateHints re-derives what the `{isXXXX()}?` gate already knew
When an introduction predicate rejects a too-new construct, ANTLR reports a generic `NoViableAlternative`.
`Parsing/EditionGateHints.cs` (207 loc, `Recognize`) then **reverse-engineers** which gated construct was
rejected, via a 29-entry table of `(offending-token, rule-stack, lookahead-window)` signatures
(`EditionGateHints.cs:35-169`). The signatures were "derived empirically" (its own remark) and
are inherently brittle: several arms carry dual-path token-adjacency fallbacks because the rule can pop off
the stack before the error is reported (`:85-88`, `:100-131`). This is a whole subsystem whose only job is
to recover an identity the gate *had at reject time and threw away*. The dossier reproduced a duplicate
diagnostic for an edition-gated construct as a direct consequence.

### 1.5 DialectLevel is double-sourced; edition metadata straddles the assembly seam
`DialectLevel` is stored independently on `CobolParserCoreBase` (`:17`), on `Frontend`
(`Frontend.cs:45`), and on the compiler's `EditionContext`. The canonical `ConstructRegistry` lives in
`Cobol.Net.Compiler`, which the frontend cannot see (Compiler references Frontend, not vice-versa) — so the
frontend `EditionGateHints` + the preprocessor's `ReferenceFormatProcessor.EditionGates` /
`CopyProcessor` each carry their **own** copy of the edition metadata and the strict/permissive severity
policy. Three independent renders of "removed = error strict / warning permissive."

### 1.6 The binder consumes the tree by string interpretation, not the generated visitor
The `Cobol.Net.Compiler` binder hand-walks the raw ANTLR context types with **~336 `GetText()` calls** and
string comparisons (dossier; e.g. `DataBinder.cs:155`), rather than the generated typed visitor. ANTLR's
`-visitor` output is generated (`Invoke-Antlr4CSharp.ps1:71`) but only `EditionValidator` uses it. The raw
parse-tree shape is thus a wide, un-narrowed, stringly-typed cross-assembly contract with no façade: any
grammar rule rename ripples into dozens of `GetText()` sites invisibly.

### 1.7 Stale namespace/assembly split and stale doc
Generated code emits into namespace `CobolSharp.Compiler.Generated` while living in assembly
`Cobol.Net.Frontend` (RootNamespace `CobolNet.Frontend`) — the package name is hard-coded at
`Invoke-Antlr4CSharp.ps1:29`. Every consumer aliases `using Core = CobolParserCore`. The preprocessor files
physically live in `src/Cobol.Net.Frontend/Preprocessor/` but still declare
`namespace CobolSharp.Compiler.Preprocessor` (verified on all five files). `Frontend.cs:16` claims it "is the
ONE place COBOL.NET reuses the legacy `CobolSharp.Compiler` assembly" — **this is stale**: the preprocessor
and parse machinery were already physically extracted into `Cobol.Net.Frontend`; only `DiagnosticBag` /
`TurnEvent` type *namespaces* remain legacy-named, and they too live in this assembly now
(`Diagnostics/DiagnosticBag.cs`, `Preprocessor/TurnDirectiveProcessor.cs`).

### 1.8 Committed build-output caches and brittle two-stage error handling
`Grammar/.antlr/` and `Grammar/Core/.antlr/` hold ANTLR java-target IDE caches checked into the tree
(`CobolLexer.java`, `CobolPreprocessor.java`, `.interp`, `.tokens`) — build output, and one of them
(`CobolPreprocessor.*`) is a cache of a DEAD grammar. `Frontend.cs:135` catches `catch (Exception)` on the
SLL bail — this masks predicate/lexer-action bugs (a `NullReferenceException` in a semantic predicate) as a
"retry with LL," hiding real defects behind a silent second parse.

### 1.9 Parallel SUBSCRIPT lexer vocabulary
SUBSCRIPT mode (`CobolLexer.g4:726-776`) re-declares its own `SUB_*` literal/operator/identifier tokens
(`SUB_INTEGERLIT`, `SUB_STRINGLIT`, `SUB_PLUS`, `SUB_IDENTIFIER`, …) paralleling the DEFAULT-mode tokens,
because the disambiguation of `x(i)`-as-subscript vs `(a+b)`-as-grouping is punted from the grammar into a
lexer mode driven by `PreviousTokenCouldBeDataName()`. The binder then re-parses the captured subscript
token run (`ReferenceResolver` `SplitSubscriptTokens`). Two vocabularies + a binder-side mini-parser for
one concept.

---

## 2. Target architecture — overview

The frontend becomes a **three-layer, single-source-of-truth** stack with a **narrow typed façade** to the
binder:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Cobol.Net.Editions            (NEW lowest assembly — see DESIGN-edition-framework.md)  │
│    ConstructRegistry · ReservedWords · EditionSeverityPolicy · EditionInfo    │
│    (generated from constructs.json + reserved-words.json + cobol-words.json)   │
└───────────────▲───────────────────────────────────────────────▲──────────────┘
                │ referenced by                                  │ referenced by
┌───────────────┴───────────────────────────┐   ┌───────────────┴──────────────┐
│  Cobol.Net.Frontend                        │   │  Cobol.Net.Compiler          │
│   Preprocessor/   (ns → CobolNet.Frontend) │   │   Binding/ (via façade only) │
│   Grammar/        (2 generated units)      │   │ Validation/VersionConformance│
│   Generated/      (ns → CobolNet.Frontend. │──▶│     Pass (sole edition gate) │
│                    Generated)              │   │   … consumes Cst façade,     │
│                                            │   │      not raw contexts        │
│   Parsing/        (ParseSession, recovery) │   └──────────────────────────────┘
│   Cst/            (NEW typed façade layer) │
│   Diagnostics/    (one Diagnostic model)   │
└────────────────────────────────────────────┘
```

Key decisions, each expanded below:

- **D1. ONE superset grammar — no per-edition forks, no edition predicates.** The single grammar recognises
  the union of all editions; every construct parses at every `--std`. Being "four compilers in one" is the
  job of the ONE `VersionConformancePass` over the bound tree, not of the parser — the only surviving
  semantic predicates are the two load-bearing forward-detects (D3). We remove the *duplication and
  reverse-engineering* around edition identity (D3, D4).
- **D2. Generate the context-sensitive word set** (lexer HashSet + parser `cobolWord` rule + the
  `ReservedWords`/gate metadata) from ONE declarative source, extending the existing
  `gen-reserved-words.ps1` codegen. Kill the triple hand-sync.
- **D3. Committed-match construct-id ANNOTATION; edition diagnosis in the `VersionConformancePass`.** Each
  version-gated rule carries a construct-id annotation — a parse *action* plus side-table storage keyed by
  parse context, never a predicate (ANTLR evaluates hoisted predicates speculatively during a failing parse,
  so a gating/stamping predicate mis-attributes constructs). The pass reads the annotation (or a
  self-identifying bound node) and gates via `ConstructRegistry.Check`. No signature table, no
  reverse-signature recogniser — `ReservedWordEditionHints` does not exist in the end state. The two
  load-bearing forward-detects survive: the `openClause` `{is2002() || retryPhraseAhead()}?` and the
  `boolExprAhead()`-based boolean-condition ENTRY (§3.2).
- **D4. Move the canonical edition registry to a shared lowest assembly** (`Cobol.Net.Editions`) both the
  frontend and compiler reference, deleting the frontend's + preprocessor's metadata/severity copies. (Owned
  by DESIGN-edition-framework.md; this doc specifies the frontend's *consumption*.)
- **D5. Delete the 5 dead grammars; split the JSON/XML fragment** — move the live
  `inlineMethodInvocationStatement` into `Core/CobolOO.g4`, delete the non-ISO JSON/XML rules and their
  dispatch arms, delete the now-empty fragment.
- **D6. Introduce a `Cst/` typed façade** (thin wrappers over the generated contexts) as the ONLY surface the
  binder consumes — retire the ~336 `GetText()` hand-walks incrementally behind it.
- **D7. One `Diagnostic` type end-to-end**, one `ParseSession` orchestrator, narrowed exception handling,
  fail-hard portable regen unchanged in behavior but with the package name in an MSBuild property.
- **D8. Bring the generated-namespace rename forward** to `CobolNet.Frontend.Generated` (it is free and
  removes the `using Core =` alias everywhere), and fix the stale `Frontend.cs` banner + preprocessor
  namespaces in the same wave.

---

## 3. Target design — detail

### 3.1 Grammar fragment factoring (unchanged shape, corrected membership)

The two generated units stay: `Grammar/Core/CobolLexer.g4` (lexer) and `Grammar/CobolParserCore.g4`
(parser). The parser keeps importing Core fragments, but the import list is corrected:

```
import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter,
       CobolIO, CobolControlFlow, CobolOO, CobolScreen;
```

`CobolExtensionsJsonXml` is **removed from the import list** (D5). Fragment responsibilities are re-stated as
a one-line banner at the top of each fragment (there is currently no per-fragment ownership note):

| Fragment | Owns | Version-gated content (construct-id annotated; gated by the pass) |
|---|---|---|
| `CobolExpressions.g4` | arithmetic/condition/boolean tiers, FUNCTION calls, literals | boolean operator tiers (2002), XOR (2023) |
| `CobolData.g4` | level entries, PIC, USAGE (incl. the shared `encodingPhrase`/`endiannessPhrase` the OPTIONS clauses also cite), OCCURS(+DYNAMIC), REDEFINES, VALUE, TYPEDEF/TYPE | OCCURS DYNAMIC (2014), TYPEDEF/TYPE/BASED/PROPERTY (2002), the USAGE float format phrases (2014) |
| `CobolSpecialNames.g4` | SPECIAL-NAMES, ALPHABET/CLASS/CURRENCY | FOR NATIONAL (2002) |
| `CobolIO.g4` | FILE-CONTROL/SELECT, OPEN/READ/…, SORT/MERGE, sharing/lock | SHARING/LOCK/RETRY/UNLOCK (2002) |
| `CobolControlFlow.g4` | IF/EVALUATE/PERFORM/GO/CALL/RAISE/RESUME | RAISE/RESUME/RAISING (2002) |
| `CobolOO.g4` | CLASS/INTERFACE/METHOD, INVOKE, **inline method invocation** | all (2002), inline-invoke (2023) |
| `CobolReportWriter.g4` | RD, report groups | — |
| `CobolScreen.g4` | SCREEN SECTION (parse-only today) | — |

**Rationale for keeping the fragment split:** ANTLR import fragments give physical cohesion per subsystem
with zero runtime cost. Version-gated rules live *inside* their subsystem fragment, each carrying its
committed-match construct-id annotation (introduction editions single-sourced in `constructs.json`, decided
by the `VersionConformancePass`); fragment-at-a-time landing keeps the incremental discipline of
`feedback_grammar_version_factoring`. Forking grammars per edition would 4× the maintenance for a model the
superset grammar + one pass already express correctly.

### 3.2 Edition-gating strategy — superset parse + committed-match construct-id annotation

**Principle:** the grammar recognises the union of all editions and never decides edition legality. A
parse-time reject would force the parser to re-derive the construct's identity after the fact (the
`EditionGateHints`/`ReservedWordEditionHints` reverse-signature trap, §1.4) — and a *predicate* cannot carry
the identity either, because ANTLR evaluates hoisted predicates speculatively at the stuck token during
failing/exploratory parses, so a gating or stamping predicate fires on paths that never match and
mis-attributes the construct. The design therefore uses a **committed-match construct-id ANNOTATION**: a
parse *action* that runs only when its alternative has actually matched, storing the `ConstructId` in a
side table keyed by the parse context:

```csharp
// CobolParserCoreBase (target)
private readonly Dictionary<ParserRuleContext, ConstructId> _constructIds = new();
/// Committed-match annotation: invoked from a rule ACTION after the alternative matched.
protected void Construct(ConstructId c) => _constructIds[Context] = c;
public IReadOnlyDictionary<ParserRuleContext, ConstructId> ConstructIds => _constructIds;
```

Grammar sites carry the annotation, not a gate — every alternative parses at every `--std`:

```
    | occursDynamicClause {Construct(ConstructId.OccursDynamic);}   // parses at EVERY --std
    | allocateStatement   {Construct(ConstructId.Allocate);}
```

Edition legality is decided in exactly ONE place: the `VersionConformancePass` over the bound tree
(`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`). Bound nodes carry a `.Syntax`
back-reference; the pass reads the construct id (from the side table, or from a self-identifying bound
node), asks `ConstructRegistry.Check` (single-sourced from `constructs.json`), and emits the
COBOLNET0900-band diagnostic through `EditionSeverityPolicy`. Because the identity is never thrown away,
there is no reverse-signature recogniser: `ReservedWordEditionHints` does not exist in the end state, and a
new gated construct is self-registering (add the `constructs.json` row + the annotation; no empirical
signature step).

**The two load-bearing forward-detects.** Exactly two semantic predicates survive in the grammar, and both
are *disambiguation* devices (choosing between two parses that are each legal somewhere), not edition gates:

1. **`openClause` — `{is2002() || retryPhraseAhead()}?`.** `retryPhraseAhead()` is true iff, with `RETRY` at
   the lookahead in the OPEN-clause position, the following tokens form a complete retry tail
   (arithmetic-expression `TIMES` | `FOR?` arithmetic-expression `SECONDS` | `FOREVER`) AND at least one
   further candidate file-name token remains before the sentence terminator (`openFileSpec+` must stay
   satisfiable). Consequences: `OPEN INPUT RETRY FOREVER.` (no trailing name) stays a two-file-name list at
   85; `OPEN INPUT RETRY 5 TIMES F.` forward-detects (an integer can never be a file name); the genuinely
   ambiguous `OPEN INPUT RETRY FOREVER F.` resolves to file names below 2002 (`RETRY` is a legal §8.9 user
   word there) and to the phrase at ≥2002 (`is2002()` is true; the §8.9 funnel reserves RETRY). Fail-safe: a
   missed real gate degrades to a neutral parse error, never a wrong edition claim.
2. **The boolean-condition ENTRY — `boolExprAhead()`** (`CobolParserCoreBase.cs:69`), generalized to fire at
   all editions using operand-adjacency (the highest-scrutiny site: it shares the comparison DFA).
   Legality of the boolean operators themselves is decided by the pass
   (`Check(BooleanOperators2002)`).

No other `is85()/is2002()/is2014()/is2023()` predicate survives in the grammar.

### 3.3 Lexer & token organization — one word source, shared literal fragments

**3.3a Single source for context-sensitive words (D2).** A new declarative source
`tests/version-matrix/cobol-words.json` (sibling of the existing `reserved-words.json`) lists every
context-sensitive word with the facts all three consumers need:

```json
{ "word": "LENGTH", "token": "LENGTH", "userWordAt": [85,2002,2014,2023],
  "reservedFunnelAt": [], "note": "FUNCTION LENGTH / START WITH LENGTH" }
```

`scripts/gen-cobol-words.ps1` (extends the proven `gen-reserved-words.ps1` pattern) generates THREE outputs
from it:
1. `Parsing/CobolLexerWordSet.g.cs` — a committed `partial class CobolLexer` (NOT under `Generated/`, so
   `dotnet clean` keeps it) carrying the subscript-trigger set; a partial `.cs` is the minimal buildable form
   because ANTLR has no portable `@members` text-include and the set references token-type `int` constants on
   the generated lexer.
2. `Grammar/Core/CobolWords.g4` — a generated parser fragment holding TWO rules, `cobolWord` and
   `reservedGatedWord`, imported by `CobolParserCore.g4`.
3. It cross-checks against `reserved-words.json` (RW-1: a subscript-trigger-only word must be 2023-reserved,
   with exact reconciliation pins on both asymmetry sides) so the two sources cannot silently disagree; a
   violation fails generation (fail-hard, like the existing drift test).

**Reservation gating has TWO halves, and the gate set itself is DERIVED — there is no flag (kb/Work PB693).**
ISO §8.3.2.1 rule 1 is the whole rule: *"Reserved words shall not be used as user-defined words or
system-names."* `cobolWord` IS the user-defined-word slot, so **every** name-slot word that §8.9 reserves at some
edition must leave that slot at those editions — otherwise an operand list absorbs the word and the construct it
begins vanishes. Step 4b of `gen-cobol-words.ps1` therefore computes the gate set from `reserved-words.json`:
a `nameSlot` row whose word is reserved at >=1 edition is gated, full stop. The generator emits each gated word
twice, with opposite predicates:

- in `cobolWord` as `{userWordHere("W")}? W` — the word is a user word exactly where §8.9 leaves it free; and
- in `reservedGatedWord` as `{!userWordHere("W")}? W` — an alternative of the DEFINITION slots `dataName`,
  `programName`, `sectionName`, `paragraphName` and the SELECT file-name, so a DECLARATION naming the word still
  PARSES where §8.9 reserves it and `VersionConformancePass`'s funnel answers with the targeted COBOLNET0901
  ("'W' is a reserved word in COBOL-nnnn") instead of a raw COBOL0001 "no viable alternative". The funnel arm
  hangs on `VisitReservedGatedWord`, the RULE — so a NEW definition slot needs no C# at all.

**`userWordHere` is `reservedHere` plus the migration mode.** `--permissive` accepts what the edition removed,
and a word §8.9 took away is exactly that (`ConstructAvailability.Removed`), so under `--permissive` the gate does
not fire, the pre-reservation reading is restored and the 0901 comes back as a WARNING on a program that runs. A
gate that ignored the permissive axis would turn the whole class into parse errors no severity policy can
downgrade. `reservedHere` keeps its own meaning ("is this token the reserved keyword here") for `facilityWord`
and the SPECIAL-NAMES CRT/CURSOR clause guards, which must keep recognizing their clauses under `--permissive`.

**The gate has exactly ONE exclusion, and it is derived too: the §15 intrinsic function names that collide with a
reserved word** (the `functionName` rule in `Grammar/Core/CobolExpressions.g4` — of the name-slot rows, LENGTH,
NATIONAL and BIT). A `cobolWord` occurrence of one of these is the KEYWORD-OMITTED function reference §15 permits
(`COMPUTE N = LENGTH(A)` reaches the name through `cobolWord`, not through a FUNCTION-led rule) — a use OF the
reserved word, the same distinction `IsBareFunctionArgumentWord` draws for §15 phrase words. Gating them turned
five conforming 2023 goldens into COBOL0001. They also carry no swallow risk: a function name leads no statement
and no clause.

⛔ **A KEYWORD SLOT MAY NOT BORROW `cobolWord` (kb/Work PB693).** The gate gives `cobolWord` one meaning —
*user-defined word* — so a rule that used it to match a reserved KEYWORD that happens to carry a lexer token is
broken at exactly the editions §8.9 reserves that word, and the parse-error path below then prints a FALSE
"'W' … cannot be used as a user-defined word" over a slot where the program never used it as a name. One rule
did: `validateStatusStage` (`Grammar/Core/CobolDeclined.g4`), the VALIDATE-STATUS clause's ON phrase, whose three
choice indicators FORMAT / CONTENT / RELATION are all UNDERLINED in §13.18.62.2 — keywords (§5.2.2), not names.
FORMAT is §8.9-reserved from 2002 and does have a token (the FD FORMAT clause, §13.18.24), so `ON FORMAT …`
became a parse error at 2002+ instead of the declined-facility COBOLNET1708. **The fix is a token arm in the
BORROWING rule, never an exclusion from the gate** — the word is reserved, and every other slot must keep
rejecting it. **⛔ THE SWEEP THAT FOLLOWED THAT SENTENCE WAS SCOPED WRONG, AND THE CORRECTION IS kb/Work PB704.**
It read: "FORMAT is the ONLY §8.9-reserved word carrying a lexer token that any rule expects `cobolWord` to match
as a keyword. Every other keyword the grammar routes through `cobolWord` — LOCALE, ORDER, CLASSIFICATION,
ATTRIBUTE, UCS-4/UTF-8/UTF-16, RELATION, NONE, RECEIVED, COBOL — is §8.10 context-sensitive or unclassified by
the standard, has NO lexer token, arrives as IDENTIFIER, and no gate can reach it." Two halves of that are FALSE.
LOCALE, ORDER, COBOL, NESTED, SYSTEM-DEFAULT and USER-DEFAULT are §8.9-RESERVED, not context-sensitive
(`reserved-words.json`), and **"no gate can reach it" was never measured**: the reservation GATE inside
`cobolWord` cannot reach an IDENTIFIER, but the §8.9 FUNNEL can and does — `VisitCobolWord`'s `CheckedTokenTypes`
holds `IDENTIFIER` and screens it POSITION-BLIND, precisely because the 2023 additions all lex as IDENTIFIER. So
having no token is not safety, it is the SAME defect by the other route, and `SORT … WITH DUPLICATES IN ORDER`
(§14.9.40.2) was refused as a user-defined-word use at 2002/2014/2023 for two trains
(`feedback_reachability_is_measured_not_deduced`). **The sweep's real axis is "is this word §8.9-reserved at any
edition", never "does it carry a token"**, and the fix is the same either way: the word becomes a lexer token
with a `cobol-words.json` nameSlot row, so the reservation gate + `reservedGatedWord` keep the NAME slot correct
at every edition and the KEYWORD slot never enters `cobolWord`. ORDER took that shape at PB704 (retiring the
`orderTableAhead()` text predicate and the funnel's ORDER TABLE slot exemption with it). The words still routed
through `cobolWord` as keywords are LOCALE, CLASSIFICATION, the LC_ categories, SYSTEM-DEFAULT/USER-DEFAULT,
NESTED, COBOL, ATTRIBUTE, RELATION, UCS-4/UTF-8/UTF-16, NONE, RECEIVED; the unreserved ones are inert (the funnel
computes `RejectsAt` false), and each reserved one is held off the funnel by a NAMED exemption in
`VisitCobolWord` — a hand list, and therefore the next PB704. LOCALE's is load-bearing for a stated reason
(`IntrinsicBinder.KeywordWordOf` needs `LOWER-CASE(x LOCALE …)` to arrive as bare words), so retiring the list is
a design change, not a sweep. `computerAttributes`, the other word sink, is a raw `~(DOT | PROGRAM | CHARACTER)+`
token loop and never enters `cobolWord` at all.
**A REFERENCE to a gated word is answered by the parse-error path.** The gate leaves no name-slot alternative for
`DISPLAY CONSTANT.` at `--std 2002`, and a source that fails to parse never reaches the bound-tree funnel. So
`CobolErrorListener` asks the parser whether the offending token is reservation-gated (the generated
`CobolLexer.IsReservationGated` set) and still rejects at this edition, and re-codes the diagnostic to
COBOLNET0901 with the ONE message `ReservedWordSet.UserWordViolationMessage` owns. The cause is named in every
position — reference slots included, which the funnel deliberately never screened. It is named ONCE per
occurrence of the word: ANTLR raises TWO syntax errors on one offending token (the prediction failure and
`CobolErrorStrategy`'s recovery message), which read as two different sentences before the re-code and as the
same sentence twice after it — and §8.3.2.1 rule 1 is broken once by one occurrence.

⛔ **Why this is generated and not written by hand (kb/Work PB300, CLAUDE.md rule 5).** The second half used to be
a hand-written list of two words inside `CobolData.g4`'s `dataName`, paired with a hand-written
`ctx.COMMIT() ?? ctx.ROLLBACK()` extraction in the funnel — three places naming the same set. It had already
rotted: CRT and CURSOR were reservation-gated by kb/Work PB301 and added to neither, so `01 CRT PIC X.` at
`--std 2002` answered a parse error that never named §8.9. Then the GATE SET itself was a hand-set
`reservationGated` row property, and **fifty-one further §8.9-straddling words never got one** — UNLOCK among
them, so a period-less `UNLOCK F1` after a `MOVE` was swallowed as two more receivers and legal COBOL-2002 source
was rejected (kb/Work PB693). Deriving the set killed the flag: `gen-cobol-words.ps1` now THROWS if a
`reservationGated` property reappears in the JSON. `reservedGatedWord`'s alternatives are all single
tokens, so the funnel reads the subrule's own text and needs no list either. `PROCEDURE` stays a hand-written
`dataName` alternative ON PURPOSE — it is reserved at *every* edition and NC205A legally names a data item with
it, so it must never reach the funnel.

`CobolWordsDriftTests` asserts the generated grammar fragments match the JSON (parallel to the existing
`ReservedWordsDriftTests`), and `CobolWordsG4_ReservationGate_Is_Derived_From_Section89` RECOMPUTES the step-4b
derivation independently — from `reserved-words.json` and the `functionName` rule, never from the grammar it is
checking — then pins set-equality in BOTH directions against the emitted `cobolWord` and `reservedGatedWord`
gates, plus the confidence alignment the generator throws on (a gated word must be a high-confidence row, or the
grammar would reject where the conservative funnel stays silent). A word reserved at EVERY edition is gated too:
its `cobolWord` half is then unreachable by design — §8.9 never leaves it free — and the declaration half carries
the whole job. Result: adding a context-sensitive keyword — a
reservation-gated one included — is a **one-line JSON edit**; the lexer HashSet, the two parser rules, and the
reserved-word funnel can no longer silently disagree.

**3.3b SUBSCRIPT-mode vocabulary (D9 partial).** The SUBSCRIPT mode stays (the `x(i)` vs `(a+b)`
disambiguation genuinely needs lexer-mode context and cannot be cleanly expressed in the parser given
COBOL's grammar), but the *duplicated* literal/operator token bodies are factored into a shared lexer
fragment `fragment` rule set (`NUM_BODY`, `STR_BODY`, `NAT_BODY`, `BOOL_BODY`, `NAME_BODY`) referenced by
both the DEFAULT tokens and the `SUB_*` tokens, so `"-15.6"` / `N"AB"` tokenization rules exist once. This
removes the "keep SUB_* in sync with DEFAULT" hazard without touching the mode-switch strategy. The
binder-side subscript re-parse (`ReferenceResolver.SplitSubscriptTokens`) is addressed by the binder
rearchitecture (structured `Place` path segments), not here; the frontend's contribution is to preserve the
captured token run faithfully.

**3.3c Mode inventory** stays: DEFAULT, PICMODE, SUBSCRIPT, COMMENT_MODE (`CobolLexer.g4:497,651,726,782`).
No change to mode semantics; only the shared-fragment factoring in 3.3b.

**3.3d The ALL figurative's literal-1 (kb/Work PB71, 2026-08-18).** `figurativeConstant`'s Format-6 arm is
`ALL allLiteral`, where `allLiteral : allLiteralOperand (AMPERSAND allLiteralOperand)*` and
`allLiteralOperand : STRINGLIT | HEXLIT | NATLIT | BOOLLIT` — ONE arm for the four literal kinds §8.3.3.6.3 SR2
admits (alphanumeric plain or hexadecimal, national N/NX, boolean B/BX), and literal-1 "may be a concatenation
expression". Two load-bearing decisions: (1) `nonNumericLiteral` lists `figurativeConstant` BEFORE
`concatenationExpression`, because `ALL "A" & "B"` is genuinely ambiguous between "ALL over the concatenated
literal-1 AB" (legal, SR2) and "a concatenation whose first operand is the figurative ALL "A"" (illegal, §8.8.3.2
SR1) — ANTLR resolves a true ambiguity toward the lower alternative, so the legal reading wins, while `"X" & ALL
"A"` still parses as a concatenation and is rejected COBOLNET1541 by `ConcatFolder`. (2) Every consumer asks
`fig.allLiteral()` — the binder (`BoundAllLiteral.Of`, the category from `CobolLiteral.ClassOf`), `ConcatFolder`,
the boolean channel, INSPECT's SR3 screen, the legacy oracle — so a fifth literal kind is one grammar line and one
classifier arm; the former shape (`ALL STRINGLIT | ALL HEXLIT | ALL BOOLLIT`, tested token-by-token at five sites)
is what let `ALL B"1"` parse and die at run time. The version pass's `VisitFigurativeConstant` owns the
statement-scoped 2002 gate for a national/boolean literal-1, the §8.3.3 hexadecimal grouping check, the SR2
zero-length check (COBOLNET1648) and the §8.8.3.2 SR1 same-class check (COBOLNET1540) — the tree walk, as for the
bare literals.

**3.3e The computer paragraphs (kb/Work PB78, 2026-08-18).** `objectComputerParagraph : OBJECT_COMPUTER DOT
((computerName computerAttributes?)? objectComputerClause* DOT)?` — ISO §12.3.6.2's `[computer-name-1]` is optional
and the two clauses (`programCollatingSequenceClause | characterClassificationClause`) may follow the period in any
order (each at most once, §5.2.6.4 — a duplicate is COBOLNET1652 in the binder). Two load-bearing decisions: (1) the
'85 attribute SINK `computerAttributes : ~(DOT | PROGRAM | CHARACTER)+` (MEMORY SIZE / SEGMENT-LIMIT / WITH
DEBUGGING MODE — deleted 2002, gated by `VisitComputerAttributes`' token scan) stays BEHIND the name — a
`~(…)+` sink reachable without one would swallow the next paragraph header — and now stops at CHARACTER as well as
PROGRAM, so both standard clauses are recognized rather than eaten. (2) `characterClassificationClause : CHARACTER
{classificationAhead()}? cobolWord (…)` — CLASSIFICATION is not a token (a plain word at '85), so the arm is
predicated on the word after CHARACTER; the clause BINDS since kb/Work PB64 T5 (A.4.9 item 7 claimed; it was
parse-to-diagnose COBOLNET1518 until then — the LOCALE clause's shape), and its words are exempt from the §8.9
funnel exactly as the LOCALE clause's are. The name-less clause form is the 2002 relaxation of the '85 required-name
format (`computer-name-optional-2002`, `VisitObjectComputerParagraph`); `sourceComputerParagraph` keeps the same
shape (`((computerName computerAttributes?)? DOT)?`). The legacy oracle reads the clause list too.

### 3.4 Delete dead grammars; quarantine JSON/XML (D5)

- **Delete** `Grammar/CobolDialect.g4`, `CobolParserGenerics.g4`, `CobolParserJsonXml.g4`,
  `CobolParserOO.g4`, `CobolPreprocessor.g4` (all unreferenced; two are stale duplicates of live code).
- **Move** `inlineMethodInvocationStatement` (the one live rule) from `Core/CobolExtensionsJsonXml.g4` into
  `Core/CobolOO.g4` (its true home — 2023 OO); it parses at every `--std`, carries its construct-id
  annotation at the `statement`-rule dispatch site, and is gated by the `VersionConformancePass`.
- **Delete** `jsonStatement` / `xmlStatement` from the fragment and the two `{is2014()}? jsonStatement` /
  `xmlStatement` arms from `CobolParserCore.g4:716-717`, plus the `jsonXmlExceptionPhrases` rule, and the
  `JSON`/`XML`/`PARSE`/`PROCESSING`-as-JSON handling. **Delete** the now-empty
  `Core/CobolExtensionsJsonXml.g4`.
- **Lexer tokens:** `JSON`/`XML` tokens: keep `PARSE`/`PROCESSING` (they remain context-sensitive user words
  per `cobolWord`), but remove `JSON`/`XML` keyword tokens if no ISO construct uses them (they become plain
  IDENTIFIERs — verify no lexer rule else-branch depends on them). The vendor JSON/XML→COBOL0313 disposition
  lives in `CobolErrorStrategy` as a token-keyed vendor hint — it is a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate — so no recogniser is involved.
- **Frozen-oracle caveat:** the legacy `CobolSharp.Compiler` differential oracle also parses via this
  frontend. Confirm no legacy test asserts a JSON/XML *parse success*; the dossier states the JSON/XML binder
  path is loud-fail-by-name already, and the version-matrix `json-generate-2014` row exists only to prove the
  seam — that row is retired with the grammar.

### 3.5 Generated-parser build (D7 + D8)

Behavior is already correct (portable flat-output regen, fail-hard, `.gitignored` `Generated/`,
java+pwsh prerequisites — `Invoke-Antlr4CSharp.ps1`, `GenerateIfNewer.ps1`). Target changes are hygiene:

1. **Package name → MSBuild property.** Replace the hard-coded `$PackageName = 'CobolSharp.Compiler.Generated'`
   default (`Invoke-Antlr4CSharp.ps1:29`) with a value passed from the csproj:
   `<AntlrPackage>CobolNet.Frontend.Generated</AntlrPackage>`, threaded via `-PackageName $(AntlrPackage)` on
   the `Exec` call. This performs the D8 namespace rename with one property and removes every
   `using Core = CobolParserCore` alias in consumers.
2. **Delete the committed `.antlr/` caches** (`Grammar/.antlr/`, `Grammar/Core/.antlr/`) and add
   `**/.antlr/` to `.gitignore`. They are IDE build output; one caches a dead grammar.
3. **Narrow the SLL-bail catch** (`Frontend.cs:135`): `catch (Antlr4.Runtime.Misc.ParseCanceledException)`
   (thrown by `BailErrorStrategy`) plus `catch (RecognitionException)`. A `NullReferenceException` /
   `InvalidOperationException` from a buggy semantic predicate or lexer action now propagates as an internal
   compiler error (surfaced by the driver's top-level boundary — see DESIGN-driver.md) instead of being
   silently retried under LL.
4. Keep the two-stage SLL→LL strategy verbatim (proven; the design does not touch the prediction pipeline).
   `ZeroTokenRewriter`'s ALGORITHM is likewise untouched — but see §7 for the one thing that changed underneath
   it: what counts as an arithmetic parenthesis (fix-queue PB48).

### 3.6 Preprocessor pipeline (namespace + one contract fix)

The five preprocessor stages stay, in the proven order (`Frontend.Preprocess`, `Frontend.cs:75-108`):

```
StripNistArchiveMarkers → NormalizeToFreeForm(edition,permissive) → ConditionalCompilation
  → CopyProcessor(edition,permissive) → NistPreprocessor(if NIST) → TurnDirectiveProcessor
```

Target changes:
1. **Namespace** all five files `CobolSharp.Compiler.Preprocessor` → `CobolNet.Frontend.Preprocessor` (D8).
2. **Delete the per-stage edition-metadata/severity copies** (D4): `ReferenceFormatProcessor.EditionGates`
   and `CopyProcessor` currently re-implement the strict/permissive `Removed()` policy. They take an injected
   `IEditionSeverityPolicy` (from `Cobol.Net.Editions`) instead, so "removed = error strict / warning
   permissive" has ONE definition. The preprocessor keeps only its *reference-format/COPY-specific* gate rows
   (VCR 2/94), read from the shared `ConstructRegistry`.
3. **Keep** the `TurnDirectiveProcessor` line-count-neutrality assertion (`Frontend.cs:103-105`, hazard H3) —
   it is a real safety invariant for TURN anchoring; convert the `throw` into a recorded internal diagnostic
   (consistent with the top-level exception boundary), not a raw exception.
4. The preprocessor remains hand-written (not the dead `CobolPreprocessor.g4`); that grammar is deleted (§3.4).

### 3.7 Parse-tree consumption — a typed `Cst/` façade (D6)

**Contract problem:** the binder reaches into raw generated contexts with ~336 `GetText()` calls. Renaming a
grammar rule silently breaks dozens of string-keyed walks.

**Target:** a `Cst/` (Concrete-Syntax-Tree façade) namespace in `Cobol.Net.Frontend` that wraps the generated
contexts in **thin, typed, read-only accessors** — the ONE surface the binder consumes. Not a re-parse: each
façade type holds the generated context and exposes typed properties instead of positional `GetText()`:

```csharp
// Cst/DataDescriptionCst.cs  (wraps CobolParserCore.DataDescriptionEntryContext)
public readonly struct DataDescriptionCst(CobolParserCore.DataDescriptionEntryContext ctx)
{
    public int Level          => ParseLevel(ctx.levelNumber());        // was int.Parse(x.GetText())
    public string? Name       => ctx.entryName()?.GetText();
    public PictureCst? Picture => ctx.pictureClause() is {} p ? new(p) : null;
    public UsageCst? Usage    => …;
    public bool IsTypedef     => ctx.typedefClause() is not null;
    public SourceSpan Span    => SourceSpan.Of(ctx);                    // line/col for diagnostics
    …
}
```

The façade is generated-context-shaped (1:1 with grammar rules), so it does NOT re-introduce a parallel model
— it is the *typed reading discipline* over the parse tree. Migration is incremental (§8): the binder can mix
raw-context and façade access during transition; the end state is that `Grammar/Generated` types are
`internal` to `Cobol.Net.Frontend` and only `Cst/` types cross the assembly boundary. This makes a grammar
rule rename a **compile error in one façade file** instead of a silent `GetText()` drift, and it is the
enabling step for the binder's "consume a typed tree, not strings" reorg.

The `VersionConformancePass` does not consume the parse tree at all — it walks the *bound* tree (each bound
node carries a `.Syntax` back-reference for spans and construct ids), so it needs neither the façade nor the
generated visitor. The façade is for the *binder*.

### 3.8 Diagnostics model — one `Diagnostic` type

The frontend already has the good pieces: `Diagnostics/DiagnosticDescriptor` (a typed
`{Code, Severity, MessageTemplate}` record) and `DiagnosticDescriptors` (a registry:
`CBL0901`, `COBOL0301`, `COBOLNET0900`, …) — `Diagnostics/DiagnosticDescriptors.cs`. This is exactly the
model the *compiler* side lacks (its 163 codes are bare strings — the understandability-critique HIGH). The
target:

1. **`DiagnosticBag`** (frontend) becomes the ONE diagnostic collector for the whole pipeline; its
   `Diagnostic` carries `{DiagnosticDescriptor, SourceSpan, args}` (code + location + severity structured all
   the way to the CLI). It moves namespace to `CobolNet.Frontend.Diagnostics` (D8).
2. The compiler-side `EditionContext.Diagnostics`/`Warnings` `List<string>` accumulators are replaced by this
   `DiagnosticBag` (owned by DESIGN-edition-framework.md / DESIGN-driver.md; the frontend provides the type). The
   frontend's descriptor-registry pattern is the template the compiler's 163-code registry adopts.
3. `DiagnosticDescriptors` is the frontend's registry; construct/edition codes (the COBOLNET0900 band) are
   sourced from `Cobol.Net.Editions` so frontend and compiler emit identical wording (one message, two emit
   layers — the EditionGateHints remark's own goal, achieved structurally).

### 3.9 Error recovery

- The two-stage SLL(bail)→LL(recover) strategy stays.
- `CobolErrorStrategy` keeps its COBOL-intent heuristics (`GuessCobolIntent`, 19 rules) — these are genuinely
  useful and not duplicative. It carries no edition logic: edition diagnosis lives in the
  `VersionConformancePass` (§3.2), and the recogniser call (`CobolErrorStrategy.cs:113`) is gone with the
  recogniser. It hosts the token-keyed vendor JSON/XML→COBOL0313 hint (§3.4).
- The `[code] message` construction (`CobolErrorStrategy.cs:93-95`) is retargeted to build a structured
  `Diagnostic` (descriptor + span) rather than a pre-formatted string, so downstream layers keep structure.
- Recovery beyond the current sync-point behavior is out of scope for this rearchitecture (the battery does
  not exercise multi-error recovery quality; changing it risks the green net for no measured gain).

---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| delete | `Grammar/CobolDialect.g4` | — | dead: unreferenced by any C#/csproj/script (§1.1) |
| delete | `Grammar/CobolParserGenerics.g4` | — | dead: unreferenced (§1.1) |
| delete | `Grammar/CobolParserJsonXml.g4` | — | dead + non-ISO (§1.1) |
| delete | `Grammar/CobolParserOO.g4` | — | dead: stale duplicate of live `Core/CobolOO.g4` (§1.1) |
| delete | `Grammar/CobolPreprocessor.g4` | — | dead: stale duplicate of hand-written `Preprocessor/` (§1.1) |
| delete | `Grammar/.antlr/`, `Grammar/Core/.antlr/` | — | committed IDE build-output caches; one caches a dead grammar (§1.8) |
| split | `Core/CobolExtensionsJsonXml.g4` | `inlineMethodInvocationStatement` → `Core/CobolOO.g4`; rest deleted | separate the one live 2023-OO rule from the non-ISO JSON/XML; delete non-ISO (§3.4) |
| delete | `jsonStatement`/`xmlStatement` + arms in `CobolParserCore.g4:716-717` | — | non-ISO, 0 spec hits (hard invariant 6) |
| create | — | `tests/version-matrix/cobol-words.json` | single declarative source for context-sensitive words (§3.3a) |
| create | — | `scripts/gen-cobol-words.ps1` | generate lexer HashSet + `cobolWord` + drift-check vs reserved-words.json (§3.3a) |
| create | — | `Grammar/Core/CobolWords.g4` (parser fragment), `Parsing/CobolLexerWordSet.g.cs` (lexer partial class) | generated word-set outputs (§3.3a) |
| refactor | `Core/CobolLexer.g4:30-72` `_dataNameTokens` | consume generated word-set | delete the hand-synced HashSet (§3.3a) |
| refactor | `CobolParserCore.g4:26-73` `cobolWord` | import generated alternative list | delete the hand-synced rule (§3.3a) |
| refactor | `Core/CobolLexer.g4:726-776` SUBSCRIPT `SUB_*` | shared `fragment` bodies with DEFAULT tokens | one tokenization rule per literal shape (§3.3b) |
| delete | `Parsing/EditionGateHints.cs` (207 loc) | — | no recogniser exists: identity is carried by the committed-match annotation; gating in the `VersionConformancePass` (§3.2) |
| refactor | `Parsing/CobolParserCoreBase.cs` | add the committed-match `Construct(ConstructId)` action + `ConstructIds` side table; keep only the two forward-detects (`retryPhraseAhead()`, `boolExprAhead()`) | constructs name themselves on match; delete signature table + double-sourced dialect (§3.2, §1.5) |
| refactor | grammar `{isXXXX()}?` edition predicates | dropped — superset parse + `{Construct(ConstructId.X);}` annotation actions | every construct parses at every `--std`; the pass gates (§3.2) |
| refactor | `Parsing/CobolErrorStrategy.cs:113` | drop the recogniser call; emit structured `Diagnostic`; host the token-keyed vendor JSON/XML→COBOL0313 hint | edition diagnosis moves to the `VersionConformancePass`; keep structure (§3.2, §3.4, §3.8) |
| create | — | `Cobol.Net.Editions` assembly | shared lowest layer both Frontend + Compiler reference (§2 D4) — owned by DESIGN-edition-framework.md |
| move | `EditionGateHints` metadata / preprocessor severity copies | `Cobol.Net.Editions` (`ConstructRegistry`, `EditionSeverityPolicy`) | one edition-metadata + one severity source (§1.5, §3.6) |
| rename | generated ns `CobolSharp.Compiler.Generated` | `CobolNet.Frontend.Generated` (MSBuild property) | remove stale split + `using Core =` aliases (§3.5, D8) |
| rename | `Preprocessor/*.cs` ns `CobolSharp.Compiler.Preprocessor` | `CobolNet.Frontend.Preprocessor` | stale namespace on physically-moved files (§1.7, §3.6) |
| rename | `Parsing/*.cs` ns `CobolSharp.Compiler.Parsing`, `CobolSharp.Compiler.Generated` (base) | `CobolNet.Frontend.Parsing` | same stale-namespace cleanup (§1.7) |
| move | `Diagnostics/DiagnosticBag.cs` ns → `CobolNet.Frontend.Diagnostics` | become the ONE pipeline diagnostic bag | one diagnostic model (§3.8) |
| create | — | `Cst/` façade namespace (`DataDescriptionCst`, `StatementCst`, …) | typed narrow contract; retire ~336 `GetText()` walks (§3.7) |
| edit | `Frontend.cs:135` `catch (Exception)` | `catch (ParseCanceledException)` + `RecognitionException` | stop masking predicate/lexer-action bugs (§3.5) |
| edit | `Frontend.cs:16` stale banner; `:103-105` throw | correct banner; record internal diagnostic | doc drift + exception-boundary consistency (§1.7, §3.6) |
| edit | `Invoke-Antlr4CSharp.ps1:29`, csproj | `-PackageName $(AntlrPackage)` | package name in one MSBuild property (§3.5) |
| edit | `.gitignore` | add `**/.antlr/` | prevent re-committing IDE caches (§1.8) |

---

## 5. Migration plan (keep the battery green at every step)

Each numbered step is an independently-committable, individually-green change. Order chosen so no step
depends on a later one and the highest-risk items (word-set generation, predicate-drop migration) come after the safe
deletions build confidence.

**M1 — dead-grammar + cache deletion (zero-risk).** Delete the 5 unreferenced `.g4` files and the `.antlr/`
caches; add `**/.antlr/` to `.gitignore`. Nothing generates or imports them → build + full battery unchanged.
Verify: `dotnet build` regenerates identically; the full battery (conformance + legacy guard) green.

**M2 — JSON/XML quarantine.** Move `inlineMethodInvocationStatement` into `Core/CobolOO.g4`; delete the
JSON/XML rules, the two `statement` arms, `jsonXmlExceptionPhrases`, and the empty fragment; remove the
`EditionGateHints` JSON/XML arm (its COBOL0313 disposition relocates to `CobolErrorStrategy` as a
token-keyed vendor hint, §3.4); drop the `json-generate-2014` version-matrix row. Verify: inline-invoke
golden still parses; no conformance test asserted a JSON/XML parse success (grep the test corpus first); full
guard green. This is the hard-invariant-6 close.

**M3 — build hygiene.** Package name → MSBuild property (performs the `Generated` namespace rename); narrow
the `Frontend.cs` catch; fix the stale banner + the line-count `throw`→diagnostic. The namespace rename
touches every `using Core =` consumer — a mechanical, compiler-verified sweep. Verify: clean build on
Windows AND WSL/Linux (portability is the known risk area); battery green.

**M4 — namespace cleanup (preprocessor + parsing).** Rename the `CobolSharp.Compiler.*` namespaces on the
already-moved frontend files to `CobolNet.Frontend.*`. Pure rename; compiler-verified. Do M3+M4 together if
convenient (both are namespace sweeps). Verify: battery green.

**M5 — single-source word set.** Create `cobol-words.json` from the current `_dataNameTokens` + `cobolWord`
contents (mechanical extraction), write `gen-cobol-words.ps1` + the `CobolWordsDriftTests`, wire the
generated fragments into the lexer/parser, delete the hand-maintained HashSet + rule. The drift test and the
full parse of the conformance corpus prove byte-identical tokenization. Highest-value dedup; medium risk
(tokenization changes are wide) — land it alone, guard-fast + full guard.

> The word set is single-sourced per §3.3a: `tests/version-matrix/cobol-words.json` → `scripts/gen-cobol-words.ps1`
> → the `Grammar/Core/CobolWords.g4` parser fragment (the imported `cobolWord`) + the committed
> `Parsing/CobolLexerWordSet.g.cs` lexer partial (the subscript-trigger set), cross-checked by RW-1 and guarded by
> `CobolWordsDriftTests`. `.tokens` output is byte-identical to the retired hand-synced sources.

**M6 — `Cobol.Net.Editions` extraction.** (Coordinated with DESIGN-edition-framework.md — that doc owns it; the
frontend's part is to consume `EditionInfo`/`ConstructRegistry`/`EditionSeverityPolicy` and delete the
preprocessor metadata/severity copies.) Verify: version-matrix accept/reject unchanged across all four
`--std` values.

**M7 — superset migration: drop the edition predicates.** After M6 (so `ConstructId`/`EditionInfo` exist):
migrate the version-gated constructs in small batches (each batch = one commit, one regen, one guard-fast —
the `feedback_grammar_version_factoring` discipline): drop the `{isXXXX()}?` predicate, add the
committed-match construct-id annotation, and gate at a bind-side `ConstructRegistry.Check` site until the
`VersionConformancePass` skeleton funnels all `Check` sites (the residue-first sequencing, batch record, and
pass skeleton are owned by `DESIGN-version-conformance-pipeline.md` §5 Stages 1–3). The two load-bearing
forward-detects (`retryPhraseAhead()`, the `boolExprAhead()` ENTRY) are the only predicates that remain.
When the last batch lands, delete the reverse-signature recogniser (`ReservedWordEditionHints`); the vendor
JSON/XML COBOL0313 disposition relocates to `CobolErrorStrategy` as a token-keyed hint. Verify per batch:
the version-matrix rows for those constructs still emit COBOLNET0900 with the same wording; the
duplicate-diagnostic case the dossier reproduced is now single. This is the biggest correctness win and the
most steps — do it incrementally, never as one batch.

**M8 — `Cst/` façade.** Introduce the façade types alongside the binder rearchitecture (this step is paced
by the binder work, not the frontend). Migrate binder `GetText()` sites to façade accessors incrementally;
when a rule's consumers are all on the façade, that rule's context can be considered migrated. End state:
generated contexts `internal`, `Cst/` the only cross-assembly surface. Verify continuously: the binder's
output bound tree is unchanged (differential oracle + conformance).

**Rollback posture:** M1–M4 are trivially revertible. M5/M7 are the risk points — each is gated behind its
own drift/matrix test and the full legacy guard, and each is landed as the *only* change in its commit so a
regression bisects to one step.

---

## 6. Risks

- **R1 (M5, high-blast):** the generated word-set could tokenize differently from the hand-maintained
  HashSet if the extraction misses a word (e.g. a word present in `cobolWord` but absent from
  `_dataNameTokens` today, which would be a *latent existing* asymmetry the generation would expose).
  Mitigation: the extraction script diffs the two current sources and fails on asymmetry, forcing the owner
  to resolve it as a real spec question before generation; the drift test + full-corpus parse are the net.
- **R2 (M7, correctness):** dropping a predicate changes the parse space, and the two surviving
  forward-detects are the delicate sites — `boolExprAhead()` shares the comparison DFA, and
  `retryPhraseAhead()` must resolve the OPEN name-list collision exactly per §3.2 (a missed real gate must
  degrade to a neutral parse error, never a wrong edition claim). Mitigation: one batch per commit with the
  FULL legacy guard; the version-matrix negative fixtures (every gated construct probed below its edition)
  are the regression net; annotation actions run only on committed matches, so speculative parses cannot
  mis-name a construct.
- **R3 (M2, oracle):** the frozen legacy oracle parses via this frontend; deleting JSON/XML could break a
  legacy test that parsed (even if it loud-failed to bind). Mitigation: grep the legacy + conformance corpus
  for JSON/XML source before M2; the dossier indicates the seam was never a passing feature.
- **R4 (M3, portability):** the package-name property + regen must work identically on Windows and Linux
  (the flat-output hazard). Mitigation: the existing portable-regen logic is untouched; only the
  `-package` argument value changes; verify on WSL per `reference_wsl_linux_repro.md`.
- **R5 (M8, scope creep):** the `Cst/` façade could balloon into a second data model. Mitigation: façade
  types are strictly 1:1 with grammar rules and hold the context (no computed/semantic state) — enforced by
  code review; semantic facts belong to the binder's model, not the façade.
- **R6 (cross-doc coupling):** M6/M7 depend on `Cobol.Net.Editions` (DESIGN-edition-framework.md). If that assembly
  slips, M7 blocks. Mitigation: M1–M5 + M8 are independent of it and deliver most of the frontend cleanup;
  sequence M6/M7 after the editions assembly lands.

---

## 7. What explicitly does NOT change (and why)

- **The single SUPERSET-grammar model** (one grammar, no per-edition forks) — correct and cheapest for
  four-editions-in-one; edition legality is the `VersionConformancePass`'s job, and we remove duplication
  *around* the grammar, not the one-grammar model itself.
- **The two-stage SLL→LL parse** — proven, subtle, and not a smell; untouched.
- **`ZeroTokenRewriter`'s algorithm** — untouched, but ⛔ **its INPUT vocabulary changed and the line that stood
  here ("proven … untouched") was hiding a defect** (fix-queue PB48). The pass reads adjacency to `(` / `)` as
  proof that a figurative ZERO sits inside parenthesized arithmetic. An argument list is delimited by those same
  characters, so every bare `ZERO` argument was converted to an arithmetic zero before any function was known and
  `FUNCTION LOWER-CASE(ZERO)` was rejected as class numeric — legal source, refused. ISO §8.4.3.2.3 SR6 makes the
  argument-list paren categorically not a grouping paren, so the LEXER now types it `FNARG_LPAREN`/`FNARG_RPAREN`
  from the `_fnParenStack` it already maintains, and the rewriter's rule became true as written. The pass also
  gained the reference-modification `COLON` as arithmetic context (§8.4.3.3.3 SR4 — both positions are arithmetic
  expressions), because a ref-mod written directly after a function name is delimited by FNARG parens too.
  ⚠ `refModPart` accepts BOTH paren flavours: SR6's precondition is "if a function's definition **permits
  arguments**", a catalog question no lexer can answer, so after a zero-argument name that token is the ref-mod.
  ⚠ **The rewriter is still the standing answer to "where does figurative ZERO become arithmetic", and it is
  still incomplete** — a bare `COMPUTE X = ZERO` and a `TB(ZERO + 1)` subscript are the positions no adjacency
  arm reaches (`kb/Work/PB51.md`, `kb/Work/PB50.md`). Retiring the pass in favour of a `primaryExpression`
  alternative is the structural candidate recorded there, with the ambiguity survey it requires.
- **The five preprocessor stages and their order** — correct per the TURN-anchoring hazard analysis; only
  namespaces + the injected severity policy change.
- **SUBSCRIPT lexer mode existence** — the `x(i)`/`(a+b)` disambiguation genuinely needs it; we dedup its
  token bodies, not its strategy.
- **`CobolErrorStrategy`'s 19 COBOL-intent heuristics** — useful, non-duplicative; only the recogniser
  call is removed (edition diagnosis lives in the `VersionConformancePass`; the token-keyed vendor
  JSON/XML hint is hosted here).

---

## 8. Open questions for the owner

1. **JSON/XML final disposition (M2).** Confirm hard-delete of the JSON/XML grammar + the `json-generate-2014`
   version-matrix row now (this design's assumption per hard-invariant 6),
   versus keeping the dead rules quarantined behind a permanent `--enable-vendor-json` flag for a possible
   future vendor-dialect mode. Recommendation: hard-delete; re-add as a real subsystem if ever scoped.
2. **`Cobol.Net.Editions` assembly boundary (M6).** This design assumes a new lowest assembly both Frontend
   and Compiler reference. If the owner prefers to keep the registry in `Cobol.Net.Compiler` and instead
   *inject* the metadata into the frontend at parse time (avoiding a new assembly), M7's construct-id
   annotation still works — `ConstructId` is the only shared type the grammar needs; introduction editions
   are read solely by the `VersionConformancePass`. Which dependency shape?
   (DESIGN-edition-framework.md needs the same answer.)
3. **`Cst/` façade timing (M8).** The façade is most valuable *with* the binder rearchitecture and risky to
   land before it. Should the frontend ship M1–M7 first and defer M8 to the binder track, or co-develop the
   façade as the binder's first refactor? Recommendation: defer M8 to the binder track; it is enabling
   infrastructure for that work, not standalone frontend cleanup.
4. **Generated-namespace rename timing (M3 vs G8).** This design brings the `Generated` namespace rename
   forward (it is free and deletes the `using Core =` aliases). Confirm this does not conflict with the
   planned G8 big-bang cut-over of the *legacy* `CobolSharp.Compiler` assembly (they are independent — this
   renames only the frontend's generated code — but the owner tracks G8 holistically).
5. **SUBSCRIPT-mode elimination — D10 = FULLY REMOVE (§9 below is the design).** The owner ruled the SUBSCRIPT
   mode + the binder subscript re-parse are removed rather than deferred; the token-body dedup (§3.3b) is in the
   tree, and the removal itself (**§9**) is scheduled for PHASE 15 §"CUT 2.5" (it cannot land while the frozen
   legacy compiler shares the `SUB_*` grammar — §9.3). It still needs ONE owner decision — the space-separator
   question (§9.4) — resolved before the §9.5 stages run.

---

## 9. D10 — SUBSCRIPT-mode removal (owner override): design + the one open decision

> **Status: DESIGN — scheduled for PHASE 15 §"CUT 2.5", sequenced immediately AFTER PHASE 15 Cut 2 deletes the
> legacy `src/CobolSharp.*` tree — the event that clears the §9.3 entanglement (the frozen legacy compiler is the
> sole remaining consumer of `SUB_*`/`SubscriptEntryContext`). The executing session resolves the §9.4 decision
> FIRST, then runs the §9.5 D10.1–D10.5 stages. This is a MAJOR multi-stage rearchitecture sub-track (a
> shared-grammar + ~250-line binder-parser rewrite); it is not doable while the legacy compiler shares the
> grammar, which is why it lands at PHASE 15 rather than earlier.**

### 9.1 Goal
Replace the lexer **SUBSCRIPT mode** (`CobolLexer.g4` — entered via `LPAREN` after a data-name token, emits the
`SUB_*` token family, popped by `SUB_RPAREN`) and the **flat uninterpreted stream** it feeds
(`subscriptOrRefMod : subToken+`, `CobolParserCore.g4`) — which the binder RE-PARSES by hand
(`ReferenceResolver.InterpretSubscripts` / `HasDepth0Colon` / `SplitSubscriptTokens` / `RenderSegment`, and the
~250-line recursive-descent arithmetic parser over `SUB_*` in `StatementBinder.Intrinsics.cs`) — with **real
grammar parse nodes**: a subscript list, a ref-mod, and a FUNCTION argument list parsed as `arithmeticExpression`
/ the existing `argumentList`, so the binder walks bound nodes instead of re-lexing a token soup. This kills a
whole hand-rolled parser (the biggest single simplification left in the frontend) and unifies the two arg-capture
paths (`subscriptPart` vs `inlineMethodInvocationStatement`'s `argumentList`) into one (singular-pattern).

### 9.2 Why the mode exists — the two hard constraints
The mode is not gratuitous; it solves two problems a naïve DEFAULT-mode rule reintroduces:
- **The LPAREN ambiguity.** `(` after a data-name is a subscript / ref-mod / function-argument opener; `(`
  elsewhere is arithmetic grouping. The mode disambiguates by the *preceding* token
  (`PreviousTokenCouldBeDataName` over the generated `_dataNameTokens` set). A grammar-level rule must recover this
  another way (the same ambiguity the `{is2023()}? inlineMethodInvocationStatement` predicate already fights).
- **⚠ Separator loss (the blocker).** DEFAULT mode `-> skip`s `WS` and `COMMA_SEP`. But ISO/IEC 1989:2023 §8.3.5
  admits a **space** as a subscript / argument separator: `X (I J)` and `MAX (A B)` are legal, alongside the comma
  forms `X (I, J)`. The SUBSCRIPT mode keeps `SUB_WS` a real token so `SplitSubscriptTokens` can split
  space-separated operands. Once WS is skipped, `X(I J)` (two subscripts) and `X(I J)` is indistinguishable from a
  malformed single expression, and — worse — there is no token boundary to split on. A pure comma-only grammar
  rule cannot parse the space-separated forms.
- **⚠ Sign adjacency.** `+1` / `-15.6` (a signed literal — `SIGNED_INTEGERLIT` / `SIGNED_DECIMALLIT`, sign
  ADJACENT to the digits) vs `+ 1` / `- 1` (a *relative* subscript offset — `SUB_PLUS SUB_WS SUB_INTEGERLIT`) are
  today distinguished LEXICALLY by the presence of `SUB_WS`. With WS skipped, `I+1`, `I + 1`, and `+1` all lex
  identically, so the relative-offset-vs-signed-literal distinction and the `-15.6` fraction-drop hazard
  (`CobolLexer.g4` SIGNED_DECIMALLIT must precede SIGNED_INTEGERLIT) can no longer be re-derived from tokens alone.

### 9.3 ⚠ Entanglement with the frozen legacy compiler
The old **structured** subscript rules `subscriptList / subscriptEntry / subscriptQualification / relativeOffset`
(`CobolParserCore.g4`) are dead in the GRAMMAR (rooted at the unreferenced `subscriptList`) — BUT the generated
`CobolParserCore.SubscriptEntryContext` type is still consumed by the **frozen legacy** compiler
(`CobolSharp.Compiler/…/ExpressionBinder.cs:1306 BindSubscriptEntry`). So the planned "D10.0 delete the 4 dead
rules, byte-neutral, land immediately" is **NOT byte-neutral** — it breaks the legacy build (verified: `CS0426`).
The `SUB_*` tokens are likewise shared with the legacy path. **Consequence:** the SUBSCRIPT machinery cannot be
fully removed from the SHARED grammar until the frozen legacy compiler is retired (**PHASE 15 / G8**), unless D10
is willing to (a) modify the frozen oracle (against its "differential net until cut-over" purpose) or (b) fork the
grammar so greenfield and legacy diverge (against singular-pattern). This re-sequences D10 to sit AFTER — or to
land ON — the G8 cut, not inside PHASE 04's byte-neutral window.

### 9.4 ⛔ THE OPEN DECISION (owner) — does COBOL.NET preserve ISO §8.3.5 space-separated lists?
This is the gating question; §9.5's staging depends on the answer.
- **Option A — spec-faithful (recommended): keep space-separated subscript/argument lists.** Then a **scoped
  WS-significance mechanism is unavoidable** (an island-grammar region, or a `WS`-non-skipping lexer predicate
  active only inside a data-name's `(...)` tail). "Full removal of the mode" then *reduces* to: **replace the flat
  uninterpreted `SUB_*` stream + the hand-rolled C# re-parsers with INTERPRETED grammar rules**, while retaining a
  minimal WS/`sign`-adjacency mechanism. This achieves the owner's REAL goal (real parse nodes, delete the ~250-line
  re-parser, unify arg capture) — it does not achieve a literal "zero lexer mechanism," because the spec forbids it.
- **Option B — narrow the language: require commas.** The mode can be fully removed, but `X(I J)` / `MAX(A B)`
  become parse errors — a **spec violation** the NIST corpus + INV-1-strong would flag. Not recommended.
- **Option C — interpret in-mode.** Keep the mode but give it real grammar rules (parse `SUB_*` structurally
  instead of the flat `subToken+`), deleting only the C# re-parsers. Smaller, but leaves the mode.

**Recommendation: Option A** — it honors the spec (hard-invariant 3) and still deletes the hand-rolled parsers,
which is the substance of the owner's directive. Frame "fully remove" as "remove the uninterpreted flat-stream +
C# re-parse," retaining the smallest possible lexer assist the spec compels.

### 9.5 Staged plan — executed as PHASE 15 §"CUT 2.5", after Cut 2 (each stage: greenfield battery + `guard.ps1` + INV-1-strong)
These D10.1–D10.5 stages ARE the PHASE-15 §"CUT 2.5" step list. Sequenced because the LPAREN mode-entry is an
all-or-nothing decision — the interpreted rules cannot coexist with the live mode, so the mode flips as the last grammar
step (D10.5), and (per §9.3) that flip requires the legacy tree already deleted — which is why the whole sub-track runs
AFTER PHASE 15 Cut 2 (§9.3 resolved). By the time D10.5 runs, the legacy `guard.sh`/353-MATCH net is itself retired, so
the verification metric is the greenfield `guard.ps1` + the D10.1 corpus + INV-1-strong.
- **D10.1** — this design note (done) + the §9.4 decision + a NEW characterization/conformance corpus exercising
  every enumerated form (multi-subscript space/comma lists, relative offsets, signed literals, ref-mod, qualified
  subscripts, nested FUNCTION args, string/national/boolean args, `table(ALL)`) captured BEFORE the change.
- **D10.2** — converge ALL ref-mod onto the existing DEFAULT-mode `refModPart` path; delete the ref-mod branch of
  `InterpretSubscripts`.
- **D10.3** — introduce the interpreted subscript grammar rule (per §9.4's answer) + rewrite
  `ReferenceResolver`'s subscript interpreters over real nodes.
- **D10.4** — MOSTLY PRE-EMPTED by P7 Step 12 (FUNCTION arguments already parse as real trees via
  `functionArgList` + the lexer's FUNCTION suppression / `SIGNED_*` twins / `FNARG_SEPARATOR`; the recursive-descent
  `SUB_*` parser is deleted; Udf/keyword-omitted route through ONE `BindArgOperand`). Residual: reunify
  `functionArgList` with `argumentList`, and re-home the keyword-omitted D2 fragment re-parse onto D10.3's
  interpreted subscripts.
- **D10.5** — delete the SUBSCRIPT-mode block + the `LPAREN` entry action + `PreviousTokenCouldBeDataName` +
  reconcile the Group-A drift test (the `subscriptTrigger` column goes dead) — **gated on §9.3** (legacy retirement
  / G8 coordination, since `SUB_*`/`SubscriptEntryContext` are legacy-shared).

**Metric:** token equivalence is NOT the goal (tokens change by design) — prove OUTPUT/behavior equivalence via the
D10.1 corpus + greenfield battery + FULL legacy guard 353 MATCH + INV-1-strong 349/349 at every stage.
