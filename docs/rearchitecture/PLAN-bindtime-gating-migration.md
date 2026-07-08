# Bind-time edition-gating migration — execution plan (LEDGER)

- **Type:** LEDGER (execution-grade, resumable). **Owner directive:** "implement correctly regardless of rework cost."
- **Origin:** rearch P2.7 (DEVLOG 679) proved forward `{Gate}?` predicate stamping unsound (risk R1 — ANTLR
  evaluates hoisted predicates speculatively at the stuck token). The R1-mitigation kept the reverse *signatures*
  (`EditionGateHints`). This plan removes them the correct way.
- **Related:** `DESIGN-edition-framework.md` §5 (R1); `PHASE-02-editions-assembly-diagnostic-registry.md` (step 8
  superseded by this); recon `wf_9c48ce3f`.

## Goal

Move edition **introduction gating** from parse-time REJECTION predicates (`{isXXXX()}?`) to **bind-time**
`ConstructRegistry.Check` at each construct's recognition point — the exact pattern the five inline gates already
use (ALTER / bare-GO-TO / ROUNDED MODE / END-ACCEPT / CALL ON OVERFLOW). This removes the speculative-eval class of
wrong diagnostics (R1) AND the reverse-signature heuristics, and unifies ALL gating on the ONE `Check` funnel.

## Architecture decision — in-binder Check, NOT a dedicated EditionValidator pass

Keep introduction-gating at each construct's recognition point in the binder. Rationale: the ONE policy funnel
(`ConstructRegistry.Check`, `ConstructRegistry.cs:30`) + `EditionSeverityPolicy` already centralize the decision;
the `where` context (data-name / FD / class-name / paragraph) is free at the recognition point and a separate pass
would re-walk to re-derive it (duplicate dispatch); 25+ live Checks already gate in-binder (one mechanism —
`feedback_singular_pattern`); and introduction rows fire at heterogeneous nodes (data entries, statements, division
headers, compilation units, REPOSITORY entries) with no cheap shared traversal. A dedicated `EditionValidator` is
reserved for genuinely cross-cutting per-edition rules (the G7 remit), not per-construct introduction gating.

## Invariants (verified by recon)

- `Check(EditionInfo edition, IDiagnosticSink sink, string id, string where)` (`ConstructRegistry.cs:30`) — silent
  when available; emits `COBOLNET0900` below `IntroducedIn` for introduction-only rows. Call forms by layer:
  StatementBinder `Check(data.Edition.Edition, data.Edition, Constructs.X, "…")`; DataBinder
  `Check(Edition.Edition, Edition, Constructs.X, "…")`; OoClassTable/PicInfo `Check(edition.Edition, edition, …)`.
- **Every grammar ungate is grammar-touching ⇒ FULL legacy guard mandatory in the SAME change set** (regen the
  ANTLR parser; NIST 353 MATCH). Per-commit gate: battery 2055 conformance / 224 unit unchanged, guard 353 MATCH.
- **Per-commit verification:** at the edition BELOW `introducedIn`, the construct now yields `COBOLNET0900` with the
  EXACT construct id (assert the id, not merely "some 0900" — closes the `VersionMatrixTests` weakness where an
  incidental earlier 0900 masks a missing one); at/above, the feature compiles + runs byte-identical; the deleted
  `EditionGateHints` arm has no other referent; DEVLOG per commit.

## STATUS

`IN PROGRESS — Cluster 9 next (LOCK MODE).` Clusters completed: **1–7 (DEVLOG 680–686) + 8a (687) + 8b (688)**.
EditionGateHints removals: C1 −3 (+1 interim SET arm), C2 −2 (+ deleted `COBOLNET0883`), C3 −2, C4 −2, C5 −4, C6 −2,
C7 −1, C8a −3 (incl. interim SET arm retired), C8b −2 (special-names FOR + repository-CLASS). Bonus in C7: the
DEVLOG-679 `SUPPRESS`@85 R1 false-positive is root-fixed. Battery green: conformance 2055 · unit 224 · FULL legacy
guard 353 MATCH.

## MOVE_TO_BINDTIME (24) — ordered clusters (each = one commit: ungate + regen + Check + delete hints arm + below-edition test + FULL guard)

