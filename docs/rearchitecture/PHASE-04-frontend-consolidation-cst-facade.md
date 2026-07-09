# PHASE 04 — Frontend Consolidation: Generated Word-Set + Typed Cst Façade

- **Phase:** P4
- **Track:** rearchitecture
- **Risk:** MEDIUM
- **Depends on:** P2 (Cobol.Net.Editions leaf assembly). Transitively assumes P1 is done (mechanical namespace
  rename `CobolSharp.Compiler.* → CobolNet.*`, dead-grammar deletion, JSON/XML removal). Groups A–C
  (word set / fragments / façade) may run **parallel to P3** (the version-conformance pipeline); the
  version-conformance leg (Group D) sequences AFTER P3's residue migration. After P3 the only surviving grammar
  predicates are the two load-bearing forward-detects (the `openClause` `{is2002() || retryPhraseAhead()}?` and
  the `boolExprAhead()`-based boolean-condition ENTRY) — this phase must **NOT re-introduce edition predicates**.
- **Design source:** `docs/rearchitecture/DESIGN-frontend-grammar.md` — this phase executes **D2** (generate the
  context-sensitive word set), **§3.3b** (share SUBSCRIPT-mode literal/operator token fragment bodies), and
  **D6/M8** (the typed `Cst/` façade) — plus the version-conformance leg from
  `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`: **superset-grammar completion** and the
  **committed-match construct-id annotation convention** (grammar actions + side-table storage keyed by parse
  context) that the `VersionConformancePass` reads. It explicitly does NOT execute D3/D4/D5/D7/D8 (those are
  P1/P2/P3).

> ⚠ **OWNER OVERRIDE — D10 (2026-07-07), SCOPE EXPANSION beyond this doc's original §3.3b.** This phase originally
> only DEDUPED the lexer `SUBSCRIPT` mode's token fragment bodies. The owner ruled it must **FULLY REMOVE** the
> `SUBSCRIPT` lexer mode AND the binder subscript re-parse, replacing them with a proper grammar-level subscript
> (`x(i)`) rule — a larger change reaching into the binder/data-model, not just the frontend. Add it as an
> explicitly-sequenced sub-track of this phase (or a dependent follow-on phase) with its OWN before/after
> characterization proof (Phase 00 net), and do NOT let it destabilize this phase's other exit criteria. See the
> master plan §6 (D10) + its status banner. This likely needs a fresh design note appended to
> `DESIGN-frontend-grammar.md` (and possibly `DESIGN-binder-bound-tree.md`) before execution — author it then.

## STATUS
`NOT STARTED`
<!-- The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`.
     Keep a one-line note per completed commit boundary in the "Execution log" at the bottom. -->

### Goal (one paragraph)
The frontend core is a single **superset grammar** — every edition's constructs parse unconditionally, and
edition legality is decided at bind time by the `VersionConformancePass`
(`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`); the problem is the **duplication around
it**. This phase removes two duplications, installs one enabling façade, and completes the grammar side of the
version-conformance pipeline: (1) the context-sensitive word set
— the tokens that are keywords in context but legal user-defined words elsewhere — is today hand-synced across
THREE physically separate places (the lexer `_dataNameTokens` HashSet, the parser `cobolWord` rule, and the
compiler `ReservedWords` table) with a source comment literally instructing a maintainer to hand-mirror them;
we make it ONE generated artifact from a declarative `tests/version-matrix/cobol-words.json`, extending the
proven `gen-reserved-words.ps1` codegen, guarded by a drift test so they provably cannot desync. (2) The
SUBSCRIPT lexer mode re-declares its own `SUB_*` literal token bodies paralleling the DEFAULT-mode literals;
we factor the shared bodies into `fragment` rules so each tokenization shape exists once. (3) We introduce a
`Cst/` typed façade (thin, 1:1-with-grammar-rules wrappers over the generated ANTLR contexts) as the narrow
cross-assembly surface the binder consumes, and migrate the two highest-churn anchor consumers off raw
`GetText()` — making a grammar-rule rename a compile error in one file instead of a silent drift across ~339
`GetText()` sites. (4) We complete the **superset grammar** and install the **committed-match construct-id
annotation convention** — grammar actions that stamp each recognized edition-gated construct into side-table
storage keyed by parse context, the grammar-side feed the `VersionConformancePass` reads (per
`DESIGN-version-conformance-pipeline.md`). **Groups A–C are behavior-neutral; the full battery stays green at
every commit boundary.**

### Exit criteria (all must hold at phase end)
1. The context-sensitive word set is **single-sourced** from `tests/version-matrix/cobol-words.json`: the lexer
   subscript-trigger set and the parser `cobolWord` rule are both generated, and a **`CobolWordsDriftTests`**
   proves lexer + parser + `reserved-words.json` cannot silently desync (a hand edit to any generated artifact,
   or a regen that touched only one, fails the test).
2. The SUBSCRIPT-mode literal/operator token bodies that have a DEFAULT-mode twin are defined **once** via
   shared `fragment` rules; the regenerated `CobolLexer.tokens` is byte-identical and the battery is green.
3. A `Cst/` typed façade exists for the **highest-churn rules** (`dataReference`, `dataDescriptionEntry`,
   `cobolWord`, `integerLiteral`) and the two anchor consumers (`ReferenceResolver`, `DataBinder.BindEntry`)
   read the façade, not raw `GetText()`.
4. **Full battery green + snapshots neutral:** greenfield conformance + unit + characterization + the FULL NIST
   legacy guard (ALL GREEN) + version-matrix accept/reject unchanged across all four `--std` values. The
   generated `.g.cs` for a representative corpus is byte-identical to pre-phase (behavior neutrality).
5. The **superset grammar is complete** and the **construct-id annotation convention** is installed (grammar
   actions + side-table storage keyed by parse context) per `DESIGN-version-conformance-pipeline.md`; **no
   edition predicate has been (re-)introduced** — the only grammar predicates are the two load-bearing
   forward-detects (the `openClause` `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based
   boolean-condition ENTRY).

---

## 1. Preconditions the executing session MUST verify FIRST

Run these before step 1. If any fails, STOP — an upstream phase is incomplete and this phase's assumptions do
not hold.

