# DESIGN — FLAG-02 / FLAG-14 migration-flagging directives (§7.3.14 / §7.3.15)

> **Status: IN PROGRESS (P13 Wave D).** Canonical deep-dive for the `>>FLAG-02` (§7.3.14) and `>>FLAG-14`
> (§7.3.15) migration-incompatibility flagging directives — the ONE shared flagging subsystem (a
> frontend-collected `FlagState`, a dedicated post-bind `FlagConformancePass`, and the two frontend-inline
> option detectors that have no bound residue). Design SSOT for the Wave-D "real FLAG-14/FLAG-02 flagging"
> item and VCR **Table 5** (rows 98, 100–113 + the directive-word rows 11/64/91/96/115). Keep CURRENT
> (describes the compiler as built); the how-we-got-here narrative lives only in `DEVLOG.md`.

## 0. What the directives are, and the master constraint

Both directives instruct the implementor's **warning mechanism** (each GR1: *"The implementor shall provide a
warning mechanism that flags the incompatibilities potentially affecting existing programs for the selected
option"*) to flag source whose **behavior differs between two editions**:

* `>>FLAG-02` (§7.3.14) — 2002 ↔ 2014 incompatibilities. **Obsolete** in 2023 (§7.3.14.1 NOTE) but still
  recognized and still required to flag.
* `>>FLAG-14` (§7.3.15) — 2014 ↔ 2023 incompatibilities. GR1's NOTE points at **Annex E.2** (*"Substantive
  changes potentially affecting existing programs"*) as the complete change list.

**Master constraint — flagging is DIRECTIVE-driven, not edition-driven.** A flag fires because a `>>FLAG-nn
OPTION ON` directive is in effect at the construct's source position, **regardless of `--std`**, and is
**always a Warning** (a migration aid; it never fails the compile). This is the axis that separates flagging
from edition conformance (`VersionConformancePass`, keyed on `_edition.Year`) and is the reason flagging is a
**separate pass**, not a bolt-on (§2, D1).

### 0.1 Directive syntax (settled from the corrected figure notes — no PDF render needed)

Both general formats (spec figures at §7.3.14.2 / §7.3.15.2) wrap the option list in **§5.2.6.4 choice
indicators** (the `|…|` bars the OCR dropped, restored in the `.md` figure notes 2026-07-19):

```
>> FLAG-02  { ALL | { |EC-PROGRAM-EXCEPTIONS | I-O-STATUS-07 | MOVE-TO-SAME-NAME
                       | RANGE-EXCEPTION-FOR-INDEX | TERMINATE-WITH-VARYING| } }  { ON | OFF }
>> FLAG-14  { ALL | { |COMPILE-TIME-ARITHMETIC-EXPRESSIONS | EVALUATE | I-O-DECLARATIVE
                       | I-O-STATUS-04 | I-O-STATUS-07 | NUM-ED-ZERO-FIGCONST | READ-PREVIOUS
                       | REF-MOD-ZERO-LENGTH | VALUE-EDITING | VALUE-FIG-CON-LENGTH | VALUE-ZERO
                       | WRITE-END-OF-PAGE| } }  { ON | OFF }
```

* One directive names **`ALL`** or **one-or-more option words** (each at most once, **any order**), then a
  **required** `ON | OFF`. (`ON` is not underlined in FLAG-02 = the implicit default when the trailing word is
  omitted; FLAG-14 requires the choice.)
* **SR1 (placement, both):** only between clauses outside the procedure division, and between statements
  within it — i.e. a free-standing directive line at a clause/statement boundary.
* **State semantics (GR2/GR3/GR5, both):** default **OFF** for every option (GR5). `ON` is **sticky-forward**
  per option until end-of-compilation-group, an **`ALL … OFF`** (turns off *all* options), or that option's own
  `OFF` (GR2/GR3). This is the same source-line-fold discipline as `>>TURN` / `>>REF-MOD-ZERO-LENGTH`.

### 0.2 Directive-word edition gating (a construct in its own right)

The directive *words* were added at different editions (Annex E.2 item 5 lists `FLAG-14` among the words new to
2023). This gates the DIRECTIVE, distinct from the flagging it drives:

* `>>FLAG-14` — a **2023 introduction** (below 2023 the word is a compilation-variable name, not a directive):
  introduction gate **COBOLNET0900** below 2023, via `ConstructRegistry.Check` (the `>>REF-MOD-ZERO-LENGTH`
  precedent). New registry construct `Flag14Directive2023`.
* `>>FLAG-02` — introduced with the **2014** standard (it flags 2002↔2014); **obsolete — NOT removed — at 2023**
  (§7.3.14.1 NOTE, PDF-confirmed p.100: *"an obsolete element in this Working Draft … to be deleted from the
  **next** edition"*). Per **§4.2.13** an implementation *shall support* obsolete elements and *shall provide a
  warning mechanism* to flag them — so at 2023 it must still **compile** and carry the **0903 obsolete warning**,
  never a `removedIn`/0902 rejection (that would refuse legal, still-supported source). Registry row
  `flag-02-directive-2014` = `introducedIn:2014, obsoleteIn:2023` (0900 below 2014, clean 2014–2022, 0903 at 2023).
  Recognized-and-flagged even when obsolete — never silently dropped. (F.2, which §4.2.13 says lists obsolete
  elements, does not name FLAG-02 — an ISO gap; the §7.3.14.1 NOTE is the direct authority.)

## 1. The verified census (adversarially reviewed 2026-07-21)

Every row: the GR4 sub-rule (verbatim-checked), the construct to detect, where it is visible today, and the
implementation increment. `[F]` = frontend-inline (no bound residue); `[B]` = bound/data walk in
`FlagConformancePass`; difficulty and any new infrastructure are noted.

### 1.1 FLAG-14 (§7.3.15.4 GR4 a–m — ALL + 12 options)

| Opt | Construct (GR4) | Visible at | Incr | Notes |
|-----|-----------------|-----------|------|-------|
| a ALL | fan-out to b–m | directive parse | 0 | sets every FLAG-14 option; `ALL OFF` is the GR2 reset |
| b COMPILE-TIME-ARITHMETIC-EXPRESSIONS | a compile-time arithmetic expr with a real operator | `[F]` `CompileTimeExpressionEvaluator` EvalArith (Add/Mul arm) | 2 | E.2 item 6. Guard on operator present (sole literal not flagged) |
| c EVALUATE | a `>>EVALUATE` directive with both a WHEN and a WHEN OTHER | `[F]` `ConditionalCompilationProcessor` Evaluate/When frames | 2 | E.2 item 8. NOT the EVALUATE statement |
| d I-O-DECLARATIVE | an INVALID-KEY-capable I-O stmt (or AT-END-capable READ) without that phrase while a USE INPUT/OUTPUT/I-O/EXTEND declarative is in effect | `[B]` bound I-O nodes + declaratives model | 4 | E.2 item 19. Needs statement→file→open-mode→declarative join (new analysis) |
| e I-O-STATUS-04 | a condition testing a FILE STATUS item against `'04'` | `[B]` needs status-ref tagging | 4 | E.2 item 15. New analysis: link data-refs to their FILE-STATUS role |
| f I-O-STATUS-07 | a condition testing a FILE STATUS item against `'07'` | `[B]` needs status-ref tagging | 4 | E.2 item 16. Same machinery as e |
| g NUM-ED-ZERO-FIGCONST | figurative ZERO in a VALUE clause of a numeric-edited item | `[B]` `GateData`/`DataItem` (DataBinder.cs:1104 `isZeroWord`) | 1 | E.2 item 28. **Same predicate as l** — one detector serves both |
| h READ-PREVIOUS | a `READ … PREVIOUS` | `[B]` `BoundKeyedRead` Previous (already matched VCP:200) | 0 | E.2 item 22. The first end-to-end slice |
| i REF-MOD-ZERO-LENGTH | a ref-mod where the `>>REF-MOD-ZERO-LENGTH` directive is **not explicitly** ON/OFF **and** EC-BOUND-REF-MOD is on | `[B]` `RefModPlace` + `RefModZeroLengthState` (tri-state ext.) + TurnState | 3 | E.2 item 23. Needs tri-state `RefModZeroLengthState` + EC read |
| j VALUE-EDITING | a numeric-edited item whose VALUE is a literal with no editing symbols | `[B]` `GateData`/`DataItem.RawValue` + Pic | 1 | E.2 item 29 |
| k VALUE-FIG-CON-LENGTH | a figurative constant in a VALUE clause of an item with no specified length | `[B]` `GateData`/`DataItem` (fig word + length model) | 1 | E.2 item 11. GR4 spells it `VALUE-FIG-CON-NO-LENTH` (typo); the accepted word is the figure's `VALUE-FIG-CON-LENGTH` |
| l VALUE-ZERO | a numeric-edited item whose VALUE specifies figurative ZERO | `[B]` = same as g | 1 | E.2 item 28. Deduped with g |
| m WRITE-END-OF-PAGE | a WRITE that *allows* END-OF-PAGE (file has LINAGE) but omits the AT EOP phrase | `[B]` `BoundWrite` + File LINAGE, no AtEop | 1 | E.2 item 19 family (AT-EOP default); GR4 m normative |

### 1.2 FLAG-02 (§7.3.14.4 GR4 a–f — ALL + 5 options)

| Opt | Construct (GR4) | Visible at | Incr | Notes |
|-----|-----------------|-----------|------|-------|
| a ALL | fan-out to b–f | directive parse | 0 | |
| b EC-PROGRAM-EXCEPTIONS | a `>>TURN` for EC-ALL/EC-PROGRAM/EC-PROGRAM-ARG-OMITTED/EC-PROGRAM-NOT-FOUND in an element that calls any function or invokes any method | `[B]` TurnState + element-scope call/invoke aggregation | 4 | New: whole-element "has a function-call or method-invoke" property. No clean Annex-E anchor (GR4 is normative) |
| c I-O-STATUS-07 | a CLOSE with WITH NO REWIND or the UNIT phrase | `[B]` `BoundClose` — **needs a NoRewind model bit** (`BoundCloseKind` has ReelUnit but not NoRewind) | 3 | E.2 item 16. `CobolIO.g4 closeOption` already parses both |
| d MOVE-TO-SAME-NAME | a MOVE whose send/receive resolve to the SAME DDE, and (1) category alphanumeric-edited, or (2) a subordinate OCCURS…DEPENDING whose DEPENDING item is subordinate to that DDE | `[B]` `MoveBinder`/`MoveClassifier` (same-DDE = symbol identity) | 3 | GR4 normative (no Annex-E re-item) |
| e RANGE-EXCEPTION-FOR-INDEX | an index-assignment/arithmetic SET with an index receiver, when EC-RANGE-INDEX checking is enabled | `[B]` `BoundSet*`/SetIndexTarget + TurnState | 3 | GR4 normative |
| f TERMINATE-WITH-VARYING | a TERMINATE of a report whose RD contains a VARYING clause | `[B]` `ReportWriterBinder` BindTerminate + ReportModel `Varyings` | 1 | GR4 normative |

## 2. Structural decisions (the singular-pattern end-state)

**D1 — a dedicated `FlagConformancePass`, NOT a bolt-on `VersionConformancePass`.** Flagging is an orthogonal
trigger axis (user directive-state vs `--std`), always Warning, and two of its detectors have no bound-tree home
at all (b, c are frontend-only). Bolting per-case `if (flagState.IsOnAt…)` onto VCP's `GateStatement` switch
would dilute that pass's documented single-responsibility ("sole owner of edition gating", the two-arm
disjointness invariant) and its "one policy = `ConstructRegistry.Check` funnel" identity. `FlagConformancePass`
is a sibling post-bind pass in `Cobol.Net.Compiler/Validation`, invoked from `BinderDriver` right after the
`BindPipeline.GroupTail()` loop with `(GroupBindContext, FlagState, sink)`.