- [x] **Cluster 1 — proof of pattern (dead-Check activation; ZERO new binder code) — DONE (DEVLOG 680).** Ungate + delete hints; the
  already-present Check goes live below its edition. (Also: cleaned ALLOCATE/FREE `where` double-citation; added an
  interim `SET`-token arm to the set-object-reference signature — its 0900 had come incidentally from the USAGE parse
  gate, which now parses; retired in Cluster 8.)
  - allocate-2002 — ungate `CobolParserCore.g4:680`; Check ALREADY at `StatementBinder.Ptr.cs:91`; delete `EditionGateHints.cs` ALLOCATE arm.
  - free-2002 — ungate `CobolParserCore.g4:681`; Check ALREADY at `Ptr.cs:127`; delete FREE arm.
  - usage-object-reference-2002 — ungate `CobolData.g4:328` (last gated usageKeyword; NATIONAL/BIT/POINTER siblings already ungated); Check ALREADY at `PicInfo.cs:651`; delete OBJECT-REFERENCE arm.
- [x] **Cluster 2 — call-by-value (dead manual-gate cleanup) — DONE (DEVLOG 681).** Ungated `CobolParserCore.g4:958`; replaced the manual `COBOLNET0883` gate with `Check(… Constructs.CallByValue2002, "the CALL … BY VALUE phrase")`; deleted the 2 BY/VALUE hints arms. 0883 fully removed (no test pinned it).
- [x] **Cluster 3 — invoke + delete-file (first NEW Checks) — DONE (DEVLOG 682).** DELETE two-alt disambiguation verified (guard + `DELETE F1 RECORD`@85 clean); beyond-recipe: added `using CobolNet.Editions;` to `StatementBinder.Oo.cs`.
  - invoke-2002 — ungate `CobolParserCore.g4:716`; ADD Check first line of `OoBindInvoke` (`StatementBinder.Oo.cs:280`); delete INVOKE arm. (Leave the 2023 inline-method `x(...)` at :717 gated.)
  - delete-file-2023 — ungate `CobolParserCore.g4:679`; ADD Check first line of `KeyedBindDeleteFile` (`StatementBinder.KeyedIo.cs:223`); delete DELETE-FILE arm. **AMBIGUITY (resolved):** two DELETE-leading alts disjoin on the 2nd token (`FILE`∉cobolWord); keep `deleteStatement` first (:678); guard is the arbiter.
- [x] **Cluster 4 — start-with-length + stop-run-status — DONE (DEVLOG 683).** STOP-status recognition was parse-then-drop → extracted a `BindStop` helper; the no-`WITH` `STATUS` alt now covered too.
  - start-with-length-2002 — ungate `CobolIO.g4:459`; ADD Check `if (kp?.startWithLength() is not null)` at `KeyedIo.cs:282`; delete arm. (Sibling StartFirstLast already Checks in the same method.)
  - stop-run-status-2002 — ungate `CobolControlFlow.g4:245`; **ADD a phrase read** at `StatementBinder.cs:207` (currently parse-then-drop): `if (stop.stopStatusPhrase() is not null) Check(…)`; delete arm. BONUS: covers the no-`WITH` `STATUS …` alt (a pre-existing residue).
- [x] **Cluster 5 — data-division clauses (all in `DataBinder.BindEntry` / ODO) — DONE (DEVLOG 684).** Beyond-recipe: added `using CobolNet.Editions;` to `DataBinder.cs` + `OdoModel.cs`.
  - based-clause-2002 — ungate `CobolData.g4:242`; Check at the basedClause arm `DataBinder.cs:1053`; delete arm.
  - type-clause-2002 — ungate `CobolData.g4:255`; Check at typeClause arm `DataBinder.cs:1059`; delete arm. (Report-Writer TYPE at `CobolReportWriter.g4:97` is a different rule — no conflict.)
  - typedef-def-2002 — ungate `CobolData.g4:262`; Check at typedefClause arm `DataBinder.cs:1057`; delete arm.
  - occurs-dynamic-2014 — ungate `CobolData.g4:346` (Format-4 `{is2014()}?`); Check inside `if (occ.DYNAMIC() is not null)` at `OdoModel.cs:221`; delete the OCCURS/DYNAMIC/CAPACITY arms.