```bash
cd E:/CobolSharp

# P1 done: generated namespace renamed, dead grammars gone, JSON/XML gone.
grep -rl "namespace CobolNet.Frontend.Generated" src/Cobol.Net.Frontend/Generated/ \
  && echo "OK: generated ns renamed" || echo "STOP: P1 namespace rename not done (still CobolSharp.Compiler.Generated?)"
ls src/Cobol.Net.Frontend/Grammar/CobolDialect.g4 2>/dev/null \
  && echo "STOP: dead grammar still present (P1 incomplete)" || echo "OK: dead grammars removed"
ls src/Cobol.Net.Frontend/Grammar/Core/CobolExtensionsJsonXml.g4 2>/dev/null \
  && echo "NOTE: JSON/XML fragment still present — confirm P1 status" || echo "OK: JSON/XML fragment removed"

# P2 done: Cobol.Net.Editions assembly exists.
ls src/Cobol.Net.Editions/*.csproj 2>/dev/null \
  && echo "OK: Editions assembly present" || echo "STOP: P2 (Cobol.Net.Editions) not done"

# Baseline green before we touch anything.
dotnet build CobolSharp.sln -c Debug   # must succeed (this regenerates the parser)
```

> **IMPORTANT — namespace drift guard.** This document was authored while the tree still had the *pre-P1*
> generated namespace `CobolSharp.Compiler.Generated` (verified at `Generated/CobolLexer.cs:22`,
> `Parsing/CobolParserCoreBase.cs:5`). Every code snippet below that names a namespace uses the **post-P1**
> name `CobolNet.Frontend.Generated`. If the preconditions show P1 is NOT yet complete, either (a) coordinate
> to land P1 first (recommended — it is `depends_on: 2` for this phase's parent P1→P2 chain), or (b)
> substitute the actual current namespace everywhere. Do NOT proceed with a mixed assumption.

**Establish the neutrality baseline (do this once, before step 1):**

```bash
# 1. Byte-identical .tokens baseline (proves the lexer refactor changes no token type assignments).
cp src/Cobol.Net.Frontend/Generated/CobolLexer.tokens /tmp/CobolLexer.tokens.baseline

# 2. Emitted-C# snapshot baseline for a representative corpus (behavior neutrality across the whole phase).
#    Pick ~12 programs exercising subscripts, ref-mod, PIC/USAGE, OO, files. Emit .g.cs for each and archive.
mkdir -p /tmp/p4-baseline
for f in tests/conformance/**/*.cob ; do : ; done   # (enumerate your representative set; see §5)
# For each chosen program: dotnet <cli> <prog.cob> -o /tmp/out.dll ; cp its .g.cs sidecar to /tmp/p4-baseline/
```

---

## 2. Rationale — the problems this phase fixes (grounded in code)

### 2.1 The context-sensitive word set is triplicated and hand-synced (DESIGN §1.3)
The set of tokens that are keywords in context yet legal user-defined words elsewhere is maintained in three
places that must agree **by hand**:
- **lexer** `_dataNameTokens` HashSet — `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4:30-72` (drives
  SUBSCRIPT-mode entry via `PreviousTokenCouldBeDataName()` at `:74`).
- **parser** `cobolWord` rule — `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4:25-113` (admits the word in
  a name slot).
- **compiler** `ReservedWords` table — `src/Cobol.Net.Compiler/Validation/ReservedWords.Table.cs` (the §8.9
  reserved-word funnel).

`CobolLexer.g4:20-21` literally says *"It MUST mirror the parser's cobolWord rule … When adding a token to
cobolWord, add it here."* A silent desync mis-triggers or fails to trigger SUBSCRIPT mode → a
wrong-or-missing parse error with no diagnostic pointing at the cause. **The asymmetry already exists:**
`cobolWord` lists `BIT` (`CobolParserCore.g4:29`) but the lexer `_dataNameTokens` does NOT — a latent
under-trigger (safe failure mode: `BIT(1)` would lex as arithmetic grouping and error on first subscripted
use). Crucially, `ReservedWords.Table.cs` is **already generated** from `tests/version-matrix/reserved-words.json`
by `scripts/gen-reserved-words.ps1` (header at `ReservedWords.Table.cs:1-6`) and guarded by
`tests/Cobol.Net.Tests.Unit/ReservedWordsDriftTests.cs` — the codegen+drift discipline the other two copies
lack **already exists and is proven**. We extend it, not invent it.

### 2.2 SUBSCRIPT mode re-declares literal token bodies (DESIGN §1.9 / §3.3b)
SUBSCRIPT mode (`CobolLexer.g4:726-776`) re-declares `SUB_STRINGLIT`, `SUB_NATLIT`, `SUB_BOOLLIT`,
`SUB_INTEGERLIT`, `SUB_DECIMALLIT`, `SUB_IDENTIFIER` with bodies **character-identical** to the DEFAULT-mode
`STRINGLIT` (`:620`), `NATLIT` (`:626`), `BOOLLIT` (`:635`), `INTEGERLIT` (`:616`), `DECIMALLIT` (`:601`),
`IDENTIFIER` (`:609`). Two copies of each tokenization rule; a fix to string-escape handling must be applied
twice or they silently diverge.

### 2.3 The binder consumes the tree by string interpretation (DESIGN §1.6 / §3.7)
The `Cobol.Net.Compiler` binder hand-walks the raw ANTLR contexts with **~339 `GetText()` calls** across 31
files (verified: `grep -rc "GetText()" src/Cobol.Net.Compiler`). The dominant rules are `dataReference` (142
accessor sites), `cobolWord` (52), `integerLiteral` (19), `fileName` (15), `dataDescriptionEntry` (12). ANTLR's
`-visitor` output is generated (`Invoke-Antlr4CSharp.ps1:71`) but only `EditionValidator` uses it. The raw
parse-tree shape is thus a wide, un-narrowed, stringly-typed cross-assembly contract with no façade: a grammar
rule rename ripples into dozens of `GetText()` sites invisibly. A typed façade makes such a rename a
**compile error in one file**. This is enabling infrastructure for the P7 binder god-class split.

### 2.4 What this phase does NOT do (owned by other phases — do not re-author)
- Dead-grammar deletion, JSON/XML removal, generated-namespace rename → **P1**.
- `Cobol.Net.Editions` extraction, diagnostic-descriptor registry → **P2**.
- Edition-predicate residue migration to bind-time `Check`, `ReservedWordEditionHints` deletion, the
  `VersionConformancePass` skeleton, VCR audit → **P3** (per `DESIGN-version-conformance-pipeline.md`). This
  phase never adds an edition predicate; the only surviving grammar predicates after P3 are the two
  load-bearing forward-detects.
- Full elimination of the SUBSCRIPT lexer mode + the binder subscript re-parse (`ReferenceResolver`
  `SplitSubscriptTokens`) → deferred data-model change (DESIGN §8 open-question 5 / **P5–P7**).
- The binder god-class split and full migration of all ~339 `GetText()` sites → **P7**. This phase installs the
  façade and migrates the two anchor consumers only; internalizing the generated contexts is P7's end-state.

---

## 3. Target end-state for this phase (concrete)

Files that exist / are changed when this phase is DONE:

**Created:**
- `tests/version-matrix/cobol-words.json` — the single declarative source for context-sensitive words.
- `scripts/gen-cobol-words.ps1` — generator (extends the `gen-reserved-words.ps1` pattern) emitting two
  committed artifacts + a cross-check against `reserved-words.json`.
- `src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4` — **generated** parser fragment grammar containing the
  `cobolWord` rule (imported by `CobolParserCore.g4`).
- `src/Cobol.Net.Frontend/Parsing/CobolLexerWordSet.g.cs` — **generated** `partial class CobolLexer` holding
  the `_dataNameTokens` set (the subscript-trigger set).
- `tests/Cobol.Net.Tests.Unit/CobolWordsDriftTests.cs` — the drift guard (parallel to `ReservedWordsDriftTests`).
- `src/Cobol.Net.Frontend/Cst/` — `SourceSpan.cs`, `DataReferenceCst.cs`, `DataDescriptionCst.cs`,
  `CstExtensions.cs` (name/integer-literal helpers). The typed façade, namespace `CobolNet.Frontend.Cst`.

**Changed:**
- `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4` — `_dataNameTokens` HashSet removed from `@members`
  (moved to the generated partial); shared literal `fragment` rules added; DEFAULT + `SUB_*` tokens reference
  them.
- `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` — hand-written `cobolWord` rule deleted; `CobolWords`
  added to the `import` list.
- `src/Cobol.Net.Compiler/Binding/ReferenceResolver.cs` — internals read `DataReferenceCst` accessors.
- `src/Cobol.Net.Compiler/Binding/DataBinder.cs` (`BindEntry`) — reads `DataDescriptionCst`.

**Unchanged (explicitly preserved):** the two-stage SLL→LL parse, `ZeroTokenRewriter`, the SUBSCRIPT mode
existence + mode-switch strategy, `PreviousTokenCouldBeDataName()`, the two load-bearing forward-detect
predicates — the `openClause` `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based
boolean-condition ENTRY, the ONLY grammar predicates that survive P3 — the preprocessor, `CobolErrorStrategy`.

---

## 4. STEP-BY-STEP

> Ordering rationale: Group A (word set) is the highest-value dedup and lands first, as a
> parallel-SSOT-then-flip so each commit is provably neutral. Group B (fragment dedup) is an independent
> low-risk lexer refactor. Group C (façade) is enabling infra (it is paced with, and hands off to, P7).
> Group D (the version-conformance leg) sequences after P3's residue migration. Each numbered step names files,
> the exact change, why, the verify command + expected result, and whether it is a **COMMIT BOUNDARY**.

---

### GROUP A — Single-source the context-sensitive word set

#### Step A1 — Author `tests/version-matrix/cobol-words.json` by faithful extraction (NO behavior change)
**File (create):** `tests/version-matrix/cobol-words.json`

**What:** Mechanically extract the CURRENT membership of the two grammar sources into a declarative JSON. One
row per context-sensitive word with the facts all consumers need:

```json
{
  "_comment": "GENERATED-INPUT / hand-curated single source for the context-sensitive word set. Each word: 'token' = the lexer token name; 'nameSlot' = appears in the parser cobolWord rule (legal user-defined word); 'subscriptTrigger' = in the lexer _dataNameTokens set (a '(' after it enters SUBSCRIPT mode); 'note' = context. scripts/gen-cobol-words.ps1 emits Grammar/Core/CobolWords.g4 + Parsing/CobolLexerWordSet.g.cs from this; CobolWordsDriftTests asserts they agree and cross-checks reserved-words.json. CONTENT-FILTER RULE: never print this file into a conversation stream.",
  "words": [
    { "token": "IDENTIFIER",  "nameSlot": true, "subscriptTrigger": true,  "note": "the base user-defined word" },
    { "token": "LENGTH",      "nameSlot": true, "subscriptTrigger": true,  "note": "START WITH LENGTH / FUNCTION LENGTH" },
    { "token": "BIT",         "nameSlot": true, "subscriptTrigger": false, "note": "USAGE BIT; KNOWN latent asymmetry — in cobolWord but NOT in the lexer set today (safe under-trigger). Captured AS-IS for neutrality; see follow-up FU-1." }
    /* … every remaining word … */
  ]
}
```

**Extraction procedure (deterministic — do NOT eyeball):**
1. `nameSlot` set = every token alternative in `cobolWord` (`CobolParserCore.g4:25-113`), including
   `IDENTIFIER`. Extract with:
   `grep -oE "^\s*\|\s*[A-Z_][A-Z0-9_]*" src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` over the rule body
   (plus the `IDENTIFIER` first alternative), strip the `| `.
2. `subscriptTrigger` set = every token in `_dataNameTokens` (`CobolLexer.g4:30-72`). Extract the identifiers
   between the `new() {` and `};`.
3. **Reconcile the diff** (this is the point of the exercise, DESIGN R1): compute `nameSlot △ subscriptTrigger`.
   The KNOWN existing asymmetries (verify against the tree, do not assume):
   - `BIT` ∈ nameSlot, ∉ subscriptTrigger.
   - The `functionName`-collision tokens `DISPLAY, MERGE, RANDOM, SIGN, SORT, SUM` (`CobolLexer.g4:64`) ∈
     subscriptTrigger; they are NOT in `cobolWord` (they parse via `functionName`, not a name slot). So for
     these, `nameSlot=false, subscriptTrigger=true`.
   For EVERY word in the symmetric difference, record it AS-IS (preserve current behavior exactly) with a
   `note` explaining the asymmetry, and add it to the **follow-up ledger** (see §7 FU-1). Do NOT "fix" an
   asymmetry inside this neutral flip — a fix changes tokenization and would break the byte-neutrality invariant.
4. Sort rows by token name (stable, so regen diffs are readable).

**Why:** One declarative source that faithfully reproduces today's two sets is the prerequisite for generating
them; the reconcile step surfaces the latent asymmetries as explicit, tracked data instead of silent drift.

**Verify:** No build yet — this is data. Sanity-check counts:
```bash
grep -c '"token"' tests/version-matrix/cobol-words.json          # expect ~ (cobolWord alts ∪ lexer-set) size
```
**Not a commit boundary yet** (commit with A2–A5).

#### Step A2 — Write `scripts/gen-cobol-words.ps1`
**File (create):** `scripts/gen-cobol-words.ps1`

**What:** A PowerShell generator modeled on `scripts/gen-reserved-words.ps1` (same header/fail-hard discipline).
It:
1. Reads `tests/version-matrix/cobol-words.json`.
2. Emits **`src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4`** — a parser fragment grammar:
   ```
   // <auto-generated> by scripts/gen-cobol-words.ps1 — DO NOT EDIT; re-run the script.
   // Source: tests/version-matrix/cobol-words.json. CobolWordsDriftTests asserts agreement.
   parser grammar CobolWords;
   options { tokenVocab = CobolLexer; }
   // cobolWord: the context-sensitive user-word list (nameSlot=true rows).
   cobolWord
       : IDENTIFIER
       | LENGTH
       | …            // every nameSlot=true token except IDENTIFIER, in JSON order
       ;
   ```
3. Emits **`src/Cobol.Net.Frontend/Parsing/CobolLexerWordSet.g.cs`** — a partial class extending the generated
   lexer with the subscript-trigger set:
   ```csharp
   // <auto-generated> by scripts/gen-cobol-words.ps1 — DO NOT EDIT; re-run the script.
   // Source: tests/version-matrix/cobol-words.json. CobolWordsDriftTests asserts agreement.
   namespace CobolNet.Frontend.Generated;   // MUST match the generated CobolLexer namespace (post-P1)
   public partial class CobolLexer
   {
       // The subscript-trigger set (subscriptTrigger=true rows): a '(' after one of these enters SUBSCRIPT mode.
       private static readonly System.Collections.Generic.HashSet<int> _dataNameTokens = new()
       {
           IDENTIFIER,
           LENGTH,
           …            // every subscriptTrigger=true token, in JSON order
       };
   }
   ```
4. **Cross-checks** against `tests/version-matrix/reserved-words.json`: for any word that is a lexer token AND
   present in the reserved-words table, assert consistency of the intent (a `subscriptTrigger=true` word must be
   a legitimate user-word at ≥1 edition per the reserved-words flags, else fail — mirrors the reserved-words
   sanity gates). Fail-hard on any inconsistency (`$ErrorActionPreference='Stop'`, `throw`), never silently emit.
5. Reports **counts only** (respect the DEVLOG-578/585 content-filter rule — never print a word list into the
   conversation stream).

**Why:** Extends the proven, trusted codegen pattern; makes "add a context-sensitive keyword" a one-line JSON
edit that regenerates all three consumers.

> **Design note — why generate a partial `.cs` for the lexer set (not a `.g4` `@include`).** ANTLR has no
> portable text-include for `@members`, and the set references token-type `int` constants (`LENGTH`, …) that
> live on the generated `CobolLexer` class. A committed `partial class CobolLexer` file (compiled alongside the
> ANTLR output, same namespace) references them unqualified and is the minimal, buildable form — exactly
> parallel to how `ReservedWords.Table.cs` is a committed generated `.cs`. The generated lexer IS `partial`
> (verified: `Generated/CobolLexer.cs:33 public partial class CobolLexer : Lexer`), so the extra partial is
> legal. The parser `cobolWord`, being real grammar, is generated as an imported fragment `.g4`.

**Verify:**
```bash
pwsh -File scripts/gen-cobol-words.ps1     # expect: "cobol-words generated: nameSlot=NN subscriptTrigger=MM ..." exit 0
git status --short src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4 \
                   src/Cobol.Net.Frontend/Parsing/CobolLexerWordSet.g.cs   # both created
```
**Not a commit boundary yet.**

#### Step A3 — Prove the generated artifacts equal today's hand-maintained sources (SAFETY NET)
**File (create):** `tests/Cobol.Net.Tests.Unit/CobolWordsDriftTests.cs` (initial form)

**What:** Before wiring anything into the grammar, prove the JSON faithfully captured current membership. Two
assertions:
1. **Lexer set equivalence (runtime):** instantiate `CobolLexer`, reflect the private static
   `_dataNameTokens` field (it is still the *hand-written* one at this point — the generated partial is not yet
   wired in, so temporarily reference the generated partial's set under a distinct name, OR run this check after
   A4 wiring; see sequencing note). Map each token int back to its symbolic name via the lexer's `Vocabulary`,
   and assert the set equals the JSON `subscriptTrigger=true` token names.
2. **Parser rule equivalence (text):** read `CobolWords.g4`, extract the `cobolWord` alternatives, and assert
   the set equals the JSON `nameSlot=true` token names.
3. **Reserved-words cross-check:** assert every subscript-trigger token that is also a `ReservedWords` entry is
   user-legal at some edition (not reserved at all four), matching the JSON note.

**Sequencing note:** the cleanest safe order is to do A3's *text* checks now (they need no wiring), and add the
*runtime lexer-set* check in A4 after the generated partial replaces the hand-written set (so the check reads
the ONE real set). Structure the test file so the runtime assertion is added in A4.

**Why:** Parallel-SSOT — the JSON is proven a faithful capture before it becomes load-bearing.

**Verify:**
```bash
dotnet test tests/Cobol.Net.Tests.Unit --filter CobolWordsDriftTests   # GREEN
```
**Not a commit boundary yet.**

#### Step A4 — Flip the lexer to the generated set
**Files (edit):**
- `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4` — DELETE the `_dataNameTokens` HashSet initializer from
  `@members` (lines 30-72), keeping the surrounding `_lastNonWsTokenType`, `PreviousTokenCouldBeDataName()`
  (which still references `_dataNameTokens`), and `NextToken()`. Replace the deleted block with a one-line
  comment: `// _dataNameTokens is generated into Parsing/CobolLexerWordSet.g.cs from cobol-words.json.`
- `src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj` — ensure the committed generated `.cs` is compiled. It
  lives under `Parsing/` (globbed by the SDK by default) so typically no change is needed; confirm it is NOT
  under `Generated/` (which the `CleanGenerated` target wipes). It must survive `dotnet clean`.
- `tests/Cobol.Net.Tests.Unit/CobolWordsDriftTests.cs` — add the runtime lexer-set assertion (A3 item 1).

**What:** The lexer now gets its subscript-trigger set from the generated partial. `PreviousTokenCouldBeDataName`
is unchanged (same field name, same semantics).

**Why:** Removes one of the three hand-synced copies.

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug                                  # regenerates + compiles; must succeed
diff /tmp/CobolLexer.tokens.baseline src/Cobol.Net.Frontend/Generated/CobolLexer.tokens   # BYTE-IDENTICAL (no token change)
dotnet test tests/Cobol.Net.Tests.Unit --filter CobolWordsDriftTests  # GREEN incl. runtime lexer-set check
dotnet test tests/Cobol.Net.Tests.Conformance                         # GREEN (subscript programs unaffected)
```
Expected: `.tokens` identical (the set is C# runtime data, not a lexer token, so the ATN is unchanged); a
handful of subscript-heavy conformance programs (e.g. any using `TABLE(I)`) still parse and run identically.
**Not a commit boundary yet** (do A5 in the same commit — the parser flip is the other half).

#### Step A5 — Flip the parser to the generated `cobolWord`
**Files (edit):**
- `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` — DELETE the hand-written `cobolWord` rule
  (lines 25-113, keep the section banner comment pointing at `CobolWords.g4`). ADD `CobolWords` to the import
  list (line 16): `import CobolExpressions, CobolData, …, CobolOO, CobolScreen, CobolWords;`.
- Confirm `Invoke-Antlr4CSharp.ps1` stages `Core/*.g4` into `obj/antlr-lib/` (line 103: `Copy-Item Core/*.g4`) —
  `CobolWords.g4` is under `Core/`, so it is staged and resolvable as an import. No script change needed.

**What:** The parser's `cobolWord` now comes from the generated imported fragment. ANTLR merges imported rules;
because the root grammar no longer defines `cobolWord`, the imported one is used.

**Why:** Removes the second of the three hand-synced copies. The third (`ReservedWords`) was already generated
+ cross-checked; now all three derive from JSON and the drift test binds them.

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug                                  # regen resolves the CobolWords import; must succeed
dotnet test tests/Cobol.Net.Tests.Unit --filter CobolWordsDriftTests  # GREEN
bash scripts/guard-fast.sh                                            # greenfield suites green
# A name-slot smoke probe: a program declaring a context-sensitive word as a data name still binds.
cat > /tmp/wordprobe.cob <<'EOF'
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WORDPROBE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LENGTH   PIC 9(4) VALUE 12.
       01 SHARING  PIC X(3) VALUE "yes".
       PROCEDURE DIVISION.
           DISPLAY LENGTH " " SHARING.
           STOP RUN.
EOF
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /tmp/wordprobe.cob --std 85 -o /tmp/wp.dll --run
# expect: prints "0012 yes", exit 0 (LENGTH/SHARING accepted as user data names at 85)
```
**COMMIT BOUNDARY.** Suggested message:
```
refactor(frontend): single-source the context-sensitive word set from cobol-words.json (P4 group A)

The lexer _dataNameTokens set and the parser cobolWord rule are now GENERATED by
scripts/gen-cobol-words.ps1 from tests/version-matrix/cobol-words.json — killing the
triple hand-sync the CobolLexer.g4:20-21 comment used to instruct. CobolWordsDriftTests
proves lexer + parser + reserved-words.json cannot silently desync. Behavior-neutral:
CobolLexer.tokens byte-identical; full battery green. Known latent asymmetries (BIT,
functionName collisions) captured AS-IS with follow-up FU-1.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017kSL7aKj3FXensEvmEDfhs
```
Add a DEVLOG entry (newest-first, per `feedback_devlog`).

---

### GROUP B — Share SUBSCRIPT-mode literal token fragment bodies

#### Step B1 — Factor shared literal bodies into `fragment` rules
**File (edit):** `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4`

**What:** Introduce grammar-level `fragment` rules for the literal bodies that have a DEFAULT/SUBSCRIPT twin,
and reference them from both token rules. Fragments are mode-independent, so one body serves all modes.

Add near the DEFAULT literal section:
```antlr
// Shared literal bodies — one definition, referenced by the DEFAULT-mode tokens and their SUBSCRIPT-mode twins.
fragment STR_BODY  : '"' (~["\r\n] | '""')* '"' | '\'' (~['\r\n] | '\'\'')* '\'' ;
fragment NAT_BODY  : 'N' STR_BODY ;
fragment BOOL_BODY : 'B' '"' [01]+ '"' | 'B' '\'' [01]+ '\'' ;
fragment INT_BODY  : [0-9]+ ;
fragment DEC_BODY  : [0-9]+ '.' [0-9]+ | '.' [0-9]+ ;
fragment NAME_BODY : [0-9]+ '-' [a-z0-9] [a-z0-9-]* | [0-9]+ [a-z] [a-z0-9-]* | [a-z] [a-z0-9-]* [a-z0-9] | [a-z] ;
```
Then rewrite the token rules to reference them (bodies MUST be character-identical to today's — verify each
against the cited line):
- DEFAULT: `STRINGLIT : STR_BODY ;` (was `:620-622`), `NATLIT : NAT_BODY ;` (`:626`),
  `BOOLLIT : BOOL_BODY ;` (`:635`), `INTEGERLIT : INT_BODY ;` (`:616`), `DECIMALLIT : DEC_BODY ;` (`:601`),
  `IDENTIFIER : NAME_BODY ;` (`:609-614`).
- SUBSCRIPT: `SUB_STRINGLIT : STR_BODY ;` (`:750`), `SUB_NATLIT : NAT_BODY ;` (`:756`),
  `SUB_BOOLLIT : BOOL_BODY ;` (`:757`), `SUB_INTEGERLIT : INT_BODY ;` (`:745`), `SUB_DECIMALLIT : DEC_BODY ;`
  (`:746`), `SUB_IDENTIFIER : NAME_BODY ;` (`:760`).

**Do NOT touch** the SUBSCRIPT-only tokens with no DEFAULT twin (`SIGNED_DECIMALLIT :739`,
`SIGNED_INTEGERLIT :742`) or any operator/whitespace tokens — they have no duplication to remove. **Do NOT
reorder** any token rule (token precedence is longest-match + declaration order; keep every rule in place, only
its body changes to a fragment reference).

> **Caveat — `NAT_BODY` referencing `STR_BODY`.** Current `NATLIT` (`:626`) is `'N' '"'…'"' | 'N' '\''…'\''`
> and `SUB_NATLIT` (`:756`) is identical. `'N' STR_BODY` produces the same language (STR_BODY already covers
> both quote forms). Confirm the generated `.tokens` and battery agree; if any doubt, spell `NAT_BODY` out
> literally as `'N' '"' (~["\r\n] | '""')* '"' | 'N' '\'' (~['\r\n] | '\'\'')* '\''` rather than composing —
> correctness over cleverness.

**Why:** One tokenization rule per literal shape; a future string-escape or national-literal fix is applied once
and cannot diverge between the two modes (DESIGN §3.3b).

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug                                  # regen + compile; must succeed
diff /tmp/CobolLexer.tokens.baseline src/Cobol.Net.Frontend/Generated/CobolLexer.tokens   # BYTE-IDENTICAL
bash scripts/guard-fast.sh                                            # greenfield green
# Subscript-with-literal-args probe (exercises SUB_STRINGLIT / SUB_NATLIT / SUB_INTEGERLIT):
cat > /tmp/subprobe.cob <<'EOF'
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROBE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(2).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH("ABCDE").
           DISPLAY R.
           COMPUTE R = FUNCTION LENGTH(N"XY").
           DISPLAY R.
           STOP RUN.
EOF
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /tmp/subprobe.cob --std 2002 -o /tmp/sp.dll --run
# expect: prints 05 then 02, exit 0
```
Expected: `.tokens` byte-identical (fragments do not change token-type assignments), full battery green.
**COMMIT BOUNDARY.** Suggested message:
```
refactor(frontend/lexer): share SUBSCRIPT/DEFAULT literal bodies via fragments (P4 group B)

STR_BODY/NAT_BODY/BOOL_BODY/INT_BODY/DEC_BODY/NAME_BODY are defined once and referenced
by both the DEFAULT-mode literal tokens and their SUB_* SUBSCRIPT-mode twins, removing the
duplicated token bodies (CobolLexer.g4 §1.9). Behavior-neutral: CobolLexer.tokens
byte-identical; battery green.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017kSL7aKj3FXensEvmEDfhs
```

---

### GROUP C — Typed `Cst/` façade for the highest-churn rules

> Group C installs the façade and migrates the two anchor consumers. It does NOT migrate all ~339 `GetText()`
> sites (that is P7) and does NOT internalize the generated contexts yet (P7 end-state). Keep each step green.

#### Step C1 — Create the façade skeleton: `SourceSpan` + the two dominant façade types
**Files (create):**
- `src/Cobol.Net.Frontend/Cst/SourceSpan.cs`
- `src/Cobol.Net.Frontend/Cst/DataReferenceCst.cs`
- `src/Cobol.Net.Frontend/Cst/DataDescriptionCst.cs`
- `src/Cobol.Net.Frontend/Cst/CstExtensions.cs`

**What:** Thin, read-only, `readonly struct` wrappers — 1:1 with grammar rules, holding the generated context,
exposing typed accessors. No semantic/computed state (that belongs to the binder's model — enforced in review).

```csharp
// Cst/SourceSpan.cs
namespace CobolNet.Frontend.Cst;
public readonly record struct SourceSpan(int Line, int Column, int Length)
{
    public static SourceSpan Of(Antlr4.Runtime.ParserRuleContext ctx)
        => new(ctx.Start.Line, ctx.Start.Column, (ctx.Stop?.StopIndex ?? ctx.Start.StopIndex) - ctx.Start.StartIndex + 1);
}
```

```csharp
// Cst/DataReferenceCst.cs  — wraps CobolParserCore.DataReferenceContext (the 142-site dominant rule).
using CobolNet.Frontend.Generated;
namespace CobolNet.Frontend.Cst;

public readonly struct DataReferenceCst(CobolParserCore.DataReferenceContext ctx)
{
    public CobolParserCore.DataReferenceContext Context => ctx;

    /// <summary>The special-register kind if this reference is LINAGE/LINE/PAGE-COUNTER, else None.</summary>
    public SpecialRegister Register =>
          ctx.LINAGE_COUNTER() is not null ? SpecialRegister.LinageCounter
        : ctx.LINE_COUNTER()   is not null ? SpecialRegister.LineCounter
        : ctx.PAGE_COUNTER()   is not null ? SpecialRegister.PageCounter
        : SpecialRegister.None;

    /// <summary>The base data-name text (the leading cobolWord), or null for a bare special register.</summary>
    public string? BaseName => ctx.cobolWord() is { } w ? w.GetText() : null;

    /// <summary>OF/IN qualifier names, innermost-first, as the parse yields them.</summary>
    public IReadOnlyList<string> Qualifiers { /* walk ctx.dataReferenceSuffix()/qualification(); see note */ get; }

    /// <summary>True if the reference carries any subscriptPart.</summary>
    public bool HasSubscripts { get; }

    /// <summary>The raw SUBSCRIPT-mode token run for the subscript/ref-mod, preserved for the binder's
    /// SplitSubscriptTokens (unchanged contract — the frontend hands the run through faithfully).</summary>
    public CobolParserCore.SubscriptOrRefModContext? SubscriptTokens { get; }

    public SourceSpan Span => SourceSpan.Of(ctx);

    // Non-invasive adoption: existing call sites pass the raw context unchanged.
    public static implicit operator DataReferenceCst(CobolParserCore.DataReferenceContext c) => new(c);
}