**D2 — a PARSE-TREE visitor (the source-line reason), reusing the generated ANTLR base visitor.** The flag fold
is **line-sensitive** (GR2: a flag applies to the text *following* the directive), so every detector needs its
construct's source line. `BoundStatement` does **not** carry a uniform source line — only a handful of nodes hold
a `SourceLine` (for DEBUG-LINE), and e.g. `BoundTerminate` has none — so a bound-tree walk cannot anchor the
fold. The **parse tree** carries `ctx.Start.Line` on every node, and because `FlagDirectiveProcessor` collects
events on the FINAL preprocessed text, those `Start.Line`s are directly comparable to `FlagEvent.Line` (the same
basis TurnState/RefModZeroLength use). Therefore `FlagConformancePass` is a `CobolParserCoreBaseVisitor<object?>`
over `group.Tree` — the SAME traversal mechanism VCP's `ParseArm` uses (the generated, drift-proof visitor; no
bespoke switch — `feedback_path_a_leverage_tooling`). Options whose trigger is **syntactic** (READ PREVIOUS,
CLOSE NO REWIND/UNIT, WRITE-without-EOP, SET-index, the VALUE-clause data options, MOVE shape) are decided from
the parse node directly; options needing a **resolved fact** (TERMINATE's report has a VARYING clause; a MOVE's
operands share a DDE; a condition tests a FILE STATUS item) look that fact up **by name** in the resolved models
reachable from `GroupBindContext` (`Units[].Data` forest, `.Reports`, `FilesByName`) — a cheap dictionary hit,
never a re-run of operand binding.