- [x] **Cluster 6 — goback-returning + procedure-returning — DONE (DEVLOG 685).** Double-diag handled (GOBACK 0880 vs RETURNING 0900 — 0900 subsumes when RETURNING present). Beyond-recipe: `using CobolNet.Editions;` in `DataBinder.Linkage.cs`.
  - goback-returning-2002 — ungate `CobolParserCore.g4:1140`; Check at the `g.dataReference()` branch `Call.cs:215`; delete arm. **DOUBLE-DIAG:** GOBACK itself is 2002+ (already emits `COBOLNET0880` at `Call.cs:210-213`) — suppress the RETURNING Check when the enclosing GOBACK already errored (preserve the precise message); assert exactly one 0900/0880.
  - procedure-returning-2002 — ungate the returningClause predicate at `CobolParserCore.g4:487` (LEAVE the sibling raisingClause predicate); Check at `DataBinder.Linkage.cs:144`; delete arm. (No transitive coverage — genuinely needs the Check.)
- [x] **Cluster 7 — class + interface definitions (DO TOGETHER — shared hints arm) — DONE (DEVLOG 686).** Message now names the unit (`class definition 'X'`); the DEVLOG-679 `SUPPRESS`@85 R1 leak is root-fixed. `using CobolNet.Editions;` in `OoClassTable.cs`.
  - class-definition-2002 — ungate `CobolParserCore.g4:145` classDefinition alt; Check in OoClassTable.Build class loop after `name` (~`OoClassTable.cs:461`).
  - interface-definition-2002 — ungate `CobolParserCore.g4:145` interfaceDefinition alt; Check in the interface loop after `iname` (~`OoClassTable.cs:362`).
- [x] **Cluster 8 — set-object-ref + special-names-FOR + repository-class (SPLIT 8a/8b) — DONE.**
  - [x] **8a set-object-reference-2002 (DEVLOG 687).** Ungated `CobolParserCore.g4:1083`; Check at top of `OoBindSetObjectRef`; retired 3 arms (incl. the Cluster-1 interim SET arm). AMBIGUITY cleared.
  - [x] **8b special-names-FOR (3 sites) + repository-class (DEVLOG 688).** New `symbolicCharactersClause` binder branch (was unbound) + new `re.CLASS()` repository branch; `using CobolNet.Editions;` in `DataBinder.Switches.cs`.
  - special-names-for-national-2002 — ungate 3 sites (`CobolSpecialNames.g4:60,75,85`); **3 reads** — `AlphabetBind` (`Switches.cs:241`), `SwitchBindClass` (`Switches.cs:342`), and a NEW symbolicCharactersClause branch (~`Switches.cs:209`, otherwise unbound); delete arm. (Its own commit — more work.)
  - repository-class-2002 — ungate `CobolParserCore.g4:455`; NEW branch in the repository loop (~`DataBinder.cs:169`) `else if (re.CLASS() is not null …)`; delete arm.
- [ ] **Cluster 9 — LOCK MODE.** lock-mode-clause-2002 — ungate `CobolIO.g4:69`; Check at the lockModeClause branch `DataBinder.cs:414`; delete the `LOCK when Next==MODE` arm. (LOCK+MODE hard-reserved; unique two-token lead.)
- [ ] **Cluster 10 — record-lock phrase (AMBIGUITY_RISK + fixes a latent gap).** record-lock-phrase-2002 — ungate `CobolIO.g4` READ :290/:292, WRITE :344, REWRITE :385; Check at `CheckRecordLockPhrase` (`StatementBinder.FileLock.cs:38`) top + at the ADVANCING-ON-LOCK recognition (`KeyedIo.cs:160`). **Do AFTER Cluster 9** (same CobolIO.g4). Three co-located optional gated tails + ADVANCING shared with WRITE's BEFORE/AFTER — FULL guard is the tail-prediction arbiter. **LATENT GAP FIXED:** no EditionGateHints arm exists for this today (below-2002 `READ F WITH LOCK` → generic error); add a real below-2002 assertion for this id.
- [ ] **Cluster 11 — position-safe reservation words (guard-gated; KEEP_PARSE_GATED fallback).** These are `cobolWord`/`_dataNameTokens` members but position-disjoint (entry-leading keyword in a closed alt set / a dedicated `IS? PROTOTYPE` tail). **Ship each ONLY if the FULL guard is byte-identical; ANY diff ⇒ keep it parse-gated (retain its hints arm).** Do LAST so a fallback never blocks the hard-reserved wins.
  - repository-interface-2002 — ungate `CobolParserCore.g4:456`; NEW `re.INTERFACE()` branch in the repository loop.
  - repository-property-2002 — ungate `CobolParserCore.g4:457`; Check alongside the existing `OoRepositoryProperties.Add` at `DataBinder.cs:169`.
  - function-prototype-2002 — ungate `CobolParserCore.g4:191`; Check at `CSharpEmitter.Call.cs:245` after `isPrototype` (EditionContext in scope), or a bind-side site at the FUNCTION-ID prototype partition (`StatementBinder.Udf.cs`).

