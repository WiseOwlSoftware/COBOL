<!-- Authoritative, implementation-ready design SSOT for PERFORM Format 3 (§14.9.28). Produced 2026-07-20
by an adversarially-verified workflow; REPLACES the deleted PHASE-13-c5-perform-format3-rederivation.json
(whose derivation carried known defects). The three figure questions are resolved in
PHASE-13-c5-perform-format3-pdf-resolution.md and applied here. Diagnostic numbers corrected to the true
contiguous free block 1597-1617 (batch 2 used 1585-1596). -->

# PERFORM Format 3 (§14.9.28) — Implementation-Ready Design

**Status:** Design SSOT for the P13 grammar-batch PERFORM Format-3 construct. Supersedes the C5 derivation and folds in the adversarial verification. Design-doc home for the same change set: `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12.

**The three mandatory adversarial corrections are applied:**
1. Next-free diagnostic is **COBOLNET1597**. NEXT-FREE is 1597, NOT 1585: this session's batch 2 consumed 1585-1588 (VALUE Format 2) and 1591-1596 (PICTURE EDITING) as COMPILER-CHANNEL raw codes (in DataBinder/PictureAnalyzer, NOT DiagnosticCatalog.cs), so a catalog-only scan is insufficient — BOTH the `grep -rho 'COBOLNET1[5-6][0-9][0-9]' src` scan (max 1596) AND the catalog must agree. The contiguous free block 1597-1617 fits this feature's 21 codes. The stale "1585"/"1581"/"1578" claims are discarded.
2. The **Grammar-Shape rules in §1 are authoritative.** The alternate grammar snippet that appeared in the runtime scouting (the greedy `WHEN cobolWord+` form) is discarded — it reintroduced the canonical-body mis-parse and dropped the `file-name-1` operand form.
3. **`RESUME AT procedure-name` inside any WHEN/OTHER/COMMON phrase is a bind-time error** (§14.9.33.3 SR1). The `ResumeSignal(targetPc)` runtime path is reachable **only from a declarative** RESUME, never from an F3 WHEN body. §6 runtime honors this.

---

## 0. Feature summary

Format 3 PERFORM is an **exception-checking PERFORM**: a per-statement exception interceptor scoped to `imperative-statement-1` (imp-1). It is *not* a block-level try/catch. It intercepts exceptions raised by the statements of imp-1, dispatches to a matching `WHEN` handler (in written order, first match wins), optionally runs `WHEN OTHER` / `WHEN COMMON` / `FINALLY`, and resumes per GR20 (resume-in-place for nonfatal; abnormal-terminate for fatal unless a RESUME redirects).

COBOL-2023 only. Gated in `VersionConformancePass`; diagnosed at `--std 85|2002|2014`.

---

## 1. Grammar (AUTHORITATIVE — greedy-safe)

### 1.1 Files touched
- `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4` — `performStatement` merge + new `performWhen*` rules.
- `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs` — add `whenOperandAhead()` near `retryPhraseAhead` (:172).
- `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` — dispatcher (:612) and `statementBlock` (:736) reused unchanged. **`useOnTarget`/`useEcEntry` are NOT reused for PERFORM** (their greedy `cobolWord+` loops are period-terminated-safe in USE but statement-abutted-unsafe in PERFORM).

### 1.2 The mis-parse being avoided (rationale, load-bearing)

The USE rules `useEcEntry : cobolWord (FILE fileName)*` and `useOnTarget` (…`fileName+`, and `fileName : … cobolWord`) are unbounded `cobolWord` loops. They are safe in USE only because a **separator period terminates the USE sentence**. In a WHEN phrase the operand loop **abuts imperative-statement-2 directly** (a `statementBlock*` with no terminator token). For the single most spec-canonical WHEN body —

```
WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT
```

— a naive `useEcEntry+` reuse greedily takes `RESUME` (a `cobolWord`) as a **second exception-name**, then `AT` fails to continue and the parse derails. RESUME in a WHEN arm is exactly what §14.9.33.3 SR1 sanctions, so this is a real defect. Therefore PERFORM gets **dedicated operand rules** with a gated continuation predicate. (Placing the predicate *inside* the shared USE rules would regress `USE … ON RESUME.` where RESUME is a legally-named file — different terminator context, so a different rule. This respects the singular-pattern rule: a statement-abutted list is a different job than a period-terminated list.)

### 1.3 The continuation predicate `whenOperandAhead()`

Add to `CobolParserCoreBase.cs` (alongside `is2023()` and `retryPhraseAhead`):

```csharp
// True when LT(1) may CONTINUE a Format-3 WHEN operand list — i.e. it is NOT one of the
// context-sensitive words that ALSO lead a statement, and would otherwise be captured as a
// spurious file-name / exception-name ahead of imperative-statement-2. This set is exactly
// (cobolWord ∩ statement-first-tokens); pure-reserved verbs (MOVE, ADD, DISPLAY, IF, PERFORM…)
// need no exclusion because they are not cobolWords and the `cobolWord` grammar element already
// rejects them, so the loop stops at them naturally.
protected bool whenOperandAhead()
{
    switch (_input.La(1))
    {
        case CobolLexer.RESUME:  case CobolLexer.RAISE:    case CobolLexer.VALIDATE:
        case CobolLexer.UNLOCK:  case CobolLexer.SEND:     case CobolLexer.RECEIVE:
        case CobolLexer.COMMIT:  case CobolLexer.ROLLBACK: case CobolLexer.GET:
        case CobolLexer.ENTER:   case CobolLexer.PARSE:
            return false;
        default:
            return true;
    }
}
```

This is the **exact, complete** overlap set `cobolWord ∩ statement-leaders`. See §1.6 for the mandatory build-time drift guard that keeps it complete.

### 1.4 The rules

```antlr
performStatement
    // ... existing out-of-line alternatives unchanged ...
    // Merged inline Formats 2 & 3 — ONE inline alternative ⇒ zero cross-format lookahead:
    | PERFORM performInlineHead? statementBlock*
        performWhenPhrase* performWhenOther? performWhenCommon? performFinally?
      END_PERFORM
    ;