**D3 — ONE FlagState, ONE FlagOption catalog, ONE directive-line parser.** A single `FlagState`
(`Cobol.Net.Compiler/Binding`, cloned from `RefModZeroLengthState` and extended per-option) folds the toggle
events to `IsOnAt(siteLine, FlagOption)`; a single `FlagOption` enum + descriptor table enumerates all 17 real
options across both directives (ALL is a parse-time fan-out, not a stored option); a single `FlagDirectiveLine`
parser splits `>>FLAG-nn [ALL | opt…] {ON|OFF}` into `(FlagDirective, ISet<FlagOption>, bool On)` and is reused
by BOTH collection sites (§3).

**D4 — TWO collection sites, forced by construct-visibility across pipeline stages (not a fork).**

* **Bound options** (all `[B]` rows) need events **line-anchored to final tokens**, so they are collected
  **post-COPY** by a new `FlagDirectiveProcessor` on the FINAL text (the `>>TURN` / `>>REF-MOD-ZERO-LENGTH`
  stage template; H3 line-count-preserving; introduction-gates the directive word). Its events build the
  compiler-side `FlagState`.
* **Frontend-only options b, c** are CONSUMED by the conditional-compilation stage (pre-COPY) and never reach
  the bound tree, so they MUST be flagged inside `ConditionalCompilationProcessor`, which already walks every
  `>>` line in order. It tracks FLAG on/off state as it scans (a `FlagScanState` using the SAME
  `FlagDirectiveLine` parser) and emits b/c inline via the `DiagnosticBag`.

Both sites reuse the ONE parser + ONE FlagOption catalog. The `>>FLAG` lines survive the CC stage via a
`leaveFlagDirectives` arm (mirroring `leaveTurnDirectives`), so the post-COPY `FlagDirectiveProcessor` collects
and blanks them. **Copybook-internal `>>FLAG` directives ride the CC-in-COPY fix** (§7.2.1; a separate Wave-D
item) — a documented limitation shared by every copybook-internal directive today, not specific to FLAG.

**D5 — diagnostics: two catalog codes, per-option identity.** Two Warning descriptors —
`Flag02Warning` (**COBOLNET1620**) and `Flag14Warning` (**COBOLNET1621**) — are the channel; each emitted
`EditionDiagnostic` carries the **per-option** `ConstructId` (suppress-key), `Message` (naming the option + the
behavior change), and `Citation` (the exact `§7.3.1x.4 GR4 <letter>` + Annex-E item). This gives per-option
citation precision and suppressibility while keeping the band to two codes (the reflected `All` auto-lists them;
the `EveryEmittedCode_IsACatalogDescriptor` drift test is satisfied). Severity is hard-coded
`EditionSeverity.Warning` (NOT `EditionSeverityPolicy`, which is edition-derived).

**D6 — VCR anchoring.** The directive-WORD rows (11/64/91/96/115) anchor to the new `constructs.json` rows
`Flag14Directive2023` / `Flag02DirectiveObsolete2023` (`<!-- gate:… -->`, genuine edition gates). The per-OPTION
rows (98, 100–113) are directive-driven, not edition gates, so they carry `<!-- ref-only -->` pointing at
`FlagConformancePass` once their detector lands (confirm against `VcrDriftTests`; do NOT invent a
`constructs.json` edition row for a non-edition-gated behavior).