public enum SpecialRegister { None, LinageCounter, LineCounter, PageCounter }
```

```csharp
// Cst/DataDescriptionCst.cs  — wraps CobolParserCore.DataDescriptionEntryContext.
using CobolNet.Frontend.Generated;
namespace CobolNet.Frontend.Cst;

public readonly struct DataDescriptionCst(CobolParserCore.DataDescriptionEntryContext ctx)
{
    public CobolParserCore.DataDescriptionEntryContext Context => ctx;
    public int? Level      => int.TryParse(ctx.levelNumber()?.GetText(), out var n) ? n : null;
    public string? Name    => ctx.dataName()?.GetText();
    public bool IsTypedef  => /* ctx.<typedefClause>() is not null — use the real accessor name */ false;
    public SourceSpan Span => SourceSpan.Of(ctx);
    // Picture/Usage/Redefines/Occurs exposed as the migration in C3 needs them; add incrementally, 1:1 with grammar.
    public static implicit operator DataDescriptionCst(CobolParserCore.DataDescriptionEntryContext c) => new(c);
}
```

```csharp
// Cst/CstExtensions.cs — the small stringly-typed helpers the binder repeats (cobolWord name, integer-literal).
using CobolNet.Frontend.Generated;
namespace CobolNet.Frontend.Cst;
public static class CstExtensions
{
    public static string Name(this CobolParserCore.CobolWordContext ctx) => ctx.GetText();
    public static int AsInt(this CobolParserCore.IntegerLiteralContext ctx) => int.Parse(ctx.GetText());
    public static bool TryAsInt(this CobolParserCore.IntegerLiteralContext? ctx, out int value)
        => int.TryParse(ctx?.GetText(), out value);
}
```

> **Accuracy note for the implementer:** the exact generated accessor names (`ctx.cobolWord()`,
> `ctx.dataReferenceSuffix()`, `ctx.levelNumber()`, the TYPEDEF-clause accessor, etc.) must be read off the
> ACTUAL generated `CobolParserCore.cs` and the grammar (`dataReference` is `CobolParserCore.g4:504-516`,
> `dataReferenceSuffix` `:518-522`, `dataDescriptionEntry` in `Core/CobolData.g4`). Fill `Qualifiers`,
> `HasSubscripts`, and `SubscriptTokens` by walking `dataReferenceSuffix()`/`qualification()`/`subscriptPart()`
> exactly as `ReferenceResolver` does today (mirror its walk so behavior is identical). The façade is the SAME
> walk, named — not a new interpretation.

**Why:** The narrow typed surface. A grammar rename now breaks these façade files (compile error) instead of
silently drifting across dozens of `GetText()` sites.

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug   # façade compiles against the generated contexts; no consumer yet
```
**Not a commit boundary yet** (land C1+C2+C3 together so the façade ships WITH a real consumer, never dead code).