performInlineHead
    : performOptions+                        // Format 2: TIMES / UNTIL / VARYING
    | performLocationPhrase                  // Format 3 head
    ;

// [WITH] LOCATION — WITH is an optional word (§8.3.2.4.3); bare LOCATION is 2023-only because
// below 2023 `PERFORM LOCATION` is an ordinary out-of-line PERFORM of a paragraph named LOCATION.
performLocationPhrase
    : WITH LOCATION
    | {is2023()}? LOCATION
    ;

// Three DISJOINT operand forms (§14.9.28.2 Format 3). Each operand list is bounded by
// whenOperandAhead() so it cannot annex the leading verb of imperative-statement-2.
performWhenPhrase
    : WHEN EXCEPTION performWhenModeList statementBlock*      // EXCEPTION {modes | file-name-1…}
    | WHEN           performWhenEcList   statementBlock*      // exception-name-1… | exc-name-2 FILE…
    ;

// EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | {file-name-1}… }.
// The figure's "IO" is the I_O token (typesetting/standard defect — I-O in every sibling
// format and §8.9; folio reference recorded in the PDF-resolution doc). A single WHEN EXCEPTION
// selects exactly ONE mode OR a file-name list — never a mix; bind-enforced (SR at §3).
performWhenModeList
    : INPUT | OUTPUT | I_O | EXTEND
    | fileName ({whenOperandAhead()}? fileName)*             // gated CONTINUATION only
    ;

// { exception-name-1 }… | { exception-name-2 FILE file-name-2 }… — open set (EC-USER-*) ⇒ cobolWord.
performWhenEcList
    : performWhenEcItem ({whenOperandAhead()}? performWhenEcItem)*   // gated CONTINUATION only
    ;

performWhenEcItem
    : cobolWord (FILE fileName)*             // inner FILE-loop is self-bounding (see §1.5 proof)
    ;

performWhenOther
    : WHEN OTHER  EXCEPTION? statementBlock*   // 2nd EXCEPTION optional (§8.3.2.4.3)
    ;

performWhenCommon
    : WHEN COMMON EXCEPTION? statementBlock*   // 2nd EXCEPTION optional (§8.3.2.4.3)
    ;

performFinally
    : FINALLY statementBlock*
    ;