## 3. The three seams (each an exact copy of a proven template)

1. **COLLECT** — `record FlagEvent(int Line, FlagDirective Which, bool On, IReadOnlyList<FlagOption> Options)`
   (Options empty ⇒ ALL). New `FlagDirectiveProcessor` (`Cobol.Net.Frontend/Preprocessor`), a line-by-line
   clone of `RefModZeroLengthDirectiveProcessor` (blank-not-delete, H3-preserving, `ConstructRegistry.Check`
   introduction gate on the directive word). Wired by a `leaveFlagDirectives:true` arm in
   `ConditionalCompilationProcessor.Process` (before the `KnownIgnoredDirectives` fallthrough; FLAG-02/14 stay
   in the set so legacy callers still consume them) and a new stage call in `Frontend.Preprocess` after the
   REF-MOD-ZERO-LENGTH stage, exposing `Frontend.FlagEvents`. The frontend-only b/c flags are emitted here-adjacent
   inside `ConditionalCompilationProcessor` from its own in-scan `FlagScanState`.
2. **THREAD + FOLD** — `FlagState` (`Cobol.Net.Compiler/Binding`): `Build(events)` + `IsOnAt(int siteLine,
   FlagOption)` folding "last toggle strictly before the site wins, default OFF" per option, with ALL fan-out
   and `ALL OFF` reset. Threaded `frontend.FlagEvents` → `CompilerDriver:114`
   (`emitter.Bind(tree, edition, TurnEvents, RefModZeroLengthEvents, FlagEvents)`) → `CSharpEmitter.Bind` →
   `BinderDriver` (`FlagState.Build(flagEvents)`), carried on `BindSession` alongside `RefModZeroLength`.
3. **EMIT** — `FlagConformancePass.Run(group, flagState, sink)` invoked right after `VersionConformancePass.Run`
   in `BinderDriver`, per-option detector methods emitting
   `sink.Report(new EditionDiagnostic(code, EditionSeverity.Warning, constructId, msg, where, cite))` gated by
   `flagState.IsOnAt(node.Line, option)`.

## 4. Increment plan (ONE option-or-group per commit, conformance test + wave-local gate each)

* **Incr 0 — CORE + first slice (both directives, both codes, purely syntactic).** `FlagOption`/`FlagDirective`
  + option catalog; `FlagDirectiveLine` parser; `FlagEvent` + `FlagDirectiveProcessor` (+ `leaveFlagDirectives`
  wiring + `Frontend.FlagEvents`); `FlagState`; thread through the drivers; `FlagConformancePass` (the ANTLR
  parse-tree visitor) with two syntactic detectors — **FLAG-14 h READ-PREVIOUS** (`readDirection().PREVIOUS()`)
  and **FLAG-02 c I-O-STATUS-07** (`closeOption` = `UNIT` or `NO REWIND`; parse-visible, so NO `BoundClose`
  model change is needed — a simplification over the original Incr-3 plan). Catalog 1620 (Flag02Warning) + 1621
  (Flag14Warning), both exercised. Goldens: `>>FLAG-14 READ-PREVIOUS ON` + a READ PREVIOUS ⇒ warning (OFF ⇒
  none); `>>FLAG-02 I-O-STATUS-07 ON` + a CLOSE WITH NO REWIND ⇒ warning. Directive-word introduction gate +
  `constructs.json` rows land here too (or the immediately-following Incr 0b if kept tight).