#### Step C2 — Migrate anchor consumer 1: `ReferenceResolver` reads `DataReferenceCst`
**File (edit):** `src/Cobol.Net.Compiler/Binding/ReferenceResolver.cs`

**What:** The single funnel that turns a `dataReference` parse node into a `Place`. Change its internals to read
the `DataReferenceCst` accessors instead of raw `GetText()`/positional walks (`ReferenceResolver.cs` has the 3
direct `GetText()` sites plus the `cobolWord()?.GetText()` base-name and qualifier walk). Keep the public
`Resolve(...)` signatures accepting the raw `DataReferenceContext` (the implicit conversion means the 142
call sites in the binder partials need ZERO change now — full call-site migration is P7). Internally:
```csharp
public Place Resolve(CobolParserCore.DataReferenceContext ctx, …)
{
    DataReferenceCst r = ctx;                     // implicit
    if (r.Register != SpecialRegister.None) return ResolveRegister(r, …);
    string baseName = r.BaseName ?? … ;
    // qualifiers via r.Qualifiers; subscripts via r.SubscriptTokens (unchanged SplitSubscriptTokens hand-off)
    …
}
```

**Why:** Proves the façade end-to-end at the highest-churn rule's chokepoint; retires the stringly-typed walk in
the one file every `dataReference` flows through. The `SplitSubscriptTokens` re-parse is preserved verbatim
(its elimination is the deferred SUBSCRIPT-mode change, DESIGN §8 Q5 / P5–P7).