```

The predicate gates only the **continuation** of each list, never the first operand. The first operand after `WHEN`/`WHEN EXCEPTION` is taken unconditionally, which (a) makes the canonical body parse correctly and (b) preserves the superset-parse posture: a malformed `WHEN RESUME …` (no exception-name) still binds RESUME as an exception-name so the binder emits the specific COBOLNET0711 "not an exception-name" diagnostic rather than a generic parse error.

### 1.5 Proof the operand list cannot swallow a following statement verb

**Head disjointness (LA after WHEN).** `EXCEPTION`, `OTHER`, `COMMON` are reserved tokens and **none is in the `cobolWord` funnel**, so `WHEN EXCEPTION…`, `WHEN OTHER…`, `WHEN COMMON…`, and `WHEN <ec-name>…` are unambiguous.

**Continuation boundary — the canonical case** `WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT`:
1. `WHEN` + not-EXCEPTION/OTHER/COMMON → `performWhenEcList`.
2. First `performWhenEcItem` = `EC-I-O-PERMANENT-ERROR`; its inner `(FILE fileName)*` sees `RESUME ≠ FILE` → no inner match.
3. Outer gate `{whenOperandAhead()}?` inspects LT(1)=`RESUME` → in the exclusion set → **predicate false → loop exits.**
4. `statementBlock*` consumes `RESUME AT NEXT STATEMENT` via `resumeStatement`. **Correct.**

Identical for RAISE, VALIDATE, UNLOCK, SEND, RECEIVE, COMMIT, ROLLBACK, GET, ENTER, PARSE, and for the `WHEN EXCEPTION file-name…` form.

**Two exclusion layers, together complete.** Pure-reserved verbs (DISPLAY, MOVE, IF…) are not `cobolWord`, so the grammar element itself fails to match and the loop exits (`WHEN EC-BOUND-SUBSCRIPT DISPLAY "x"` needs no predicate). The 11 context-sensitive verbs *are* `cobolWord`, so the predicate stops them. Between the grammar element (necessary) and the predicate (funnel overlap), every statement-leading token is excluded.

**Inner FILE-loop self-bounding.** `(FILE fileName)*` re-enters only on `FILE`; `FILE` leads no statement and is not in `cobolWord`. So `EC-I-O-PERMANENT-ERROR FILE F1 FILE F2 RESUME…` consumes both `FILE F_n` pairs inside the item, then the outer gate stops at RESUME. No inner predicate needed.

**Accepted, documented limitation** (record in the grammar comment): a file *literally named* one of the 11 words, used as the 2nd+ operand of `WHEN EXCEPTION` (e.g. `WHEN EXCEPTION F1 RESUME`), reads RESUME as imperative-statement-2, not a second file-name. This is the spec-consistent reading (a word in statement-verb position reads as the verb) and never affects exception-names (all begin `EC-`), so it is harmless.

### 1.6 Mandatory drift guard (build-time assertion, not "should")

Add a CobolWords-style hard build-time assertion: the set `{ words in cobol-words.json (name slot) that are the first token of any dispatcher statement }` **must equal** the `whenOperandAhead()` exclusion list. A future name-slot statement verb (a real GET / PARSE statement, etc.) then **fails the build** until the exclusion set is updated. Correctness of the parse depends on this set staying complete, so the guard is a build gate, not a lint.

---

## 2. Tokens & registry work

### 2.1 New lexer tokens

Two new reserved-word tokens are required (currently lex as IDENTIFIER):

| Token | Word | Notes |
|---|---|---|
| `LOCATION` | `LOCATION` | Used by `performLocationPhrase`. Bare `LOCATION` is 2023-gated in-grammar via `{is2023()}?`. |
| `FINALLY` | `FINALLY` | Used by `performFinally`. |

**Already-existing tokens reused (no new work):** `PERFORM`, `WHEN`, `EXCEPTION`, `OTHER`, `COMMON`, `INPUT`, `OUTPUT`, `I_O`, `EXTEND`, `FILE`, `WITH`, `END_PERFORM`.

**FIXED FIGURE FACT:** the figure's `IO` denotes **I-O**. Use the existing `I_O` token — do **not** mint a new `IO` keyword.

**`COMMON` caution:** `COMMON` already exists as a reserved word in the IDENTIFICATION/PROGRAM context (COMMON program attribute). Confirm the existing token is reusable here as a context word in `performWhenCommon` without regressing that use; it is only consumed positionally after `WHEN`, so no conflict is expected, but include a targeted parse test.

### 2.2 Registry work (three JSON files + generated guards)

`LOCATION` and `FINALLY` become reserved in COBOL-2023:

- **`cobol-words.json`** — add `LOCATION` and `FINALLY` entries with the correct edition/first-appearance metadata (2023). These are reserved words, **not** name-slot (`cobolWord`) words — they must be excluded from the `cobolWord` funnel or they would themselves become spurious operands.
- **`reserved-words.json`** — add both words as reserved in the 2023 edition list (and confirm they are *not* reserved in 85/2002/2014, so the version-matrix continuity invariant holds — `PERFORM LOCATION` naming a paragraph remains legal pre-2023).
- **`constructs.json`** — add the `perform-format-3` construct row with its 2023 gate + the `perform-exception-checking-2023` feature key (see §4) so the construct is discoverable in the traceability inventory.
- Re-run the generated catalog/word guards; the `LOCATION`/`FINALLY` additions must round-trip on both OSes (`feedback_commit_generated_parser`).

### 2.3 The drift-guard registry hook

The `whenOperandAhead()` completeness guard (§1.6) reads the name-slot set from `cobol-words.json` at build time. Wire it as a generated assertion so adding a name-slot verb to the JSON is what trips it.

---

## 3. Complete syntax-rule list with diagnostics

Diagnostics are allocated from **NEXT-FREE = COBOLNET1597** upward (the contiguous free block 1597-1617; 1585-1596 are in use as batch-2 compiler-channel codes — confirm with BOTH the src grep AND the catalog scan). Reuse the existing context-parameterised **COBOLNET0711** for an unknown/invalid exception-name. Each new code lands in `DiagnosticCatalog.cs` in the same change set, plus the catalog-drift guard.

### 3.1 Scope-region taxonomy (load-bearing for the containment walk)

The binder performs a **single lexical-containment walk** over the bound F3 PERFORM sub-tree, classifying each contained statement/directive by region:

- **Region A — imperative-statement-1 ONLY.**
- **Region B — WHOLE PERFORM (imp-1..imp-5, anywhere).**
- **Region C — WHEN phrases only** (imp-2 / imp-3 / imp-4; the handler bodies).
- **Region D — imperative-statement 2/3/4/5 only** (everything except imp-1).

Use one shared `IsLexicallyWithin(node, region)` predicate (the same predicate the POP/PUSH/`>>TURN` checks already use) to drive every rule below.

### 3.2 Format-3's own syntax rules (§14.9.28.3 SR14–SR16) and structural facts

| ID | Rule | Region | Diagnostic |
|---|---|---|---|
| **PF3-STRUCT-END** | `END-PERFORM` required to close the inline Format-3 statement. | structural | Reuse existing missing-`END-PERFORM` scope diagnostic (no new code). |
| **PF3-STRUCT-WHEN-REQUIRED** | At least one ordinary `WHEN` phrase is required (outer brace, ellipsis outside). A PERFORM of only WHEN OTHER / WHEN COMMON / FINALLY is not admitted. | structural (bind) | **COBOLNET1597** — "a Format-3 (exception-checking) PERFORM requires at least one WHEN phrase". |
| **PF3-STRUCT-ORDER** | The three optional trailing lines appear only in fixed order: `WHEN OTHER`, then `WHEN COMMON`, then `FINALLY`. (Structurally enforced by the grammar ordering `performWhenOther? performWhenCommon? performFinally?`.) | structural | Grammar-enforced; no runtime diagnostic. |
| **PF3-STRUCT-WHEN-OPERAND-EXCLUSIVE** | A single ordinary WHEN selects exactly ONE operand form: (a) `EXCEPTION` + one mode/file-name selection, (b) an `exception-name-1` list, or (c) an `exception-name-2 FILE file-name-2` list. No mixing across the three across one WHEN. The grammar's `performWhenEcItem` mildly supersets by allowing bare EC-names and `FILE`-paired items to interleave. | per-WHEN (bind) | **COBOLNET1598** — "a WHEN phrase mixes exclusive operand forms (bare exception-name with a FILE-paired exception-name)". |
| **PF3-SR14** | A `file-name` shall not appear more than once across the WHEN phrases **unless every such instance is paired with an exception-name** (§14.9.28.3 SR14, L29192). | across all WHEN operands (bind) | **COBOLNET1599** — "file-name '{0}' is specified in more than one WHEN phrase without an exception-name pairing". |
| **PF3-SR15** | An `exception-name` shall appear only once **unless each occurrence pairs a different file-name** (§14.9.28.3 SR15, L29194). | across all WHEN operands (bind) | **COBOLNET1600** — "exception-name '{0}' is specified more than once without a distinct file-name". |
| **PF3-SR16** | If `file-name-2` is specified, `exception-name-2` shall begin with the COBOL characters `EC-I-O` (§14.9.28.3 SR16, L29196). | per FILE-paired operand (bind) | **COBOLNET1601** — "exception-name '{0}' paired with FILE must begin with 'EC-I-O'". |

### 3.3 Cross-statement syntax bans (verified verbatim against the spec)

All line numbers and SR ordinals verified against `specs/ISO_COBOL.md`. Each ban uses the shared containment predicate over the indicated region.

| ID | Statement / directive banned | Region | Section (SR, line) | Diagnostic |
|---|---|---|---|---|
| **XS-POP** | `>>POP` directive | B (whole PERFORM) | §7.3.20.3 SR4 (L4705) | **COBOLNET1602** — "the POP directive shall not appear within an exception-checking PERFORM". Hard `shall not`. |
| **XS-PUSH** | `>>PUSH` directive | B | §7.3.22.3 SR4 (L4798) | **COBOLNET1603** — "the PUSH directive shall not appear within an exception-checking PERFORM". Hard. |
| **XS-EXIT-PERFORM-CYCLE** | `EXIT PERFORM CYCLE` (the CYCLE phrase) | B | §14.9.14.3 SR8 (L27097) | **COBOLNET1604** — "EXIT PERFORM CYCLE shall not appear within an exception-checking PERFORM". Plain `EXIT PERFORM` remains legal. **Retracts** the C5 anchor's false "needs no new syntax" claim. |
| **XS-INITIATE-MULTI** | `INITIATE` naming >1 report-name | B | §14.9.21.3 SR3 (L27719) | **COBOLNET1605** — "an INITIATE with more than one report-name shall not appear within an exception-checking PERFORM". |
| **XS-TERMINATE-MULTI** | `TERMINATE` naming >1 report-name | B | §14.9.46.3 SR3 (L32271) | **COBOLNET1606** — "a TERMINATE with more than one report-name shall not appear within an exception-checking PERFORM". |
| **XS-VALIDATE-MULTI** | `VALIDATE` naming >1 identifier | B | §14.9.50.3 SR6 (L32798) | **COBOLNET1607** — "a VALIDATE with more than one identifier shall not appear within an exception-checking PERFORM". |
| **XS-GOTO** | `GO TO` anywhere in a WHEN phrase | C (WHEN phrases) | §14.9.17.3 SR3 (L27300) | **COBOLNET1608** — "GO TO shall not appear in a WHEN phrase of an exception-checking PERFORM". |
| **XS-RESUME-PLACEMENT** | `RESUME` outside a WHEN phrase (i.e. in imp-1 or FINALLY) of an F3 PERFORM | body ∖ C | §14.9.33.3 SR1 (L29950) | **COBOLNET1609** — "RESUME may appear only in a declarative or a WHEN phrase". |
| **XS-RESUME-OPERAND** | `RESUME AT procedure-name` inside any WHEN/OTHER/COMMON phrase (must be `RESUME NEXT STATEMENT`) | C | §14.9.33.3 SR1 (L29950) | **COBOLNET1610** — "RESUME in a WHEN phrase shall specify NEXT STATEMENT". **This is the adversarial reconciliation: the binder rejects it; the runtime never executes a `RESUME AT proc` from a WHEN body.** |
| **XS-RAISE** | `RAISE` in any imperative-statement other than imp-1 | D (imp-2/3/4/5) | §14.9.29.3 SR4 (L29402) | **COBOLNET1611** — "RAISE shall appear only in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-CLOSE-MULTI** | `CLOSE` naming >1 file-name | A (imp-1 only) | §14.9.6.3 SR3 (L26022) | **COBOLNET1612** — "a multi-file CLOSE shall not appear in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-DELETE-FILE-MULTI** | `DELETE FILE` naming >1 file-name | A | §14.9.10.3 SR4 (L26352) | **COBOLNET1613** — "a multi-file DELETE FILE shall not appear in imperative-statement-1". |
| **XS-INITIALIZE-DUP** | `INITIALIZE` repeating identifier-1 | A | §14.9.20.3 SR2 (L27570) | **COBOLNET1614** — "identifier '{0}' is specified more than once in an INITIALIZE in imperative-statement-1". |
| **XS-MERGE** | any `MERGE` | A | §14.9.24.3 SR1 (L28272) | **COBOLNET1615** — "MERGE shall not appear in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-OPEN-DUP** | `OPEN` repeating file-name-1 | A | §14.9.27.3 SR3 (L28839) | **COBOLNET1616** — "file-name '{0}' is specified more than once in an OPEN in imperative-statement-1". |
| **XS-SORT** | any `SORT` | A | §14.9.40.3 SR3 (L31491) | **COBOLNET1617** — "SORT shall not appear in imperative-statement-1 of an exception-checking PERFORM". |

**Allocated range: COBOLNET1597–1617** (21 codes, contiguous). Re-verify against BOTH the src grep AND `DiagnosticCatalog.cs` at implementation time (two independent scans must agree) before minting. These are COMPILER-CHANNEL raw codes (the 1542/0808 pattern), not catalog descriptors.

**Not enumerated (explicitly out of scope, deliberately):**
- §14.9.28.3 SR8 (UNTIL EXIT "under" a VARYING/TEST PERFORM) — orthogonal to exception-checking; it constrains a nested UNTIL-EXIT PERFORM, not the F3 body. Its meaning IS spec-resolved (the C5 non-redundancy derivation: only an EXPLICIT `WITH TEST BEFORE`/`TEST AFTER` or a `VARYING` on the enclosing PERFORM triggers it — the IMPLIED TEST BEFORE of §14.9.28.3 SR1 does not, else SR8's separate mention of VARYING would be redundant). DEFERRED as a separate work item (orthogonal scope), tracked in the §11 analysis backlog — NOT an owner question and NOT implemented here.
- General rules describing WHEN-precedence over declaratives (ACCEPT, DISPLAY, OPEN GR, RECEIVE, SEND, WRITE) — execution semantics, realized by the runtime in §6, not syntax bans.
- §7.3.x TURN L4937 (EC-I-O-WARNING) — permissive, handled in §6.3.

### 3.4 The RESUME AT-optional grammar fix (§14.9.33.2) — ships in this change set

`§14.9.33.2` makes `AT` an **optional word** in RESUME (it is not underlined in the general format), so `RESUME NEXT STATEMENT` (no `AT`) is legal source. The current `resumeStatement` rule makes `AT` mandatory, which would make XS-RESUME-OPERAND's "must specify NEXT STATEMENT" unsatisfiable without `AT`. Fix:

```antlr
resumeStatement
    : RESUME AT? (NEXT STATEMENT | procedureName)
    ;