## KEEP_PARSE_GATED (6) — reservation-word residue (correctness limit, NOT cost)

These keywords are user-defined words below their edition (in `cobolWord` + `_dataNameTokens`); the `{isXXXX()}?`
predicate is what promotes them to operator/keyword. Ungating would re-tokenize/re-parse valid lower-edition
programs (e.g. a COBOL-85 data item named `XOR`). They stay parse-gated; the diagnostic stays reliable because a
parse error AT the reserved token can only be the gated edge (below the edition the token parses as a user word and
never errors). EditionGateHints keeps ONLY these arms after the migration:

| construct | reserved token(s) | ambiguity if ungated |
|---|---|---|
| unlock-statement-2002 | UNLOCK | `01 UNLOCK PIC X` at '85 |
| property-clause-2002 | PROPERTY (data ctx; + the valueClause disambiguation guards `CobolData.g4:382,389`) | `01 PROPERTY PIC 9` / VALUE operand |
| logical-xor-operator-2023 | XOR / EXCLUSIVE_OR | `COMPUTE Y = XOR` at <2023 |
| boolean-operators-2002 | B_AND/B_OR/B_XOR/B_NOT (+ COMPUTE-F2 lookahead) | `COMPUTE Y = B-AND` at '85 |
| file-sharing-clause-2002 | SHARING | `OPEN INPUT SHARING` (file named SHARING) — the OPEN leg pins the construct |
| retry-phrase-2002 | RETRY | `READ RETRY` / `OPEN INPUT RETRY` (file named RETRY) |

Residual `EditionGateHints` = a minimal reserved-token→constructId map (UNLOCK/XOR/EXCLUSIVE_OR/B-ops/SHARING/RETRY)
+ two lookahead cases (COMPUTE-with-a-B-op-ahead → BooleanOperators2002; PROPERTY-in-dataDescription → PropertyClause2002)
+ the JSON/XML vendor COBOL0313 token check. Keep it in place (shrink, don't rewrite — `feedback_singular_pattern`);
rename/redocument it as the *reserved-word introduction-hint* table once down to these.

## Risks to resolve empirically (the FULL guard is the arbiter)

1. ANTLR ALL(*) selection after removing a predicate that doubled as a disambiguator — delete-file (2 DELETE alts),
   set-object-reference (NULL/SELF/SUPER), record-lock (3 co-located tails; ADVANCING shared). Green byte-identical
   guard = accept; any regress ⇒ keep that construct parse-gated with a token-keyed hint.
2. Position-safe reservation words (Cluster 11) — guard is the acceptance test; do NOT ship on argument alone.
3. goback-returning double diagnostic (0880 vs 0900) — suppress-when-already-errored; assert exactly one.
4. Parse-then-drop points that must be CREATED, not annotated: stop-run-status (`stopStatusPhrase` never read),
   special-names-FOR (SYMBOLIC CHARACTERS entirely unbound), repository-class/interface (loop drops CLASS/INTERFACE).
5. function-prototype recognition is emit-time (`CSharpEmitter.Call.cs:245`) — acceptable (sink in scope) or move bind-side.
6. Out-of-scope latent inconsistency (log, don't fix): ReservedWords.Table marks several bind-time keywords `R85=false`
   ("user-word-legal at '85") yet the lexer never admits them as user words — the ungate is tokenization-neutral anyway.