**Verify:** build + differential (bound-tree neutrality):
```bash
dotnet build CobolSharp.sln -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance     # GREEN — resolution output unchanged
bash scripts/guard.sh                              # FULL legacy guard ALL GREEN (bound tree byte-neutral)
```
**Not a commit boundary yet.**

#### Step C3 — Migrate anchor consumer 2: `DataBinder.BindEntry` reads `DataDescriptionCst`
**File (edit):** `src/Cobol.Net.Compiler/Binding/DataBinder.cs`

**What:** `BindEntry` (the ~240-line clause decoder, around `DataBinder.cs:1027+`) currently reads
`entry.levelNumber().GetText()`, `entry.dataName()?.GetText()`, picture/usage/redefines/occurs via positional
`GetText()`. Wrap the incoming `DataDescriptionEntryContext` in `DataDescriptionCst` and read `.Level`,
`.Name`, and add the picture/usage/redefines/occurs accessors to the façade (C1) as you migrate each — 1:1 with
the grammar rule, each accessor replacing one `GetText()` cluster. Keep the decode LOGIC identical; only the
READS change from positional strings to typed accessors.

**Scope discipline:** migrate `BindEntry`'s own reads and the level/name/condition-value reads it calls. Do NOT
chase into the file/report/switch partials (P7). This bounds C3 to the DATA-DIVISION entry decoder — the
12-site `dataDescriptionEntry` rule + the level/name churn.