```

`AT?` makes both `RESUME NEXT STATEMENT` and `RESUME AT NEXT STATEMENT` legal, and both `RESUME proc` / `RESUME AT proc`. This is a dependency fix (not a Format-3 SR) that must land in the same change set. Add a targeted parse test for all four spellings and re-run the full legacy guard (RESUME is shared with declaratives).

---

## 4. Version gate

- **Feature key:** `perform-exception-checking-2023`.
- **Diagnostic (used at `--std 85|2002|2014`):** **COBOLNET0900** — the `ConstructRegistry.Check` introduction gate, CONFIRMED: this is the exact gate `picture-editing-2023` and `value-table-format-2002` fire this batch (recognition-based, via a `constructs.json` row + a `VisitPerformStatement`/`ParseArm` override). No new code needed. VCR row for exception-checking PERFORM.
- **Gate site:** `VersionConformancePass`. The construct is rejected (edition error) below 2023. Bare-`LOCATION` disambiguation is additionally handled in-grammar (`{is2023()}?`) so that pre-2023 `PERFORM LOCATION` continues to parse as an out-of-line PERFORM of a paragraph named `LOCATION` (continuity invariant).
- **Version matrix:** add rows to `docs/VERSION_TEST_MATRIX_DESIGN.md` / `VERSION_CHANGE_REFERENCE.md`: introduction at 2023; not-present in 85/2002/2014 (both the reserved words `LOCATION`/`FINALLY` and the construct); behavior invariants for 2023.

---

## 5. Runtime — GR14–GR22 with LANDABLE-vs-STAGED boundary

### 5.1 Semantics (implementation terms)

The F3 PERFORM is a **per-statement exception interceptor scoped to imp-1**, not a block try/catch. This dictates the whole integration: do **not** wrap imp-1 in a block `try/catch(CobolException)` — a block catch unwinds past the remaining imp-1 statements and cannot deliver GR20's nonfatal resume-in-place.

- **GR14 — implicit TURN over imp-1, LOCATION-conditional.** For each exception-name named in a WHEN, if checking isn't already enabled at imp-1's first statement, an implicit `>>TURN <name> CHECKING ON` (LOCATION iff `WITH LOCATION`) is assumed over imp-1's textual extent — a scoped overlay that leaks nothing past imp-1 and removes only what it added. A real `>>TURN` on before the PERFORM survives; a purely-implicit enable is dropped. "Enabled" is a **compile-time** decision here (the `TurnState` fold gates whether a raise guard is emitted), so GR14 is a **bind-time overlay on `TurnState`**.
- **GR15** — imp-1 is the guarded body.
- **GR16** — FINALLY (imp-5) is "the end of the PERFORM"; without FINALLY, END-PERFORM is the end. No transfer of control out of imp-5 (syntax rule); an `EXIT PERFORM` in imp-5 degrades to CONTINUE after END-PERFORM.
- **GR17** — a WHEN match runs imp-2; any USE declarative that would otherwise match is **ignored** (the WHEN replaces the USE/`__EcDispatch` path for that condition). Match test = the USE GR3a–3g hierarchy predicate.
- **GR18** — WHEN OTHER runs imp-3 for any *enabled-at-detection* condition not named in a WHEN; USE ignored.
- **GR19** — WHEN COMMON runs imp-4 after imp-2 or imp-3 completes.
- **GR20 — resumption.** imp-1's last statement completes → end of PERFORM. If a WHEN was taken: **nonfatal** → implicit CONTINUE immediately after the statement in imp-1 that raised (resume-in-place; imp-1 is not abandoned); **fatal** → after handlers, continue per §14.6.13.1.3 (abnormal termination) unless a declarative-style RESUME redirected.
- **GR21** — ECs raised inside imp-2/3/4/5 are **not** re-caught by this PERFORM; they behave as in a Format-2 PERFORM (normal USE/fatal). Interceptor scope is imp-1 only.
- **GR22** — a WHEN's exception enabled by a real TURN *before* the PERFORM stays enabled; a real TURN *within* the range is retained; otherwise the WHEN's checking is not enabled afterward = "pop the overlay, keep the real directives".

**Precedence:** a statement's own `ON SIZE ERROR` / `AT END` / `INVALID KEY` / `ON OVERFLOW` phrase precedes the PERFORM WHEN (§14.6.13.1.3 #1 / .4 #1). The emitter already gates via `if (!hasPhrase)`, so the WHEN interceptor slots into the same `!hasPhrase` branch — no new precedence logic.

**WHEN matching = USE GR3a–3g predicate minus the declarative-ordering tiers:** a level-3 name matches itself; a level-2 name covers its children (`ExceptionCatalog.UnderLevel2`); level-1 `EC-ALL` covers all; a file-scoped operand additionally requires the raised condition be associated with that file. `__EcDispatch`'s Tier conditions and `TurnState.NameMatches` already implement this. For WHEN phrases the cross-declarative ordering becomes simply **written order, first match wins**.

### 5.2 Integration seams (the interceptor mechanism)

Reuse the existing per-statement dispatch-result protocol (`-1` continue / `-2` resume-next / `-3` … / `≥0` pc) and `ResumeSignal` — do not invent new control machinery.

1. **New bound node `BoundExceptionPerform`** (`ControlFlowBinder` + `EcBinder`) carrying: bound imp-1 list; ordered WHEN descriptors `(MatchKind, names/files/modes, bound imp-2)`; bound OTHER (imp-3), COMMON (imp-4), FINALLY (imp-5) blocks; `WithLocation` flag.

2. **GR14 overlay (bind time):** before binding imp-1, push an overlay of `(name, withLocation)` for each WHEN-named exception onto the `EcState`/`TurnState`; seed the fold as a synthetic enabling event at line 0 so a real source `>>TURN OFF` inside imp-1 overrides it (GR14) and real enables persist (GR22). **Pop after imp-1.** imp-2/3/4/5 bind **without** the overlay (GR21).

3. **RESUME in a WHEN (binder):** relax `EcBinder.BindResume`'s SR1 ("declarative only") to also accept "inside an F3 PERFORM WHEN/OTHER/COMMON" — **but only `RESUME NEXT STATEMENT`** there (XS-RESUME-OPERAND rejects `RESUME AT proc`). The `ResumeSignal(targetPc)` path (a real pc jump) is bound **only for declarative RESUME**.

4. **Ambient F3-frame stack in `ExceptionEngine`/`ExceptionState`** (run-unit-scoped, push/pop nesting- and CALL-safe): each frame carries a matcher delegate the emitted PERFORM installs — a closure `(string ec, string? file, bool fatal) => int` that does the written-order WHEN match, runs the matching imp-2 (or imp-3 for OTHER) then imp-4 (COMMON) inline, and returns the dispatch-protocol action. A `Handling` flag makes the frame transparent while its own handler bodies run (GR21).

5. **Emit shape** (inside the paragraph dispatcher `case`):
   ```
   __ecFrames.Push(new PerformFrame((ec,file,fatal) => {
       if (__whenN_matches(ec,file)) { /* imp-2 inline */ ...; return __action; }
       ... /* WHEN OTHER → imp-3 */
       return PerformFrame.NoMatch;   // fall through to __EcDispatch (USE)
   }));
   try { /* imp-1, compiled with the GR14 overlay */ }
   finally { __ecFrames.Pop(); }
   /* imp-5 FINALLY block (trailing, reached on normal fall-through) */
   __perfEnd{n}: ;
   ```

6. **Interception wrapper:** replace bare `__EcDispatch(ec, file)` at raise sites with `__EcPerform(ec, file, fatal)` = "if a non-`Handling` F3 frame is on top and its matcher handles `(ec,file)`, return that action (skip USE — GR17/18); else `__EcDispatch(ec,file)`." One-line substitution at each existing dispatch site in `EcEmitter` (`EmitRaise`, `EmitSizeHandling`, `EmitOverflow`, `EmitArgOrPlain`) and `__IoCheckEc`. Existing `!hasPhrase` gate preserved → statement-own-phrase precedence for free.

7. **Resume/return mapping (all via the existing protocol):**
   - nonfatal handled → matcher returns `-1`; raise site continues after the raising statement → GR20 resume-in-place, automatic.
   - `RESUME NEXT STATEMENT` in a WHEN → matcher returns `-2`; raise site falls through past the raising statement, suppressing fatal termination → GR20 / §14.9.33 GR2.
   - **`RESUME AT proc` — NOT reachable from a WHEN body** (bind-rejected by XS-RESUME-OPERAND). The `ResumeSignal(targetPc)` throw→`catch(ResumeSignal){ __pc = rs.TargetPc; break; }` at the PERFORM boundary exists **only for the declarative RESUME path**.
   - fatal, unresumed → matcher returns `-1` after handlers; raise site's existing `throw CobolFatalException` fires; `finally` still pops the frame.

8. **GR21** — `Handling` flag around inline handler bodies; `__EcPerform` skips a `Handling` frame; imp-2..5 also bound without the overlay (belt and suspenders).

9. **EXIT PERFORM** — `BoundExitPerform` currently emits `break` (correct for a C# loop, wrong inside an F3 dispatcher body). Give the emitter a "current F3 PERFORM end label"; inside an F3 body a non-CYCLE `EXIT PERFORM` emits `goto __perfEnd{n};` (the `finally` runs on the way out → frame pop guaranteed). `EXIT PERFORM` in imp-5 → CONTINUE (fall through past END-PERFORM), GR16. `EXIT PERFORM CYCLE` is bind-rejected (XS-EXIT-PERFORM-CYCLE), so it is never emitted.

10. **GR22** — satisfied by popping the GR14 overlay; no runtime code.

### 5.3 EC-I-O-WARNING end-of-PERFORM turn-off (adversarial item, resolved here)

§…L4937: `EC-I-O-WARNING` may be turned **off** only by an explicit `>>TURN` directive **or by the end of an exception-checking PERFORM**. The GR14 overlay as described pops at the **end of imp-1**, which for this one EC is *earlier* than "end of the whole PERFORM." **Resolution:** for an `EC-I-O-WARNING` WHEN operand specifically, the overlay disable is deferred to the **end of the whole PERFORM** (after FINALLY), not the end of imp-1. Because "enabled" is a compile-time `TurnState` decision and imp-2..5 bind without the general overlay anyway, this is a targeted overlay-scope extension for `EC-I-O-WARNING` only; record it in the design doc and cover it with a test. (For all other ECs, end-of-imp-1 pop is correct.)

### 5.4 LANDABLE vs STAGED boundary

**LANDS NOW (one change set — the tractable core):**
- Grammar + tokens + registry + version gate (§1–§4).
- All syntax-rule diagnostics COBOLNET1597–1605 (§3) + the RESUME `AT?` fix.
- `BoundExceptionPerform` binder with the GR14 `TurnState`-seed overlay and WHEN descriptors reusing `ExceptionCatalog.UnderLevel2` / level checks.
- RESUME-in-WHEN relaxation limited to `NEXT STATEMENT`.
- Ambient F3-frame stack + `__EcPerform` wrapper wired into the **common raise families**: RAISE, EC-SIZE, EC-OVERFLOW-STRING/-UNSTRING, the fatal ambient gates (EC-ARGUMENT-FUNCTION, EC-BOUND-REF-MOD), and EC-I-O via `__IoCheckEc`.
- RESUME NEXT via `-2`; nonfatal resume-in-place; EXIT-PERFORM goto-end; FINALLY as a trailing block; GR22 via overlay pop; EC-I-O-WARNING scope (§5.3).
- A conformance test (§7).

This works because the per-statement dispatch protocol and `ResumeSignal` already deliver the resume-in-place and RESUME semantics GR20 / §14.9.33 require — the F3 PERFORM is largely a *reordering* of which handler (frame vs `__EcDispatch`) fires first.

**STAGED (documented P14 / next-wave GAPs, each with a `COBOLNET0899` "documented non-support in current build" disposition emitted at the relevant construct so no silent mis-compile occurs):**

1. **`WHEN EXCEPTION INPUT/OUTPUT/I-O/EXTEND` open-mode operand form** — matching an EC-I-O by the raising file's *current open mode* needs the runtime matcher to query the connector's open mode at the raise site (parallel to `__IoCheckEc`'s open-mode tier, on the WHEN path). Disposition: emit **COBOLNET0899** on this operand form until staged; stage after the name-list/FILE forms land. *(Note: the grammar in §1.4 already parses this form; the GAP is the runtime matcher, so 0899 is emitted at bind for the mode-operand form only.)*
2. **Exhaustive raise-site sweep** — every remaining site that today calls `__EcDispatch` or throws `CobolFatalException` (EC-DATA-CONVERSION, EC-BOUND-OVERFLOW, EC-RANGE-*, and CALL/CANCEL-raised EC-PROGRAM/EC-EXTERNAL surfacing as `CobolCallException` across an activation boundary) must also route through `__EcPerform`. The cross-CALL cases interact with the PERFORM "range" (GR1: performed declaratives / called elements are in range) and frame-stack save/restore across `CallProgram`. Disposition: the un-swept ECs simply fall to normal USE/fatal (a documented behavioral GAP, not a mis-compile); tracked as a mechanical sweep + focused cross-activation design.
3. **FINALLY-on-abnormal-termination / RESUME-AT-bypasses-FINALLY** — a genuine spec ambiguity: NOTE 8 says end-of-PERFORM *includes* FINALLY (implying it runs on the fatal-terminate path); NOTE 9 says a transfer out during WHEN processing (a declarative `RESUME AT proc`) never hits the PERFORM exit (implying FINALLY is bypassed). This is a GENUINE STANDARD CONTRADICTION (the §14.9.28.4 GR18/NOTE-pair defect already recorded in DEVLOG 925 / CONFORMANCE.md) — not an owner question. Per the standing rule (record the defect, choose a behavior, never silently code around it): the CHOSEN default is **FINALLY runs on the normal fall-through path only** (it does NOT run on the fatal abnormal-termination path). Record this choice as a documented standard-defect disposition in `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` when the abnormal-termination path lands; revisit only if the four-edition inventory surfaces a conformance test that pins the other reading.
4. **EC-FLOW-USE (§14.9.49.4 GR2) and `>>PROPAGATE`** interactions with active F3 frames — ride the PROPAGATE wave, not this change set.
5. **Exception *object* raised inside imp-1** — GR17 matches exception *names*; an object raise must bypass the F3 frame and fall to `__EcObjDispatch` (Format 4). Small explicit test; can ride the core or stage.

### 5.5 Key files
- Grammar: `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4` (`performStatement` :18) + `CobolParserCoreBase.cs` (`whenOperandAhead()` near :172).
- Binder: `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.cs` (`BindPerform` :123, and the ≥1-WHEN check in `BindPerformControl` :156); `.../Verbs/EcBinder.cs` (`BindResume` :119, overlay/`EcWrap` :289).
- Overlay host: `src/Cobol.Net.Compiler/Binding/TurnState.cs` (`Fold` :83).
- Emitter: `src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs` (raise sites + `EmitDispatchSelector` :253); `.../Verbs/ControlFlowEmitter.cs` (`EmitPerform` :80).
- Runtime: `src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs` (add the F3-frame stack); `.../Control/Signals/ResumeSignal.cs`.
- Dispatch/`__RunUse`: `src/Cobol.Net.Compiler/CodeGen/DispatchEmitter.cs` (:163).
- `BoundExitPerform`: `.../Binding/Bound/BoundTree.cs:517` + emit at `.../CodeGen/StatementEmitter.cs:112`.
- Diagnostics: `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` (mint 1585–1605; catalog-drift guard).
- Design SSOT to update same change set: `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12 (:220).