* **Incr 1 — the data/VALUE + report/WRITE options** (parse arm + resolved lookups), delivered as sub-increments:
  * **1a (DONE)** g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO — `VisitDataDescriptionEntry`; numeric-edited via the ONE
    `PictureAnalyzer` (discard edition); figurative ZERO via `FirstDescendant<FigurativeConstantContext>`.
  * **1c (DONE)** m WRITE-END-OF-PAGE (WRITE + `FileModel.Linage` + no `writeAtEndOfPage`) + FLAG-02 f
    TERMINATE-WITH-VARYING (TERMINATE + a report whose fields carry `Varyings`) — source-name lookups built in
    `Run` from `GroupBindContext.Units[].Data`.
  * **1b (DONE for j)** j VALUE-EDITING — a numeric-edited VALUE that is a LITERAL (not figurative) with no
    editing symbols (§13.18.63 SR6/SR11 + E.2 item 29: numeric literals get editing auto-supplied, alphanumeric/
    national literals now require it). "Editing symbols" scanned from the literal via the unambiguous insertion set
    `{space / , . + - $ *}` + trailing CR/DB; `0`/`B` insertions are a documented rare false-negative.
  * **1b (DONE) k VALUE-FIG-CON-LENGTH** — a figurative-constant VALUE (any figurative, via
    `FirstDescendant<FigurativeConstantContext>`) on a data item with **no specified length** (§7.3.15.4 GR4 k;
    E.2 item 11). "No specified length" is decided precisely from the parse: **no PICTURE clause**, **no
    length-implying USAGE** (`UsageGivesNoLength` — DISPLAY/absent has none without a PICTURE; COMP-*/INDEX/POINTER/
    float/binary families imply one — CLI-verified that `USAGE DISPLAY VALUE SPACE` IS flagged and `USAGE COMP-2
    VALUE ZERO` is NOT), and **not a group** (`HasSubordinates` — the immediately-following sibling entry is a real
    subordinate, level 2–49; a group's figurative VALUE is filled to the subordinates' length so its length IS
    specified, §13.18.63 SR13). Restricted to real data levels (1–49, 77) — a level-88 condition-name reaches
    `VisitDataDescriptionEntry` via `valueClause` and is excluded (`IsRealDataLevel`). k reaches ANY item (not just
    numeric-edited); g/l/j remain numeric-edited-gated.
* **Incr 2 (DONE)** the frontend-inline options b COMPILE-TIME-ARITHMETIC-EXPRESSIONS + c EVALUATE, emitted in
  `ConditionalCompilationProcessor` (these are consumed at the CC stage, never reach the bound tree). A
  `FlagScanState` tracks FLAG-14 ON/OFF as the stage scans (still leaving the >>FLAG line for the post-COPY
  bound-option `FlagState`); `DirectiveDiag.FlagWarn` emits with the same code/message shape as
  `FlagConformancePass`. c: the `Frame` records `>>WHEN`/`>>WHEN OTHER` presence, flagged at `>>END-EVALUATE`. b:
  `EvaluateOperandText`/`EvaluateCceText` call `diag.FlagArithmetic` on the parsed fragment (a real addOp/mulOp,
  evaluated context only).
* **Incr 3 — the state-coupled options** i REF-MOD-ZERO-LENGTH (tri-state `RefModZeroLengthState`
  extension + EC-BOUND-REF-MOD read), d MOVE-TO-SAME-NAME (same-DDE via name resolution), e
  RANGE-EXCEPTION-FOR-INDEX (SET-index + EC-RANGE-INDEX TurnState read).
* **Incr 4 — the new-analysis options** d I-O-DECLARATIVE, e/f I-O-STATUS-04/07 (FILE-STATUS reference
  tagging), FLAG-02 b EC-PROGRAM-EXCEPTIONS (element-scope call/invoke aggregation). Each is real
  cross-cutting analysis designed here to spec; implemented last because it introduces new machinery, NOT a
  documented non-conformance. Wave I merges only when every option is landed or an owner decision stages one.

## 5. Testing

Per `feedback_conformance_tests_per_feature`: every option ships a positive golden (ON ⇒ the warning) in the
SAME commit; the wave-local gate is fresh build + characterization + the FLAG unit/corpus filter + a CLI probe.
Because flagging is a Warning (never fatal), a golden asserts on stderr/warning text, not exit code. The
comprehensive gate (full Conformance + legacy guard) runs once pre-merge; the GnuCOBOL differential is a triage
net (flagging adds warnings, never rejects — expect zero accept/reject flips). A new 2023/2014 golden that the
frozen legacy pipeline cannot reproduce needs a `GreenfieldOnly` exclusion in the same commit
(`feedback_legacy_suite_on_shared_corpus`).