**Why:** The second anchor; demonstrates the façade for the group-vs-elementary entry model that P7's
`EntryTreeBuilder` extraction will consume.

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance                       # GREEN
dotnet test tests/Cobol.Net.Tests.Unit                              # GREEN
bash scripts/guard.sh                                               # FULL legacy guard ALL GREEN
# Emitted-C# neutrality on the representative set (see §5):
# regenerate .g.cs for each baseline program and diff against /tmp/p4-baseline/ — expect ZERO diffs.
```
**COMMIT BOUNDARY.** Suggested message:
```
refactor(binder): introduce Cst/ typed façade; migrate ReferenceResolver + BindEntry (P4 group C)

New Cobol.Net.Frontend/Cst/ (SourceSpan, DataReferenceCst, DataDescriptionCst, CstExtensions)
is the narrow typed surface over the generated ANTLR contexts. ReferenceResolver (the
dataReference funnel, 142 upstream sites) and DataBinder.BindEntry (the dataDescriptionEntry
decoder) now read typed accessors instead of raw GetText(). Implicit conversions keep the
142 call sites unchanged — full migration + context internalization is P7. Behavior-neutral:
conformance + FULL legacy guard ALL GREEN + emitted-.g.cs byte-identical.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017kSL7aKj3FXensEvmEDfhs
```

---

### GROUP D — Version-conformance leg: superset-grammar completion + construct-id annotation

> Executed FROM `docs/rearchitecture/DESIGN-version-conformance-pipeline.md` (the canonical design for this
> leg), AFTER P3's residue migration has landed. Complete the superset grammar (every edition's constructs
> parse unconditionally at all four `--std` values) and install the **committed-match construct-id annotation
> convention**: grammar actions stamp each recognized edition-gated construct into side-table storage keyed by
> parse context, which the `VersionConformancePass` reads. Do **NOT** introduce any edition predicate — the
> only surviving grammar predicates are the two load-bearing forward-detects (§3). The executing session
> authors the step-by-step from the design doc at execution time, with the same per-commit battery discipline
> as Groups A–C.

---

## 5. Verification — full battery at phase end + neutrality checks

Run ALL of the following after the last commit. Every one must pass.

```bash
cd E:/CobolSharp