---

## 6. Test plan

Every post-1985 feature ships conformance tests in the same commit (`feedback_conformance_tests_per_feature`); parser + emitter + output-verifying test ship together (`feedback_parse_and_emit_together`).

### 6.1 Parse tests (grammar)
- **Canonical body must parse (the defect-2 regression net):** `WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT` — assert `RESUME` is imp-2, not a second exception-name. Repeat for all 11 exclusion verbs (RESUME, RAISE, VALIDATE, UNLOCK, SEND, RECEIVE, COMMIT, ROLLBACK, GET, ENTER, PARSE) as the leading verb of imp-2.
- `RESUME NEXT STATEMENT` and `RESUME AT NEXT STATEMENT` both parse (the §14.9.33.2 `AT?` fix); plus `RESUME proc` / `RESUME AT proc`.
- Each WHEN operand form: `WHEN EXCEPTION INPUT`, `... OUTPUT`, `... I-O`, `... EXTEND`; `WHEN EXCEPTION F1 F2`; `WHEN EC-BOUND-SUBSCRIPT`; `WHEN EC-I-O-PERMANENT-ERROR FILE F1 FILE F2`.
- `WITH LOCATION` and bare `LOCATION` head; the trailing `WHEN OTHER [EXCEPTION]`, `WHEN COMMON [EXCEPTION]`, `FINALLY` lines with and without the optional 2nd `EXCEPTION`.
- `WHEN EC-BOUND-SUBSCRIPT DISPLAY "x"` (pure-reserved verb needs no predicate) parses.
- `COMMON`-as-program-attribute not regressed (targeted).

### 6.2 Negative (diagnostic) tests — one per code
COBOLNET1597 (no WHEN), 1586 (mixed operand forms), 1587 (SR14 dup file-name), 1588 (SR15 dup exception-name), 1589 (SR16 EC-I-O prefix), 1590 POP, 1591 PUSH, 1592 EXIT PERFORM CYCLE, 1593 INITIATE>1, 1594 TERMINATE>1, 1595 VALIDATE>1, 1596 GO TO in WHEN, 1597 RESUME outside WHEN, **1598 `RESUME AT proc` in a WHEN** (the adversarial reconciliation — must reject), 1599 RAISE in imp-2, 1600 CLOSE>1 in imp-1, 1601 DELETE FILE>1 in imp-1, 1602 INITIALIZE dup in imp-1, 1603 MERGE in imp-1, 1604 OPEN dup in imp-1, 1605 SORT in imp-1. Plus COBOLNET0711 for an unknown exception-name operand.
- **Region discrimination:** verify a multi-file CLOSE in imp-2 (not imp-1) is *accepted* (region A only), and RAISE in imp-1 is *accepted* (region D bans only imp-2..5). Verify INITIATE>1 in *any* region is rejected (region B).

### 6.3 Behavior (output-verifying) tests
- WHEN name-list match runs imp-2; unmatched enabled condition → WHEN OTHER (imp-3); WHEN COMMON (imp-4) chains after imp-2 and after imp-3; FINALLY (imp-5) runs on normal completion.
- **Nonfatal resume-in-place:** a nonfatal EC raised by a non-last imp-1 statement runs its WHEN, then imp-1 *continues* at the next statement (assert later imp-1 side effects occur).
- **Fatal terminate:** a fatal EC runs the WHEN then terminates abnormally (no RESUME).
- **RESUME NEXT STATEMENT** in a WHEN suppresses fatal termination and falls through past the raiser.
- **GR21:** an EC raised inside imp-2 is *not* re-caught by the same PERFORM (falls to USE/fatal).
- **GR22:** an exception enabled by a real `>>TURN` before the PERFORM stays enabled after; a purely-implicit WHEN enable does not.
- **GR14 override:** a source `>>TURN OFF` inside imp-1 for a WHEN's exception disables that WHEN.
- **EC-I-O-WARNING:** overlay turn-off deferred to end of whole PERFORM (§5.3).
- **EXIT PERFORM** inside an F3 body jumps to END-PERFORM (runs FINALLY-path fall-through correctly); EXIT PERFORM in imp-5 = CONTINUE.