# 1. Build (regenerates parser; a failed regen fails the build — no stale fallback).
dotnet build CobolSharp.sln -c Debug

# 2. Greenfield conformance + unit.
dotnet test tests/Cobol.Net.Tests.Conformance        # ALL GREEN
dotnet test tests/Cobol.Net.Tests.Unit               # ALL GREEN, incl. CobolWordsDriftTests + ReservedWordsDriftTests

# 3. Full legacy differential guard (the byte-exact net).
bash scripts/guard.sh                                # FULL legacy guard ALL GREEN, 0 regressions

# 4. Version-matrix accept/reject unchanged across editions (word set + fragments are edition-neutral).
bash scripts/version-continuity-sweep.sh             # (or the P3 harness) — all four --std unchanged

# 5. Token-assignment neutrality (Groups A + B).
diff /tmp/CobolLexer.tokens.baseline src/Cobol.Net.Frontend/Generated/CobolLexer.tokens   # byte-identical

# 6. Emitted-C# neutrality (whole phase). For each representative program:
#    subscripts+refmod (any TABLE(I)(a:b) program), PIC/USAGE (a COMP/DISPLAY-heavy program), OO (a CLASS-ID
#    program), files (SQ/RL/IX), report writer, and a name-slot-as-data-name program.
#    Emit .g.cs and diff against /tmp/p4-baseline/ — expect ZERO diffs.
```

**Neutrality contract:** this phase changes NO observable behavior. The proofs are: (a) `CobolLexer.tokens`
byte-identical (token types unchanged), (b) the FULL legacy guard ALL GREEN (bound tree byte-neutral), (c)
emitted `.g.cs` byte-identical on the representative corpus (codegen neutral), (d) the drift test green (the
generated word set equals the JSON equals the runtime lexer set). If any diff appears with no intended behavior
change, it is a bug — bisect to the offending step.

**Grammar-doc sync (`feedback_grammar_doc_sync`):** update any grammar overview doc that describes `cobolWord`
/ `_dataNameTokens` / the SUBSCRIPT literal tokens to point at `cobol-words.json` + `CobolWords.g4` + the shared
fragments. Update `docs/DOC_INDEX.md` if you add the two generated artifacts as tracked files.

---

## 6. Rollback / resumability

- **Resuming mid-phase:** the `STATUS` line + the Execution log (below) record the last completed commit
  boundary. Groups A–C are independent and ordered A → B → C (Group D sequences after P3's residue migration);
  within a group the commit boundaries are A5, B1, C3. Re-establish the `/tmp` baselines (§1) before resuming
  a neutrality check.
- **A is parallel-SSOT-then-flip:** if A4/A5 shows a `.tokens` diff or a battery regression, the JSON extraction
  (A1) missed or mis-classified a word. Revert A4/A5 (the grammar files) — the JSON + script + drift test can
  stay; fix the JSON, regen, re-verify. The hand-written sources are the fallback until A5 lands.
- **B is a single revert:** `git revert` the B1 commit restores the duplicated token bodies; nothing depends on
  the fragments.
- **C is additive:** the façade + anchor migrations. If a differential regression appears, revert C3/C2; the
  `Cst/` types (C1) are inert without consumers and can stay. The implicit conversions mean no call site is
  stranded by a revert.
- **Do NOT** land any commit with a red battery. Each boundary is independently green by construction.

### Risks + mitigations
- **R-A1 (extraction miss, MEDIUM/high-blast):** a word in one source but not the JSON changes tokenization.
  *Mitigation:* the A3 drift test compares JSON to BOTH sources before the flip; the `.tokens` diff + full-corpus
  parse are the net; land A alone so a regression bisects to one commit.
- **R-A2 (namespace mismatch):** the generated lexer partial must be in the SAME namespace as the generated
  `CobolLexer` (post-P1 `CobolNet.Frontend.Generated`). *Mitigation:* the precondition check (§1) + the gen
  script reads the target namespace from a single MSBuild property / the P1 rename; a wrong namespace is a
  compile error, not a silent bug.
- **R-A3 (clean wipes the partial):** `CobolLexerWordSet.g.cs` must live under `Parsing/`, NOT `Generated/`
  (the `CleanGenerated` target deletes `Generated/*`). *Mitigation:* place + verify it survives `dotnet clean`.
- **R-B1 (fragment composition, LOW):** `NAT_BODY : 'N' STR_BODY` must produce the identical language.
  *Mitigation:* `.tokens` byte-diff + the subscript-literal probe; fall back to a spelled-out body if any doubt.
- **R-C1 (façade balloons into a second model, MEDIUM):** façade types must be 1:1 with grammar rules, holding
  the context, no computed/semantic state. *Mitigation:* code-review rule; keep C bounded to the two anchors,
  defer the rest to P7.
- **R-portability (regen on Linux):** the added `CobolWords.g4` import + fragments must regen identically on
  Windows AND Linux (the DEVLOG-554 flat-output hazard). *Mitigation:* the portable-regen logic is untouched;
  verify on WSL per `reference_wsl_linux_repro.md` (build on Windows, `dotnet test --no-build` on WSL) before
  the final commit.

---

## 7. ISO feature work in this phase

**None — this phase adds no new ISO construct.** It is a structural refactor of how EXISTING §8.9 (reserved
words) / §8.10 (context-sensitive words) membership and the §5.3 subscript lexing are *sourced*, plus a
consumption façade. The relevant spec sections are ISO/IEC 1989:2023 §8.9 (reserved words), §8.10
(context-sensitive words), §8.3.3 (literals: alphanumeric/national/boolean), and §5.3 (subscripting) — all
already implemented; this phase preserves their behavior byte-for-byte.

**Conformance guard added:** `tests/Cobol.Net.Tests.Unit/CobolWordsDriftTests.cs` — the version-invariant proof
that the three word-set consumers cannot desync (parallel to `ReservedWordsDriftTests`). No new goldens are
required (behavior is neutral); the existing subscript/name-slot conformance programs are the behavioral net.

**Follow-up ledger (record in DEVLOG + carry forward — NOT fixed here to preserve neutrality):**
- **FU-1 — word-set asymmetries.** `BIT` is in `cobolWord` (nameSlot) but not the lexer subscript-trigger set;
  the `functionName`-collision tokens (`DISPLAY/MERGE/RANDOM/SIGN/SORT/SUM`) are trigger-only. These are
  captured AS-IS in `cobol-words.json`. Whether `BIT` SHOULD be a subscript trigger (so `BIT(1)` as a
  subscripted data item lexes correctly) is a spec question (§8.10 + §13 USAGE BIT) to resolve as a separate,
  behavior-changing fix with its own conformance test — NOT inside this neutral refactor.
- **FU-2 — full `GetText()` migration + context internalization.** P7 migrates the remaining ~337 `GetText()`
  sites onto the `Cst/` façade and flips the generated contexts to `internal` (façade becomes the ONLY
  cross-assembly surface). This phase delivers the façade + the two anchors only.

---

## 8. Execution log (the executing session appends one line per commit boundary)
<!--
- A5 (word set single-sourced) — <date> — .tokens identical, battery green, commit <sha>
- B1 (fragment dedup)          — <date> — .tokens identical, battery green, commit <sha>
- C3 (Cst façade + anchors)    — <date> — guard ALL GREEN, .g.cs identical, commit <sha>
- D  (superset + construct-id annotation) — <date> — battery green, no edition predicates, commit <sha>
-->