### 6.4 Version-matrix tests
- Construct at `--std 2023` compiles; at `--std 85|2002|2014` emits COBOLNET0900 (edition gate).
- `PERFORM LOCATION` (paragraph named LOCATION) still parses as out-of-line PERFORM at 85/2002/2014 (continuity invariant); `LOCATION`/`FINALLY` not reserved pre-2023.

### 6.5 Staged-GAP tests
- `WHEN EXCEPTION INPUT/OUTPUT/I-O/EXTEND` emits COBOLNET0899 (documented non-support) at bind until §5.4-1 lands — a test asserting the 0899 disposition, replaced by a behavior test when the mode-matcher lands.

### 6.6 Guard
Full legacy guard (`scripts/guard-fast.sh`) green before each commit (shared `.g4` + shared `resumeStatement`/RESUME touched ⇒ full legacy conformance leg, not guard-fast alone). Run the full CI-equivalent (`-c Release` leg) before push. Update DEVLOG (top, dated) and the plan §0 NEXT-FREE marker (advance to **1618** after this lands).

---

## 7. Doc & plan sweep (same change set)
- `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12 — fold this design in as the current-state SSOT (including the §5.3 EC-I-O-WARNING scope and the §5.4 staged GAPs with their 0899 dispositions).
- `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 — grammar-batch 7/7 (PERFORM Format-3 core landed; the exception-checking runtime staged per §5.4); NEXT-FREE diagnostic → 1618; battery + branch banner.
- `VERSION_TEST_MATRIX_DESIGN.md` / `VERSION_CHANGE_REFERENCE.md` — new rows.
- Grammar docs synced to the `.g4` changes; `DOC_INDEX.md` if any doc materially changes.
- DEFERRED (NOT owner-gated — both resolved from the spec, see above): §14.9.28.3 SR8 (nested UNTIL-EXIT — spec-resolved by the non-redundancy derivation in §3.4, orthogonal scope, tracked in the §11 analysis backlog) and the FINALLY-on-abnormal-termination path (a recorded standard-defect with the default chosen in §5.4-3).
