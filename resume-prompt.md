# COBOL.NET — Next-Session Kickoff Prompt

> ⛔🔥 **PIVOT (2026-06-08, DEVLOG 457 — THIS supersedes the byte-engine plan below).** The owner directed a
> **blank-slate rewrite**: a NEW compiler that translates COBOL → **idiomatic typed-native C# source, compiled by
> Roslyn** (a COBOL record IS a .NET `record struct`; an elementary item IS a native field). **There is NO byte
> `ProgramState` substrate** — do not reintroduce it, do not "fall back" to the legacy byte engine. The legacy
> `CobolSharp.Compiler` is kept ONLY as a differential oracle until cut-over. **READ `docs/COBOLNET_DESIGN.md` FIRST**
> — the decision-complete SSOT (bound-tree pipeline [NO lowered IR], data model, native scaled-integer numerics,
> PC-dispatcher control flow, REDEFINES, strings, files, interprogram, OO, conditions/EC, intrinsics, project
> reorg/rename to Cobol.NET/cobol.exe, no-god-class structure, C# 14 usage, the §18 settled decisions, and the G0–G8
> build order). `COBOLNET_ARCHITECTURE.md` is the brief overview. Memory: `feedback_complete_dotnet_migration_no_byte`,
> `feedback_fully_autonomous_push`. Tests may break mid-transition; the bar is 100% green at completion.
>
> **MISSION (restated 2026-06-09): drive to FULL ISO/IEC 1989:2023 AND correct support for ALL PRIOR EDITIONS
> (1985 / 2002 / 2014), validated by the VERSION TEST MATRIX (test the compiler as N per-edition compilers).** Two
> interlocking tracks: (T1) the **version-correctness framework** — `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
> edition-change checklist) + `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix); and (T2) the
> **feature/corpus drive** — green the NIST corpus + implement the M2/M3/M4 catalog. Every behavior that changed across
> editions is gated by `DialectLevel`; new features enabled only at ≥ their edition; obsolete/removed flagged. (Memories
> `feedback_version_test_matrix`, `feedback_version_targeted_semantics`.)
>
> **CI NOTE (RETIRED at DEVLOG 596 — kept for history): the DEVLOG-554 rule ("commit the regenerated parser
> with any .g4 change") is OBSOLETE. The regen has been path-portable + fail-hard since DEVLOG 555/556, and
> as of DEVLOG 596 `src/Cobol.Net.Frontend/Generated/` is fully UNTRACKED (the 8 leftover tracked files were
> removed — their checkout mtimes were suppressing CI regeneration, which is how the d6c8143 stale-TYPE-gate
> incident happened, DEVLOG 591). A fresh checkout ALWAYS regenerates (java + pwsh are build prerequisites,
> both preinstalled on the GitHub runner images); a failed generation FAILS the build. There is no committed
> parser to keep in sync anymore.**
>
> ⛔🏗 **GO-FORWARD SSOT (DEVLOG 665, 2026-07-07 — committed d455f56): the roadmap for ALL future work is now
> `docs/COBOLNET_REARCHITECTURE_PLAN.md`** — a COMPLETE, resumable, execution-grade **17-phase** plan taking the
> compiler from its current state (2036 conformance · 213 unit · 32 characterization GREEN) to **clean architecture + 100% ISO (all editions)
> + a selectable Roslyn↔CIL backend**. It **SUBSUMES the prior feature/ISO drive**: the remaining ISO features (M2 OO
> residue, national/boolean, M3-2014, M4-2023, EC remnants, the version-gating audit) are now its **phases 09–14**, to
> be landed ON the rearchitected foundation — NOT bolted onto the current code. **A NEW SESSION:** (1) read that plan's
> **§0 RESUME PROTOCOL**; (2) obey its status banner → the current `docs/rearchitecture/PHASE-NN-*.md`'s STATUS line →
> execute its numbered steps, battery-green at every commit boundary; (3) the **§6 OWNER DECISIONS are ALL RESOLVED
> (D1–D12)** — no ratification pending. **Structure:** the master
> `docs/COBOLNET_REARCHITECTURE_PLAN.md` (resume protocol · north-star 5-assembly architecture · 11 principles · the
> dual-backend §3 mandate · the phase index + checkboxes · the owner-decisions table · §7 backfill refinements R1–R6) +
> `docs/rearchitecture/` (9 `DESIGN-*.md`, 6 `SURVEY`/`CRITIQUE-*.md`, 17 `PHASE-00..16-*.md`). **EXECUTION IN PROGRESS:
> PHASE 00 (migration safety net) ✅ DONE (2026-07-07, DEVLOG 667, commit `c65cfad`) — the characterization net (gates
> 2+3), the `DifferentialGolden` oracle bake-out (360 committed goldens; the battery no longer runs the legacy engine),
> the `tests/nist/corpus.tsv` fold, and the cached Roslyn ref-set. **PHASE 01 ✅ DONE (DEVLOG 668–669, `db6ae722`)**
> pulled the `CobolSharp.Compiler.{Common,Diagnostics,Generated,Parsing,Preprocessor}` → `CobolNet.Frontend.*` rename
> FORWARD from the G8 big-bang (G8 is now a pure deletion), deleted the 5 dead grammars + `.antlr` caches + the non-ISO
> JSON/XML grammar, and narrowed the SLL-bail catch — behavior-neutral, PROVEN by the byte-identical characterization
> gate. **PHASE 02 (`Cobol.Net.Editions` leaf + first-class diagnostic registry) IN PROGRESS — steps 1–6 of 11 DONE**
> (2026-07-07, DEVLOG 670–677, commits `62e09db1`→`4990fae7`, all pushed; recon `wf_9944fe61-fcc`): the edition machinery
> now lives in the new lowest-layer `Cobol.Net.Editions` assembly (referenced by both Frontend + Compiler), is
> **single-sourced from `tests/version-matrix/constructs.json`** (the registry / `Constructs.*` ids / `GateId` enum are
> generated by `scripts/gen-constructs.ps1` + drift-guarded — committed `.g.cs`, the reserved-words discipline, not
> MSBuild), `EditionContext` is a byte-stable adapter over an immutable `EditionInfo` + `IDiagnosticSink`,
> `ConstructRegistry.Check` is layer-neutral + sink-based, the ONE `EditionSeverityPolicy` is the sole strict/permissive
> decision, and every binder gate (incl. the 5 formerly-inline 08xx/0882 gates) routes through the registry funnel +
> enters the version matrix (0816 END-ACCEPT folded-but-`pending` — the grammar has no `END_ACCEPT`, gate unreachable).
> **⛔ RESUME AT step 7** — forward `{Gate(edition, GateId)}?` predicate stamping replacing the `EditionGateHints`
> reverse-engineering table (grammar-touching → FULL legacy guard per `.g4` fragment; ANTLR speculative-eval risk;
> `EditionGateDiagnosticTests` re-baselines because the parse-layer 0900 message FORMAT differs; `GateId` 55 members +
> the `GateIds.ConstructId` map already generated). **Read the five concrete gotchas + full landed-commit list in
> `docs/rearchitecture/PHASE-02-editions-assembly-diagnostic-registry.md`'s STATUS block FIRST.** Battery held green
> throughout: **2055 conformance · 224 unit · FULL legacy guard NIST 353 MATCH.** Steps 8–11: delete `EditionGateHints`
> (go/no-go = `EditionGateDiagnosticTests` parity) + re-home JSON/XML `COBOL0313`; preprocessor gates →
> `EditionSeverityPolicy`; the `DiagnosticDescriptors` registry + split the 44-site `COBOLNET0899` catch-all +
> `docs/DIAGNOSTICS.md`; docs sync + adversarial review + phase close.** The dual-backend goal
> (`project_dual_backend_goal`) is first-class (PHASE-16). Memory: `project_rearchitecture_plan`. **The §"NON-NEGOTIABLE
> PROCESS RULES" block below stays in force.** The STATE banners below are PRE-REARCHITECTURE HISTORY — the
> 2036/213-green baseline they describe is superseded by the 2055/224 above; the go-forward PLAN is the rearchitecture
> roadmap above, not their per-feature RESUME AT.
>
> **(pre-rearchitecture) STATE (DEVLOG 661–664, 2026-07-07 14:38 PDT): ⛔🎉 TYPEDEF / the TYPE clause (data-model D17, §13.18.58 /
> §13.18.57, COBOL-2002) is FEATURE-COMPLETE (all 4 increments) AND REVIEW-HARDENED — the adversarial find→verify
> review (wf_7d3b1492-01a, 13 agents) raised 8 candidates → 7 CONFIRMED, all FIXED (DEVLOG 664).** Increments 2–4 were
> all BINDER-ONLY (no grammar, no legacy guard). **inc 2 —
> STRONG typing (661):** `DataItem.StrongRoot`/`IsStrongGroup`/`SameStrongType` (equal strong-root `TypeName` +
> relative `CsName` path, §8.5.3); USE gates → **1533** (`CheckStrongMove` §14.9.25.3 SR2 · the same-type check in the
> ONE `CheckedRelational` chokepoint §8.8.4.2.3 SR1 · `CheckClassConditionOperand` §8.8.4.4.3 SR1); DECL gates →
> **1532** (SR6 in `ExpandType`; SR3/SR4 in `CheckStrongTypeDeclarations`). Golden `typedef_strong_ok` +
> `TypedefStrongTests` ×8. **inc 3 — level-88s in a TYPEDEF (662):** `DataItem.Own88s`; a template's 88s kept OFF the
> global by-name index (GR1), cloned per reference via `CloneConditionOnto`. Golden `typedef_88` +
> `TypedefConditionTests` ×2. **inc 4 — staged-loud residue (663):** 1534 EXTERNAL type · 1535 RENAMES-in-TYPEDEF +
> strong bool/object ordering compare (§8.8.4.2.3 SR4) · 1531 INDEXED-type ≥2× (a SINGLE ref WORKS — golden
> `typedef_indexed`). `TypedefResidueTests` ×5. Matrix: STRONG rides the SAME `typedefClause` gate (no new row);
> `ISO2023_CONFORMANCE_PLAN` M3-2 synced (TYPEDEF ◑ DONE; SAME AS / TYPE TO deferred). **Review fixes (664):** 2 HIGH
> — the SR6 strong-in-strong false-positive (hoist `StrongType` before the clone loop in `ExpandType`) + a cloned
> OCCURS DEPENDING binding data-name-1 globally (`OdoResolve` now subtree-first); 4 MED — `SameStrongType` now uses the
> NEAREST `DataItem.TypeAnchor` not the outermost root (nested-strong same-type), + 3 newly-enforced §13.18.57.3 SRs:
> **1536** SR7 (level-77→group), **1537** SR2 (subordinate/88 after a TYPE entry), **1538** SR5 (USAGE/SIGN
> superordinate). 1 candidate REFUTED (a CAPACITY-register edge, mis-attributed — reproduces without TYPEDEF). 15xx
> TYPEDEF band now 1529–1538; goldens `typedef_nested_strong`/`typedef_odo` + `TypedefReviewFixTests` ×5. **Battery:
> 2028 conformance · 213 unit GREEN; greenfield-only (every typedef golden is GreenfieldOnly).**
> ⛔🎯 **RESUME AT: pick the next ISO-scoped item.** ⚠ **JSON/XML (plan M3-3) is NON-ISO** — `specs/ISO_COBOL.md` has ZERO `JSON`/`XML`
> occurrences (they are IBM-vendor extensions, deferred post-ISO per the roadmap-council scrub) — so it is NOT part of
> "100% ISO." The next ISO-scoped items: **M3-4** (function/method pointers, conditional-expression enhancements,
> IEEE-754 alignment, increased limits); **M4-2b/M4-3** (the remaining 2023 intrinsics + the per-edition
> VERSION-GATING audit — the "N per-edition compilers" mission, `docs/VERSION_CHANGE_REFERENCE.md`); the flagged
> **M2-OO method-scope typedefs** (the D17 follow-up) + the M2-OO-1i residue; and **SAME AS** (a distinct deferred
> feature — `CloneItem` is built generically for its reuse). Reconfirm each against the spec before starting.
>
> **(superseded) STATE (DEVLOG 657–659, 2026-07-07 11:23 PDT): ⛔ TYPEDEF / the TYPE clause (data-model D17, §13.18.58 /
> §13.18.57, COBOL-2002) — increment 1 (the weak-TYPE spine) LANDED; OCCURS DYNAMIC review-hardened.**
> ⛔🎯 **RESUME AT: TYPEDEF increment 2 (STRONG typing) — all remaining incs (2–4) are BINDER-ONLY (no grammar, no
> legacy guard; greenfield battery suffices).** Implement directly from the D17 section of `docs/COBOLNET_DATA_MODEL_DESIGN.md`
> (the decision-complete design; the full recon plan is also saved at `scratchpad/typedef_plan.txt`). The 4-increment
> plan: **(1) ✅ DONE (659)** grammar (`STRONG` token + `typedefClause` — the ONLY legacy-guard slice; the TYPE-reference
> rule pre-existed) + the weak-TYPE spine: `DataItem` `IsTypedef`/`TypedefStrong`/`TypeRefName`/`TypeName`/`StrongType`;
> `BindEntries` routes a level-01 TYPEDEF to `DataBinder.TypeDecls` (off `Roots`/`ByName`; `RegisterTypeDecl`→**1529**);
> a post-build `ExpandTypes` (top of `BindResolve`, after ALL `BindEntries` → forward refs OK) clones each `TYPE IS name`
> via `CloneItem` (fresh `Uid`; CLONED `OccursSpec` [D17 risk#1]; elementary→copy PIC, group→clone children; subject
> VALUE/OCCURS kept GR3/SR14; unresolved/recursive→**1530**). Zero emitter change (a weak TYPE = macro-expansion). Fixes
> the silent-drop bug (TYPE was parsed+dropped). Goldens `typedef_weak_elem`/`typedef_weak_group` (2002 corpus,
> GreenfieldOnly). **(2) NEXT — STRONG typing:** `StrongRootOf(item)` (walk to the outermost `StrongType`) + `SameType(a,b)`
> (equal strong-root `TypeName` + relative `CsName` path, §8.5.3.3) → gate `BindMove`(StatementBinder.cs ~492)/
> `BindComparison`(~1470)/the class-condition arm → **COBOLNET1533** on a non-same-type operand; declaration SRs
> §13.18.57.3 SR3/SR4/SR6 (rename/redefine/placement of a strong item) → **1532**. Goldens `typedef_strong_ok`/
> `typedef_strong_bad`. **(3)** level-88 condition-names inside a TYPEDEF (clone + re-`BindCondition`; §13.18.58.4 GR1) →
> `typedef_88`. **(4)** staged-loud residue — EXTERNAL type decl→**1534**, RENAMES-in-TYPEDEF→**1535**, strong boolean/
> object non-equality compare→1535, type-with-INDEXED-BY referenced ≥2×→**1531** — + the matrix behavior/continuity rows +
> `DOC_INDEX`/`ISO2023_CONFORMANCE_PLAN` M3-2 sync; **then an adversarial find→verify review over TYPEDEF incs 1–4**
> (every prior feature's review found real defects — run it). SAME AS is DEFERRED (a distinct feature; a hard `AS`
> keyword is a legacy-compat hazard; `CloneItem` is built generically for its later reuse). Diagnostic band 15xx: 1529/
> 1530 used, 1531–1535 reserved. **Battery at head: 2003 conformance · 213 unit GREEN; FULL legacy guard NIST 353 MATCH
> (the shared `STRONG`/`typedefClause` grammar is byte-safe — the first run's 3 RL "regressions" were a JOBS=32
> parallel-build race, gone on re-run).**
>
> **STATE (DEVLOG 652–657, 2026-07-07 03:44 PDT — superseded by 659 above): ⛔🎉 OCCURS DYNAMIC (data-model D9, §13.18.38
> Format 4, COBOL-2014) COMPLETE + REVIEW-HARDENED across all 5 increments.** Adversarial review (wf_3f05d472-ad8, DEVLOG
> 657): **7 confirmed defects / 10 candidates, ALL FIXED** — #1 [HIGH] a whole-GROUP receiving MOVE into a group nested
> BELOW the dynamic level used `RefSending` (no growth → silent data loss) → a `DynTablePlace` arm in `EmitGroupMove`;
> #2 [MED] `CorrEligible` `Occurs is null`→`!IsTable` (CORRESPONDING mis-emitted member access on a `CobolDynTable<T>`
> field, CS1061); #5 [MED] SEARCH of a dynamic table nested under a fixed OCCURS silently scanned ZERO → now LOUD; #6
> [MED] OCCURS DYNAMIC in the FILE SECTION silently accepted → **COBOLNET1526** (§8.5.1.9.1); #7 [MED] the SET F14
> capacity peek `refs.Resolve` routed an OO property through the property hook → a PURE `CapacityRegisterFor` peek; #4
> [LOW] group-subordinate VALUE + TO → the 1528 guard extended (§13.18.63 GR16); #3 [LOW] the `GrowTo` docstring's false
> "EC-BOUND-OVERFLOW wired" claim corrected. Goldens `dyn_nested_group_move`/`dyn_corr`; guards
> `GroupSubordinateValueWithTo_Rejected1528`/`FileSectionDynamicTable_Rejected1526`.
>
> **STATE (DEVLOG 652–656, 2026-07-07 02:54 PDT — superseded by 657–659 above): ⛔🎉 OCCURS DYNAMIC (data-model D9, §13.18.38 Format 4, COBOL-2014)
> COMPLETE — dynamic-capacity tables across all 5 increments.** Built recon-first (wf_973560a9-bb6: 6 parallel readers →
> an xhigh synthesis, 761k tok) then implemented + verify-by-running each leg. (1) **652** declaration + the out-of-line
> growable `CobolNet.Runtime.CobolDynTable<T>` substrate + the `{is2014()}?` grammar (`CAPACITY` token, `OCCURS DYNAMIC
> occursDynamicPhrase*`) + the edition gate (COBOLNET0900 below 2014) + the `occurs-dynamic-2014` matrix/registry row
> (the ONLY grammar/legacy-guard slice — FULL legacy guard, NIST 353 MATCH). (2) **653** the CAPACITY register READ
> (`CapacityRegisterPlace` over `{tbl}.Capacity`, synthesized in `DataBinder.DynamicResolve`, an early resolver hook +
> `ReferenceResolver.TablePath`) + SET Format 14 (TO/UP BY/DOWN BY → `SetCapacity`/`CapacityUpBy`/`CapacityDownBy`,
> reroute in `BindSetTo`/`BindSetUpDown`); 1523 (register-as-receiver, the `ResolveReceiving` chokepoint + SR30
> collision) · 1524 (SET F14 mixed target). (3) **654** subscripted element access — the D9 `CobolTable.At(…,receiving)`
> sketch was WRONG (a `MemberPlace` path can't carry read/write polarity); corrected to `AccessDir` + a `DynTablePlace`
> whose `Read()`→`RefSending` / `Write()`→`RefReceiving` (grow-and-seed); arity via `IsTable`. (4) **655** SEARCH/SEARCH
> ALL bound over `.Capacity` (`EnterSearch`/`ExitSearch` try/finally → EC-FLOW-SEARCH GR31) + INITIALIZE (a spec-checked
> correction — §14.9.20 GR10 wants the CATEGORY DEFAULTS over 1‥Capacity, NOT the VALUE grow-seed → `InitializeDynLoop`
> + `InitializeDynCursor`, not `InitializeAll`). (5) **656** staged-loud guards: 1522 (SR28 FROM/TO) · 1525 (REDEFINES
> over a dynamic table, §13.18.44 SR5) · 1528 (VALUE on an elementary dynamic entry = VALUE-derived capacity, staged) ·
> 1527 (variable-length-group ops). **1526 SKIPPED** — ref-mod of a dynamic element empirically works (over-restriction
> avoided). **Battery: 1993 conformance · 213 unit GREEN** (greenfield-only; incs 2–5 no grammar → greenfield battery,
> not the full legacy guard). Diagnostic band 15xx→1528. Two flagged follow-ons, both LOUD today (never silently wrong):
> EC-BOUND-OVERFLOW (nonfatal/checking-gated) + full variable-length-group MOVE/COMPARE + FUNCTION LENGTH
> (`DynWholeTablePlace` = `Capacity × elemWidth`). ⛔ RESUME NEXT (Phase 6): **TYPEDEF** (D5-adjacent); then the M2-OO-1i
> method-own-FILE-SECTION residue / Phase 7 (2023 finalization).
>
> **STATE (DEVLOG 650, 2026-07-06 23:38 PDT — superseded by 652–656 above): ⛔🎉 PHASE 6 OPENED — floating-point USAGE (FLOAT-SHORT/LONG/EXTENDED
> + COMP-1/2) LIVE (numeric design D16).** The readiest Phase-6 feature. Recon wf_9de26ab6-3a8; §13.18.60.4 GR13
> verified verbatim (implementor-defined signed numerics; short⊆long⊆extended nesting → FLOAT-EXTENDED=double, no
> .NET quad). Native float/double field (never the scaled-integer substrate); a `Real` flag on the NumX carrier → any
> float-bearing expression evaluates in IEEE binary64; a float→fixed store lands via new `CobolFloat.ToScaled` then
> the existing store funnel; DISPLAY=`CobolFloat.Display`; compare=native IEEE double. Un-gated `ParseUsage`;
> picture-less `FloatItem` factory; PICTURE-with-float→**COBOLNET1521** (the 08xx band is exhausted). ALSO fixed a
> pre-existing COMP-1/2 stub bug (pic=null NRE + a (long) fraction truncation). 6 goldens (float_usage+comp1_comp2
> LEGACY-SHARED — the frozen oracle agrees byte-for-byte; float_move/neg/rounded/compare GreenfieldOnly). ⚠ DEFERRED
> (documented, LOUD not silent): the float LITERAL exponent form (1.5E3, §8.3.3.3.3) is not lexed → a loud parse
> error; the 6b IEEE family (FLOAT-BINARY/DECIMAL) + external-float PICTURE E stay loud. **FULLY COMPLETED (DEVLOG
> 651, owner-directed):** floating-point LITERALS (`1.5E3`, §8.3.3.3.3 — a `FLOATLIT` lexer token, SHARED-grammar so
> it passed the FULL legacy guard) + an adversarial review (wf_145d8cc9-0b6, **9 confirmed defects ALL FIXED**: float
> →numeric-edited CS1503, NEAREST-TOWARD-ZERO truncation, fractional level-88-on-float, `10/3`-into-float-receiver via
> a new `TargetReal` flag, PROHIBITED-inexact-float SIZE ERROR, transcendental-into-float full-precision, a NaN
> comment). **Battery: 1977 conformance · 213 unit · FULL LEGACY GUARD (NIST 353 MATCH · 0 regressions · legacy unit
> 1204 · integration 590) GREEN.** ⛔ RESUME NEXT (Phase 6): **OCCURS DYNAMIC**, then **TYPEDEF**; then the M2-PRE
> residue / Phase 7 (2023 finalization) sweep.
>
> **STATE (DEVLOG 649, 2026-07-06 22:29 PDT — superseded by 650 above): ⛔🎉 M2-OO-1i COMPLETE & REVIEW-HARDENED — the
> OBJECT/FACTORY ENVIRONMENT + FILE division (files referenceable from methods).** A find→verify review (wf_7355579f-e66) over the
> 5 inc-commits found 8 confirmed defects, ALL FIXED (DEVLOG 649): the predicted THIRD class-emit gap — REPORT SECTION
> in an object/factory → the complete Report Writer is now WIRED into the class path (golden `oo_object_report`; NOT
> gated — an owner correction) — plus an OBJECT SD → CS0103 (golden `oo_method_sort`, SORT-in-object works), a method
> EC-I-O `__IoCheckEc` gap, a `~CobolObject` finalizer DATA RACE (now enqueue/drain), the EXTERNAL keyed-register
> idempotency guard, a keyed `CloseAndDrop` leak, the COBOLNET1519 REPORT/SCREEN § citations (§13.8.3/§13.9.3), and
> GLOBAL-on-data-items → COBOLNET1520 (§13.18.27.3 SR4, was FD-only). Battery now **1973 conformance · 216 unit ·
> legacy integration 81 GREEN.** LESSON reinforced a THIRD time: a class-emit path silently omits per-unit scaffolding
> (the `using`, the external backing, the report engine) that ONLY an incremental compile-AND-RUN catches.
>
> **STATE (DEVLOG 648, 2026-07-06 21:49 PDT — superseded by 649 above): ⛔🎉 M2-OO-1i COMPLETE — the OBJECT/FACTORY
> ENVIRONMENT + FILE division (files referenceable from methods).** The recon (wf_5d22beb6-140) RE-FRAMED the ticket, verified against
> the spec: a method definition canNOT own an ENVIRONMENT DIVISION / FILE SECTION / WORKING-STORAGE (§12.4.3 SR1 /
> §13.4.3 SR1 / §13.5.3 SR1 — factory/instance only; a method owns only LOCAL-STORAGE + LINKAGE). So the real leg is
> the OBJECT/FACTORY paragraph's INPUT-OUTPUT + FILE division, referenceable from method bodies (§11.7.4 GR5). Landed
> in 5 increments (DEVLOG 644-648): (1) method ENV/FILE/REPORT/SCREEN → hard **COBOLNET1519**; (2) **`FileKeyExpr`** —
> the one canonical connector-key expression (byte-identical ~28-site sweep); (3) FACTORY files register in the class
> singleton (+ a class-file `using CobolNet.Runtime.IO` root-cause fix — `anyFiles` now counts class files); (4) OBJECT
> files are PER-OBJECT connectors (a minted `__fkey_X = MintInstanceKey("Class::INST::name")` + a §9.1.4
> `~CobolObject()` finalizer, suppress-by-default/re-arm-for-file-owners); (5) EXTERNAL object/factory FD shares the
> one run-unit connector + record area (`::EXT::` + `EmitExternalBackings` now emitted for classes). GLOBAL on a
> class/method FD → **COBOLNET1520** (§13.18.27.3 SR4). 4 goldens (`oo_factory_file` / `oo_object_file` /
> `oo_object_file_two_instances` / `oo_external_file_shared`, all GreenfieldOnly) + 3 OoSpineTests diagnostic facts.
> **Battery: 1969 conformance · 216 unit · 128 corpus goldens · legacy integration 79 GREEN; greenfield-only, CI green
> per commit.** Diagnostic-code map add: **1519 method-owns-section, 1520 GLOBAL-in-class/method.** ⛔ RESUME NEXT: the
> low-severity **M2-PRE preprocessor follow-ups**; then **Phase 6 (OCCURS DYNAMIC, TYPEDEF, floats) / Phase 7 (2023
> finalization)**. LESSON reinforced twice this leg: a class-emit path silently omits per-unit scaffolding
> (`using`/external backings) that only an incremental compile-AND-RUN catches — the recon design won't flag it.
>
> **STATE (DEVLOG 643, 2026-07-06 20:23 PDT — SUPERSEDED by the 648 banner above; kept for the M2-OO-1h detail):
> ⛔🎉 PHASE 5 INTRINSICS COMPLETE + the M2-OO-1h METHOD-SCOPE DATA
> MODEL LANDED & REVIEW-HARDENED.** Since the 635 snapshot below: (a) the Phase-5 intrinsic families were
> adversarially hardened (636, 6 fixes); (b) **M2-OO-1h — the method-scope data model — LANDED (DEVLOG 637–642):**
> REDEFINES / ODO / RENAMES / INDEXED-SEARCH now work inside method WORKING-STORAGE / LOCAL-STORAGE / LINKAGE, via a
> scope-aware name lookup (`OoRootOwner` + `LookupDataInScopeOf`, §11.7.4 GR5 method-first-then-global) and per-scope
> emission (WS→static fields, LOCAL-STORAGE→re-init-per-activation C# locals §14.5.3, LINKAGE→ref params); 8 goldens
> (`oo_method_{renames,odo,redefines_ws,redefines_local,redefines_linkage,indexed_search,indexed_two_methods}`); (c)
> a find→verify review of 637–640 (wf_8b9a8453-7f8, 3 reviewers) raised 7 / confirmed 6 → **4 method-scope REDEFINES
> defects FIXED (642)** [LINKAGE Tier-B USING/RETURNING copy-out named a suppressed root → CS0103; cross-section
> REDEFINES wrongly accepted → COBOLNET1518; Tier-A view dead local; LINKAGE backing seed not width-normalized] +
> **fix E (643):** `ImageInitOf` seeded a Tier-B-over-fixed-OCCURS-VALUE backing with ONE occurrence, not all
> (§13.18.63 GR9) — fixed at the single image-building site via `CobolString.Repeat`, regression
> `RedefinesTierBDifferentialTests.FixedOccursValue_SeedsEveryOccurrence`; the 7th finding refuted. **Battery: 1959
> conformance · 216 unit · 124 corpus goldens GREEN; greenfield-only, CI green per commit.** Diagnostic-code map
> add: **1518 cross-section method REDEFINES.** ⛔ RESUME NEXT (updated — the 635 "RESUME NEXT" M2-OO-1h data model
> is now DONE): **M2-OO-1i — a method's OWN ENVIRONMENT DIVISION + FILE SECTION (§14.5) + method-level EXTERNAL/
> GLOBAL** (recon-workflow → design-in-`PHASE4_RECONCILIATION.md` → implement → adversarial-review, the proven
> cadence) **+ the low-severity M2-PRE preprocessor follow-ups; then Phase 6 (OCCURS DYNAMIC, TYPEDEF, floats) /
> Phase 7 (2023 finalization).**
>
> **STATE (DEVLOG 635, 2026-07-06 20:05 PDT — SUPERSEDED by the 643 banner above; kept for the Phase-5 detail):
> ⛔🎉 PHASE 5 INTRINSICS COMPLETE — all six remaining §15 families
> LANDED IN FULL this session (DEVLOG 630–635), each done completely per the owner's *do every feature well*
> directive.** The proven per-intrinsic cadence: a background RECON WORKFLOW (wf_840f8070-fdf) produced
> decision-complete designs for the four research-heavy families (date/CONVERT/algebraic/MODULE-NAME) while
> FIND-STRING + SUBSTITUTE were implemented inline; then catalog Deferred→Runtime + bespoke bind (keyword functions)
> or generic bind + a renderer case + a runtime body → golden (GreenfieldOnly) + differential tests + a full
> battery per commit. Landed:
> • **FIND-STRING (§15.37, 630)** — LAST / START AFTER argument-3 / ANYCASE keyword phrase; non-overlapping match
>   counting; `CobolIntrinsics.FindString`.
> • **SUBSTITUTE (§15.87, 631)** — per-pair `[ANYCASE][FIRST|LAST]` variadic replacement in one left-to-right pass;
>   EC-ARGUMENT-FUNCTION on zero length.
> • **CONVERT (§15.19, 632)** — source/destination format keywords (ANY/ANUM/HEX/NAT/BYTE); Latin-1 ↔ UTF-16BE ↔ hex
>   ↔ byte; the NONFATAL **EC-DATA-CONVERSION** ambient gate BUILT end-to-end (`DataConversionChecking`, verified via
>   EXCEPTION-STATUS under `>>TURN … ON`); COBOLNET1514 SR band.
> • **MODULE-NAME (§15.65, 633)** — a real runtime **module call-name stack** (`CobolModule`, thread-static, pushed/
>   popped in `ProgramRegistry.RunMain`/`CallProgram`); CURRENT/ACTIVATING/NESTED/STACK/TOP-LEVEL; NESTED gate 1515.
> • **SMALLEST / HIGHEST / LOWEST-ALGEBRAIC (§15.83/§15.43/§15.58, 634)** — the PICTURE-metadata compile-time fold
>   family (all-nines / two's-complement container / edited-mask via the now-public `CobolEdit.MaskCapacity`);
>   COBOLNET1516.
> • **The COBOL-2014 date/time + number family (§15.17/38-41/48/69/79/92/95, 635)** — a full §15.3 format engine
>   (one `Tokenize` → `EmitFormatted` + `Analyze`; ISO-week / UTC / offset / fractional seconds; §15.92.4 per-digit
>   error positions) + NUMVAL-F/TEST-NUMVAL-F; non-literal-format gate 1517. Golden `formatted_datetime` (16 lines,
>   every one spec-verified).
> **THEN adversarially hardened (DEVLOG 636):** a find→verify review workflow (wf_18f20f2b-d5e, one reviewer per
> family) raised 7 findings, **6 confirmed + FIXED** — FIND-STRING overlapping occurrences (§15.37.4 r1, was
> non-overlapping); SUBSTITUTE ANYCASE → the LOWER-CASE fold (§15.87.4 r5, was OrdinalIgnoreCase); CONVERT ANY over
> a national item → its UTF-16BE storage bits (§15.19.3 SR7, was Latin-1+substitution); CONVERT malformed HEX →
> fatal EC-ARGUMENT-FUNCTION not nonfatal EC-DATA-CONVERSION (SR4); MODULE-NAME STACK collapses same-unit frames
> (§15.65.4 r9, was a MAIN;MAIN duplicate for nested); NUMVAL-F requires the sign after E (§15.69.3). +7 regression
> tests; 1 finding refuted.
> **Battery: 1948 conformance (+67 this session) · 216 unit · 117 corpus goldens GREEN; greenfield-only, CI green
> per commit (the legacy job passes — every new golden is GreenfieldOnly).** Diagnostic-code map: 1514 CONVERT,
> 1515 MODULE-NAME NESTED, 1516 ALGEBRAIC, 1517 non-literal-format. Still-Deferred §15 rows (loud, never wrong):
> BYTE-LENGTH, DISPLAY-OF/NATIONAL-OF (residue #11 national data-class), the LOCALE-* / TEST-DATE / BOOLEAN-OF-*
> family, CHAR-NATIONAL, SECONDS-PAST-MIDNIGHT, TEST-NUMVAL(-C), the DATE/DAY/YEAR-TO-* 4-digit-year family.
> **RESUME NEXT: the M2-OO-1h data-model residue** (intricate — unstaging REDEFINES/ODO/RENAMES/INDEXED in method
> data means extending the data-model classification/index/ODO machinery into method scope; method own ENV/FILE/
> SCREEN; PROPAGATE ON + object VIEWS + OO-RAISING are 2014/2023 — must be done in full, not dismissed) **+ the
> low-severity M2-PRE preprocessor follow-ups; then Phase 6 (OCCURS DYNAMIC, TYPEDEF, floats) / 7 (2023
> finalization).** All Phase-4 lettered tracks (a)-(g) + the UDF subsystem + all Phase-5 intrinsics are DONE.
>
> **(superseded) STATE (DEVLOG 629, 2026-07-06 15:38 PDT): PHASE 5 IN PROGRESS — 3 intrinsics LANDED: CONCAT (§15.18) +
> BASECONVERT (§15.12) [628] + TRIM (§15.96) IN FULL [629].** Goldens `intrinsics_string_2023` + `intrinsics_trim`,
> GreenfieldOnly. 1881 conformance · 216 unit. **ALSO a CI SPEEDUP ~17min→~7min (DEVLOG 627):** the monolithic guard
> job → 4 parallel jobs + guard-fast.sh + NuGet caching; and the ~29-min INV-1 sweep pole → **~2 min** via a no-emit
> `CompilerDriver.CheckOnly` + a `cobol check-batch` subcommand (parse+bind-check the whole manifest in one warm
> parallel process). (The Phase-5-in-progress RESUME AT is now closed — see the DEVLOG 635 banner above.)
>
> **(superseded) STATE (DEVLOG 626, 2026-07-06 12:26 PDT): ⛔🎉 PHASE 4 TRACK (c) — THE UDF SUBSYSTEM IS COMPLETE.** M2-UDF-3
> (separate-compilation function PROTOTYPES: `FUNCTION-ID … IS PROTOTYPE`, cross-assembly locate via the sibling probe
> → **EC-FUNCTION-NOT-FOUND** on absence, §12.3.8 GR11 in-group-def→prototype→external resolution — DEVLOG 624) +
> M2-UDF-4 (REPOSITORY `FUNCTION ALL INTRINSIC` / named-intrinsic binding + the §8.4.3.2 SR2 **keyword-omitted**
> reference form, bind-side D2 at the RefExpr/FieldOperand chokepoints, data-item-wins-safe, gated ≥2002 — DEVLOG
> 626) both LANDED. Track (c) = invocation(615)+EXIT FUNCTION(616)+prototypes/cross-asm(624)+specifiers/omission(626),
> DONE. Goldens `udf_prototype` (P=000049, in-group + a cross-assembly Fix-G test) + `udf_keyword_omitted`
> (MAX=0034/MIN=0012/MOD=0004), both GreenfieldOnly. Battery: **201+ unit · 1868 conformance · FULL legacy guard
> 353 MATCH + 11 DIVERGENT** — 0 regressions; the PROTOTYPE grammar token + the RefExpr/FieldOperand re-route are
> both 85-byte-invariant. ⛔ **ALSO: a ROOT-CAUSE CLI fix (DEVLOG 625) — the hand-rolled `cobol` arg parser (which
> let a value option swallow a following flag: `--nist --run` ate `--run`, so the program never ran) was replaced
> with `System.CommandLine`** (owner-chosen; the owner restated the standing rule *never work around — fix the root
> cause; apply to all existing workarounds*, memory `feedback_no_workarounds_root_cause`). 11 CliParserTests lock it.
> **RESUME NEXT: the remaining Phase-4 M2 tracks — the M2-OO-1h residue (0899-staged: method own ENV/FILE/SCREEN,
> REDEFINES/ODO/RENAMES/INDEXED in method data, PROPAGATE ON, FACTORY-OF/ACTIVE-CLASS RAISING, object VIEWS,
> STOP…RAISING — Phase-3 OO-port residue) and the M2-PRE preprocessor robustness follow-ups; then Phase 5
> intrinsics / 6 (OCCURS DYNAMIC, TYPEDEF, floats) / 7 (2023). All (a)-(g) LETTERED Phase-4 tracks are now LANDED.
> The proven cadence: recon→design into PHASE4_RECONCILIATION→implement in small green commits→adversarial
> find/verify→full battery + FULL legacy guard→GreenfieldOnly for shared-corpus goldens→commit+push+CI.**
>
> **(superseded) STATE (DEVLOG 623, 2026-07-06 01:40 PDT): ⛔🎉 PHASE 4 TRACK (d) — FILE SHARING / LOCK MODE / RETRY / UNLOCK
> (M2-FILE-1) LANDED.** The COBOL-2002 file-sharing / record-locking subsystem is live end-to-end: the SHARING clause
> + OPEN SHARING phrase (§12.4.5.15/§14.9.27), LOCK MODE (§12.4.5.9), RETRY (§14.7.9), the WITH/NO/IGNORING LOCK
> record-lock phrases (§14.9.30/.51/.35), UNLOCK (§14.9.47), the 51/52/53/54/61/62 statuses, the COBOLNET1512 SR band.
> **Synthesis decision D1 — built REAL** (not stubbed): a physical-file registry (`src/Cobol.Net.Runtime/IO/CobolFile.Locks.cs`)
> keyed by resolved host path makes two-SELECTs-one-file 61/51 conflicts deterministic in ONE run unit. Golden
> `file_sharing` byte-exact (`OPEN-A=00/OPEN-B=00/READA=00/READB=51/RETRYB=51/IGN=ALPHA/AFTER=00/EXCL=61`). AS-BUILT
> deviations (sharing-active-only-on-clause default → legacy corpus byte-invariant; sequential record-lock effect =
> residue) logged in `docs/PHASE4_RECONCILIATION.md` §M2-FILE-1. Battery: 201 unit + 1844 conformance + 557 legacy
> integration GREEN; full guard running. **RESUME NEXT: the remaining M2 tracks (M2-OO sub-features a–h; M2-ARITH /
> M2-PRE / M2-ILA residue). Recent track chain: (a) national/boolean [DEVLOG 619–622], (b) data pointers [617],
> (c) UDFs [615/616], (d) file sharing [623]. The proven cadence: recon→design into PHASE4_RECONCILIATION→implement
> in small green commits→adversarial find/verify wave→full battery + FULL legacy guard→GreenfieldOnly for shared-corpus
> goldens the frozen legacy can't bind→commit+push+CI.**
>
> **(superseded) STATE (DEVLOG 595, 2026-07-03 21:10): ⛔🎉 ROADMAP PHASE 2 W2 + W1.5 COMPLETE (commits a3e29b6 + 1f2156b) —
> the four-track W2 wave (A: the MOVE rows 1/92/128 + both latent bugs on the StoreAsImage substrate ·
> B: the loud-guard misbind sweep, ParseUsage/Analyze now (EditionContext,where), COBOLNET0899/0808, the
> allocate/free/invoke false-greens exposed→pending · C: the 18-case negative corpus, all CLI-verified ·
> D: position-aware reserved words, `IsProvableUserWordPosition`, the RW104A hazard closed — only 7 of 34
> band tokens even had §8.9 rows) + the ADVERSARIAL REVIEW (6 confirmed / 0 refuted, ALL fixed: SR1
> class-index 0809, the ref-mod round-trip loss `MarkRefModStoreImage`, the QUOTE dual row
> move-quote-numeric-obsolete-2014 [E.2 item 21], the ';' single-strip + `PIC ;` leak, '$'-under-custom-
> currency, .err specificity ×18) + W1.5 (all 17 reachable intro-gate sites → COBOLNET0900 edition-naming
> via the new frontend `EditionGateHints` parse-layer mapping; JSON/XML → vendor COBOL0313; the 0860/0861
> double allocation resolved by registry migration; 5 new rows; expectDiagnostic on 21 rows). Battery:
> conformance 1353/1353 · unit 102/102 · sweep 419 OK/0 BREAKS · INV-1-STRONG 349/349 byte-exact at
> 2023-permissive · FULL legacy guard-fast ALL GREEN. VCR: rows 1/5/6/7/32/89/90/92/126/127/128 GATED,
> 28 PARTIAL (QUOTE leg), Table 7 grown 7.4–7.14 (7.14 = the trailing-`,` KNOWN MISBIND — the `;` twin —
> queued to the W3 lexer cure; 7.13 = the multi-char-ALL 2002-edge research row).
> **W3 PART 1 DONE (DEVLOG 596, commit follows 035d42f):** ① `Generated/` fully UNTRACKED — the DEVLOG-554
> rule is RETIRED (fresh checkouts always regenerate; the tracked files' checkout mtimes were suppressing CI
> regen — the d6c8143 incident's root); ② the PIC_STRING separator cure LANDED (VCR 7.14 FIXED — trims a
> trailing `,`/`;` only when LA(1) is whitespace, the §8.3.5 r2 shape; the LA-guard is load-bearing: NC125A's
> legal SR7 `…9,.` mask broke the naive version); ③ XOR/EXCLUSIVE-OR REGATED to 2023 (VCR 41 GATED:
> `{is2023()}?` operator + cobolWord/_dataNameTokens/CheckedTokenTypes admission + 3 registry rows + the
> corpus re-edition + the M4-2a doc correction + the legacy XOR test retargeted to Cobol2023). Battery:
> conformance 1367/1367 · sweep 419/0 · INV-1-STRONG 349/349 · FULL legacy guard green (353 MATCH,
> 1204 unit, 537 integration).
> **W3 ⑥ DONE (DEVLOG 597): the 2002-corpus audit** — 6 programs re-editioned (inspect_backward→2023 per
> Annex E.3 item 34; the 5 OPTIONS/ROUNDED-MODE programs→2014), **11 programs ENABLED with a live RUN
> CONTRACT** (CorpusRunnerTests now compiles strict + runs + byte-compares vs .out on the CutRunner.Normalize
> basis; the 2002+ positive corpus is no longer empty), a Pic-null doomed-emit CRASH fixed (picture-less
> skeleton usages get PicInfo.RecoveryItem, the IndexItem pattern), 2 .outs re-baselined to ISO (§14.9.11.4
> GR6 full-width DISPLAY; the legacy ConformanceTests gained the guard-style LEGACY_DIVERGENT skip with
> citations). CI PROVEN on the untracked-Generated path (both jobs green on 01cb96d).
> **W3 ⑤ DONE (DEVLOG 598): preprocessor DialectLevel threading — VCR rows 2/4/94 GATED** (the frontend's
> first edition-aware gates: `ReferenceFormatProcessor.EditionGates` word-continuation 0902 + col-7 0903;
> `CopyProcessor.OnNonPseudoTextOperand` 0902; `Frontend.Permissive` threaded; frontend-bag warnings now ride
> `Result.Warnings` on every outcome — they were silently dropped). 3 drift-locked rows + 2 negative
> witnesses; battery green at conformance 1398/1398.
> **W3 ④ DONE (DEVLOG 599, 2026-07-04) — ⛔🎉 ROADMAP PHASE 2 IS CLOSED.** The notInGrammar 85-acceptance
> set is GATED (VCR Table 7 rows 7.15–7.18): RERUN / ENTER / USE FOR DEBUGGING / section segment-numbers
> parse UNGATED at every edition, bind accepted-inert at 85 per the X3.23-1985 rules, and 0902 ≥2002 via
> the registry. 7 new lexer tokens (RERUN/ENTER/EVERY/CLOCK-UNITS/DEBUGGING/REFERENCES/PROCEDURES) admitted
> at all three user-word sites; `enterOperand` is deliberately NOT cobolWord (`ENTER COBOL.` — COBOL is
> '85-reserved, a funnel false-reject caught by probe); the '85 debug DUAL posture is implemented
> (switch-absent ⇒ comment-treated section, binder AND validator — DB103M with its 95 DEBUG-register
> references now COMPILES at 85; switch-present ⇒ compiled-never-triggered, DB301M/302M/305M; DEBUG-*
> under the switch ⇒ 0899 not-implemented, never the false 0901 — DB101A). 4 registry+json rows, 4
> negative cases, Ansi85AcceptanceTests ×23 (incl. per-word §8.9 freeing editions: RERUN/ENTER 2002,
> DEBUGGING 2014, EVERY/CLOCK-UNITS/REFERENCES/PROCEDURES 2023). Battery: conformance 1453/1453 (+55) ·
> unit 102/102 · INV-1-STRONG 349/349 · sweep 439 OK / 20 SKIP85 / 0 BREAKS (grew from 419 OK — the
> DB/SG/OBIC SKIP85→OK migration, predicted) · FULL legacy guard ALL GREEN (353 MATCH · 1204 · 537).
> **PHASE 3 OPENED (DEVLOG 600): spine part 1 LANDED** — `CobolObject` runtime base (D2: `__CobolInvoke`
> default → EC-OO-METHOD; `RequireNonNull` → EC-OO-NULL, both via the landed EC engine) + USAGE OBJECT
> REFERENCE LIVE in its universal form (PicCategory/Usage out of the skeleton band; `PicInfo.
> ObjectReferenceItem(className?)`; new 0812 PICTURE-conflict gate; GR1 inheritance in ResolveIndexItems;
> typed references 0899-STAGED pending the class symbol table; skeleton tests flipped to the live
> contract). Battery: conformance 1454/1454 · unit 101/101 · no NIST/grammar exposure.
> **SPINE PART 2 LANDED (DEVLOG 601, 2026-07-04) — ⛔🎉 THE OO SPINE IS CLOSED; legacy slice 1 (+ the
> 455-slice method scoping) SUBSUMED.** A CLASS-ID compiles to a real `public class FOO : CobolObject`:
> the pass-1 class symbol table (D1 — new `OoClassTable`, built in CallCollectUnits BEFORE any bind;
> 0820/0821/0822 structural band), typed object references un-staged (unknown class → new 0813), the
> emit-into-a-type parameterization (new `CSharpEmitter.Oo.cs` — OBJECT WS → instance fields, VALUE →
> field initializers = the D4 predefined-NEW ctor; one `public virtual` method per METHOD-ID over its
> EXIT-BOUNDED pc range in the class's ONE dispatch space; the shared `EmitDispatchMethod` extracted from
> EmitDispatcher and reused untouched), per-method paragraph scopes with NO program-wide fallback
> (traps #4/#10 structural; SECTIONs-in-methods implemented, superseding legacy COBOL0116), D8 realized
> CATCH-AT-ENTRY (`BoundMethodReturn` → new runtime `MethodReturn`, the ProgramReturn pattern — a plain
> `return` can't unwind nested PERFORM __Dispatch frames; STOP RUN in a method still kills the run unit;
> EXIT METHOD = method return, 0827 misplacement, 0902-at-2023 via the existing window row), and INVOKE
> binding (`OoBindInvoke`: NEW → `new FOO()` w/ RETURNING+§14.8 conformance 0826, no-arg instance →
> `RequireNonNull(recv).M()` w/ compile-time 0825 unknown-method; 0823/0824 target band; SELF/SUPER/
> factory/USING-RETURNING/universal stage LOUD). **4 of the 9 oo_* goldens ENABLED under the 2002
> manifest run contract** (oo_hello, oo_instance_data = trap-#1 two-object independence,
> oo_method_perform, oo_object_group — all byte-exact); the 3 pending OO matrix rows flipped ACTIVE
> (+12 cells); new `OoSpineTests` ×12 day-one adversarial facts. Battery: conformance **1472/1472** ·
> unit 101/101 · **INV-1-STRONG 349/349 byte-exact** · sweep 438 OK + 1 solo-clean transient (ST137A,
> the DEVLOG-590 watched flake) / 20 SKIP85 · drift green · zero grammar/legacy exposure.
> **OO PORT SLICE 2 LANDED (DEVLOG 602, 2026-07-04)** — method LINKAGE → typed `ref` C# params over
> CAPTURABLE locals, LOCAL-STORAGE → C# locals (re-init per activation), method WS → STATIC fields (D3)
> with the §13.5.3 SR1 window (`method-working-storage-window` — 0900/0902; permissive keeps the static
> semantics; boundary PINNED provisional, VCR Table 6 row 130e), per-method DATA scopes (§11.7 GR5
> shadowing; trap #6 structural — `OoMethodDataScope` + the resolver/88 overlays), the LOCAL-FUNCTION
> dispatcher realization (paragraphs emit inside `__MDispatch` capturing the method locals — reentrant,
> :12032 implicit-RECURSIVE proven by a 3-deep obj-ref-formal recursion test; `EmitDispatchMethod`
> slices + `_dispatchName` threading), and INVOKE USING/RETURNING marshaling (D6): §14.8.2 STRICT
> conformance at bind = TYPE-PRESERVING crossings (no cross-class profile references), direct-`ref`
> fast path (GR7a once-evaluation free), copy-in/out temps elsewhere, groups as character images,
> SR 10 object-data auto-CONTENT (GR6a2) + explicit-BY-REFERENCE-of-object-data 0828, literal
> fit-checking, RETURNING = the C# return value delivered via receiver-side bridges (GR8). New 0828
> conformance band; matrix: `exit-method-window` fliped ACTIVE + the new `method-working-storage-window`
> row via the NEW `expectDiagnosticBelow` dual-window mechanism (the documented reactivation contract —
> closed). **5 of 9 oo_* goldens ENABLED** (+ oo_method_args, byte-exact); OoSpineTests ×18 (trap #3
> arity 0828, trap #6 cross-wiring, recursion/reentrancy, SR 10 both ways, strict-conformance 0828s,
> method-WS static semantics + window). Legacy-never-landed multi-method LINKAGE: DONE net-new.
> **SLICES 3a+3b LANDED (DEVLOG 603, 2026-07-04)** — INHERITS (`: BASE`, cycle 0820, override marking
> + §9.3.8.2 signature 0829 via the ONE shared DescriptionMismatch rule; trap #2 dead by the uppercase
> CsName convention) + SELF/SUPER (D5: `this.M(…)` virtual GR2 / `base.M(…)` non-virtual GR3;
> 0827 placement band incl. SUPER-in-root trap #7; full slice-2 marshaling shared via
> OoBindResolvedInvoke). Subclass-own OBJECT data native (base data name-invisible per encapsulation).
> **ALL 9 oo_* GOLDENS BYTE-EXACT AND ENABLED.** The 3a/3b wave was reviewed by the multi-lens
> adversarial workflow (find→verify) + a 12-probe edge battery on the prebuilt CLI; next-slice briefs
> (FACTORY / OVERRIDE-FINAL attrs / universal dispatch / EC-OO / INTERFACE+PROPERTY) regenerated there.
> **FACTORY LANDED (DEVLOG 604, per its brief's D11 — statics SUPERSEDED):** factoryParagraph grammar
> (+ the FACTORY token via the XOR-recipe: lexer + _dataNameTokens + cobolWord + funnel — user word at
> 85, 0901 ≥2002, proven both ways), per-class factory SINGLETON classes (`FOO__FACTORY :
> BASE__FACTORY | CobolObject`, `__Instance` + covariant `__New`), separate factory roster (dual
> dispatch with instance names §9.3.6; 0836 factory-NEW-name; factory overrides + 0829), `INVOKE
> Class "M"` → `__Instance.M(…)`, SELF/SUPER roster selection by OoInFactory (SR4f–i) + SELF|SUPER
> "NEW" → `this.__New()` active-class creation (§16.2.1), factory data/method-WS on the factory class
> (SR-10 auto-CONTENT + the §13.5.3 window ride free), `OBJECT REFERENCE FACTORY OF` staged 0899.
> oo_factory (10/10 oo corpus) proves §8.6.4 per-class copies of INHERITED factory data (trap #11 —
> FC=02/FC=01) and inherited-MAKE-creates-the-runtime-class (WOOF). FIRST .g4 change of the OO drive —
> FULL legacy guard in the battery.
> **OVERRIDE/FINAL LANDED (DEVLOG 605):** the ISO method attributes parse (spec order; OVERRIDE token
> via the XOR-recipe — user word at 85, 0901 ≥2002); STRICT §11.7: SR4a redefinition-without-OVERRIDE =
> 0837 via the ONE EditionContext.Removed seam (error strict / warning + pre-wave inference permissive),
> SR3 = 0838, the FINAL family (override-of-FINAL GR3; INHERITS-a-FINAL-class §11.3 GR3) = 0839; the
> TOTAL D7 modifier table emits sealed/sealed-override/non-virtual (the CS0549 sealed-factory `__New`
> trap caught on the golden's first compile). Corpus: the three redefining oo_* sources gained OVERRIDE
> (greenfield-only now — the frozen legacy can't parse it); new oo_override_final golden (12/12 oo).
> **INTERFACE-ID + IMPLEMENTS + PROPERTY declarations LANDED (DEVLOG 606):** C# interface emission
> (prototypes via the ONE OoSignatureOf; §10.6.2 SR4 LINKAGE binding; 0840 band), the
> BINDER-authoritative §9.3.11/§9.3.8.2.3 conformance pass over the §11.8.4 GR2 closure = 0841
> (Roslyn insufficient BOTH directions — 9(4)/9(8) `ref long` under-reject; covariant returns
> over-reject, cured by explicit-implementation AdapterPairs), interface-typed receivers
> (SR4e prototype dispatch + the widening interface branch on every path), PROPERTY declarations
> (clause-synthesized accessors under the pinned §11.7.4 GR1a `__GET_/__SET_` names + explicit
> GET/SET PROPERTY methods; 0842 = SR5/SR6/SR7/§13.18.42.3-SR4), the VALUE-loop PROPERTY guard
> (both consumption points; TokenStream.LA in C# predicates), 6 registry + 4 matrix rows + W1.5
> hints, 4 goldens (16 oo_*), +11 OoSpine tests. Words: GET/PROPERTY/INTERFACE reserved 2002+;
> IMPLEMENTS §8.10 context-sensitive (user word at ALL editions).
> **Property REFERENCES LANDED (DEVLOG 607):** the §8.4.3.9.4 GR1–GR3 desugar is LIVE —
> ReferenceResolver's fallback binds `P OF {obj|Class}` to a synthesized temp (Roots-declared,
> refmod rides the normal tail), `BoundStores.StoreKindOf` (NEW — a TOTAL 3-state store-polarity walk
> over all 119 bound nodes, emitter-verified by a 15-agent survey; unknown → 0843 loud) classifies
> GR1/GR2/GR3, and `StatementBinder.OoWrapPropertyOps` wraps at the ONE BindStatement chokepoint
> (mark/drain-own-suffix) into the NEW `BoundSequence` node. 0843 band = SR1 specifier / SR2
> universal / SR3 no-GET-when-sending / SR4 no-SET-when-receiving (polarity-aware — WITH NO SET stays
> readable). Factory form (`P OF Class`) live. 3 goldens (19 oo_*): oo_property_ref (all three GR
> forms — the FIRST runtime exercise of synthesized accessors), oo_property_explicit_ref
> (invocation-count proof: GR2 never gets, GR1 never sets), oo_property_factory_ref. +4 spine tests.
> **THE UNIVERSAL WAVE LANDED (DEVLOG 608, D10 complete):** universal refs emit `CobolObject?`;
> per-class `__CobolInvoke(string, CobolInvokeArg[], CobolInvokeArg?)` switches (declared-non-override
> roster, base-chain = §9.3.6, both halves); §14.9.23.4 GR7c conformance AT RUNTIME via
> ConformanceDescriptor (ONE rule beside DescriptionMismatch) → EC-OO-UNIVERSAL on
> arity/descriptor/RETURNING-presence, unconditionally; canonical-by-descriptor box forms (D-U6a —
> N:Display boxes the IMAGE string, bridged by FormatDisplay/StoreDisplay per side); identifier-2
> (SR7/SR8) live; 0866 band. SET Format 5 live (dataReference+; the BindSetTo SEMANTIC re-route — either
> side object ⇒ F5; NULL/SELF/SR13-factory senders; universal-into-typed FORBIDDEN → 0867). Object
> relations live (Format 3 =/<>; ReferenceEquals identity; 0868; IS-class STRUCK non-ISO). 4 goldens
> (23 oo_*), +11 spine tests, set-object-reference-2002 row+hint.
> **⛔🎉 EC-OO LANDED (DEVLOG 609) — ALL FIVE OO BRIEFS COMPLETE (604–609).** The exception-OBJECT
> channel: RAISE identifier (GR2 never-fatal, 0848 band), ExceptionState.ExceptionObject (CobolObject?)
> + the "EXCEPTION-OBJECT" EXCEPTION-STATUS sentinel (zero function changes), USE F4 __EcObjDispatch
> (GR14a = C# `is`; GR3 replaces F1/F3 tiers), GOBACK/EXIT/method RAISING identifier
> (BoundRaising.ObjectSource; SR4a/SR4d = 0849 compile-time — the activated-side rule-1 check statically
> discharged, D-EO5), the ONE pickup with the object branch at every CALL + INVOKE + universal site
> (rule-4 EC-OO-EXCEPTION fatal conversion into the F3 tiers), SET … TO EXCEPTION-OBJECT (typed targets
> runtime-narrow → EC-OO-UNIVERSAL), header partitions (0858), F4 unknown-class 0859, the 0901
> register-context exemption. 2 goldens (25 oo_*), +9 spine tests. Residue 0899-named: PROPAGATE ON,
> interface/FACTORY-OF/ACTIVE-CLASS RAISING legs, method declaratives, object VIEWS.
> **PHASE 4 OPEN (DEVLOG 610): the reconciliation audit LANDED** — `docs/PHASE4_RECONCILIATION.md` is the
> greenfield-truth view (catalog ☑ marks are legacy-only mirages: all M2-DATA + UDF-invocation rows stage
> LOUD; M2-PROC-4 EC + the OO umbrella are actually done). Per-track sizing there.
> **(e) arithmetic DONE (DEVLOG 611)** — the PROHIBITED-inexact edited-receiver cure (CobolNum.RescaleChecked;
> the leak was the edited-store path's plain Rescale) + ARITHMETIC IS STANDARD positive behavior (routes to
> the CobolDec engine for fixed-point, §8.8.1.2/§8.8.1.4; removed at 2023); both 2014 goldens enabled.
> **(b) pointers increment 1 DONE (DEVLOG 613)** — USAGE POINTER data + SET TO NULL/pointer + [NOT] EQUAL
> on the ManagedPointer carrier (PicCategory.Pointer, BoundSetPointer, ManagedPointer.SameTarget, 0869
> band); pointer_data.cob enabled; increment 2 = ADDRESS OF (byte-backing) / BASED / SET ADDRESS OF /
> ALLOCATE-FREE (based_pointer/pointer_alloc/pointer_arith PENDING).
> **(M2-DATA-1) BINARY-CHAR family DONE (DEVLOG 614)** — USAGE BINARY-CHAR/-SHORT/-LONG/-DOUBLE
> [SIGNED|UNSIGNED] are now PICTURE-less native 1/2/4/8-byte two's-complement integers (SIGNED default /
> UNSIGNED widens) on the COMP-5 BinaryCapacity discipline: PicInfo.BinaryItem + the un-skeletoned Usage
> members; CobolNum.WrapBinary/InBinaryRange IMPLEMENT the byte-width wrap + SIZE-ERROR range check (the
> BinaryCapacity path was a documented stub — also cures COMP-5's stubbed overflow); binary_usage.cob
> byte-exact; PICTURE prohibited COBOLNET0870 (§13.16.3 SR8); implied DISPLAY width 3/5/10/19·20. Battery:
> Unit 123 · Conformance 1610 (+18) · 0 regressions. Floats (M2-DATA-2) stay Phase 6.
> **(c) M2-UDF-1+2 inline UDF invocation DONE (DEVLOG 615)** — `FUNCTION user-name(args)` live for the
> in-group whole-source form: hoisted CALL…RETURNING over a §8.4.3.2.4 GR1 result temp (BoundSequence
> pre-op; §8.4.3.2.3 SR1 = never receiving), args per §8.4.3.2.4 GR5 (identifier→Reference, literal/arith→
> content cell conformed by CobolArgAdapt), §12.3.8.2 GR12 user-shadows-intrinsic dispatch, FUNCTION-ID
> units structurally RECURSIVE (§9.4). ⚠ The design's "bind is already two-phase" claim was FALSE — the
> real work was splitting `CallBindUnit` → `CallBindUnitData`/`CallBindUnitProcedure` with the
> `UserFunctionSignature` table between (INV-1-STRONG + full battery prove it behavior-neutral). New
> 1505–1509 band (+1501 GR12 hint), 1509 = the PERFORM-UNTIL/SEARCH re-evaluation loud-guard the design
> missed. THE ADVERSARIAL REVIEW WAVE landed same change set (wf_e38982d1-0d2: 28 raw → 24 confirmed, all
> fixed/staged/documented — see the reconciliation's review subsection): the StoreAsImage (temp,model)
> re-sync pass, EcWrap + ContainsNextSentence BoundSequence transparency (both holes predated UDFs via
> property ops), the §8.8.4.13 short-circuit + EVALUATE 1509 guards, §12.3.4 GR1 repository inheritance
> into contained programs, §8.4.6.6 self-recursion without a repository entry, 1510 staged non-numeric
> RETURNING; documented deviations = §12.3.8 SR10 forward-definition leniency (ordering diagnostic lands
> with UDF-3 prototypes) + the pre-existing D3/D4 recursive-model static-WS deviation (§14.6.2.3.2/.3).
> All 5 udf_* goldens byte-exact FIRST RUN (incl. udf_recursion 5!=120 + udf_nested_args GR5a by-ref
> mutation); UdfInvocationTests ×26; user-function-invocation-2002 registry+matrix row. Conformance 1645 ·
> unit 123 · 0 regressions. Residue named in the reconciliation as-built: UDF-3 prototypes (1505), UDF-4
> ALL-INTRINSIC/keyword-omitted legs, EXIT FUNCTION (unblocked), class-unit UDF refs, BY VALUE header
> formals, per-evaluation activation (1509), the category-carrying result channel (1510).
> **EXIT FUNCTION leg DONE (DEVLOG 616)** — UdfBindExitFunction → BoundGoback (§14.9.18.4 GR5 synonym;
> RAISING rides GOBACK's), 0827 placement band, exit-function-window matrix row ACTIVE (witness +
> expectDiagnosticBelow 0900 / 0902-at-2023 + the permissive leg), udf_exit_function golden X=0014
> (6 udf goldens). Track (c) residue = M2-UDF-3 prototypes + M2-UDF-4 legs only.
> **(b) pointers increment 2 DONE (DEVLOG 617)** — data pointers end-to-end on the StorageCell+CellPointer
> window model: ADDRESS OF (ForceStringCanonical cell-forcing — the ONE Tier-B re-basing, EXTERNAL unified),
> BASED deref bridges (GR2 NULL / GR3-GR4 loud), SET F7 both directions (SR18), ALLOCATE both formats /
> FREE three-way, F10 arithmetic (GR19 EXACT via UpByScaled — runtime EC-SIZE-ADDRESS); structural §8.8.4.2
> equality; 3 goldens byte-exact; 7 negative cases; zero grammar change; IC/EXTERNAL baselines byte-identical
> over the Holder→StorageCell rename. The adversarial wave (23 confirmed) fixed subordinate-ADDRESS-OF
> ClassOffset drop, the GR19 silent truncation + false registry claim, BASED×EXTERNAL/USING-formal/class-unit
> gates, FREE's missing F3 selection, and REVERTED the wrongly-invented VALUE-on-BASED 0881; named residue =
> CALL-boundary pointer legs, TURN'd-fatal-EC USE-F3 walk, gap-#10 image guard (reconciliation as-built).
> **CI NOTE (DEVLOG 618):** the 615/616/617 pushes ran RED on CI — the legacy jobs run the SHARED 2002
> corpus through the frozen oracle, and `udf_exit_function` + `udf_nested_args` exercise legs it never had;
> both joined the legacy runner's `GreenfieldOnly` exclusion (the DEVLOG-604 mechanism) in 618. ⚠ STANDING
> RULE: enabling a shared-corpus golden ⇒ run the LEGACY conformance suite locally too (or add the exclusion
> in the same change set) — CI is the backstop, not the discovery mechanism.
> **⛔ OWNER GRANTS (2026-07-05, mid-session):** ① **ALL grammar changes pre-authorized** ("approved to make all
> required grammar changes without further owner approval" — `feedback_grammar_approval` rewritten; the FULL
> legacy guard in the same change set remains the discipline); ② **"use worktrees and maximum parallelism where
> beneficial"** — the worktree stale-base hazard is CURED (fresh worktrees verified basing at HEAD; the leftover
> `worktree-agent-a0cfb422` branch deleted; `feedback_worktree_workflows_stale` rewritten).
> **⛔🎉 TRACK (a) COMPLETE (2026-07-05, HEAD=66773d6): increment 1 (national+boolean DATA, DEVLOG 619/620, CI
> green) + increment 2 (boolean OPERATORS, DEVLOG 621) both landed.** Increment 2: B-AND/B-OR/B-XOR/B-NOT LIVE in
> COMPUTE Format 2 (byte-exact vs the ISO Annex A oracle 0100/1101/1001/0011; nesting via parens; figurative
> ALL B"…") + the simple boolean condition over a bare length-1 boolean item; NEW BoundBoolExpr channel + runtime
> CobolBool (28 unit facts); COBOLNET1511 band. Battery: conformance 1803 · unit 187 · FULL guard ALL GREEN.
> ⚠ **THE INCREMENT-2 LESSON**: adding a booleanExpression alt to the SHARED comparisonExpression rule (to get
> boolean relations/IF-conditions) passed the greenfield battery but the FULL LEGACY GUARD caught 31 integration
> regressions — subscript/refmod comparisons at 2002+ (`IF ELEM(I)=x`, every SEARCH WHEN) broke, invisible to the
> greenfield suite which runs at 85. REVERTED. The boolean RELATION (§8.8.4.2.2) + IF-condition B-op forms are
> STAGED RESIDUE for a focused grammar pass that does NOT touch comparisonExpression. **✅ BOTH DONE: the
> boolean-condition pass landed via a `boolExprAhead()` semantic predicate at `primaryCondition` — NOT touching
> comparisonExpression (DEVLOG 622); track (d) file-sharing/lock/retry landed (DEVLOG 623). RESUME NEXT: the
> remaining M2 tracks — M2-OO sub-features (a–h) or the M2-ARITH/M2-PRE/M2-ILA residue.**
> **(a) national/boolean increment 1 DONE (DEVLOG 619)** — M2-DATA-3/4 END-TO-END on the string substrate:
> PIC N = one UTF-16 char per position (D-N1, char-position widths everywhere, ImageWidth never doubled),
> PIC 1/USAGE BIT = one '0'/'1' char per position (D-B1, GR14 R14 — both usages, same storage; pad params on
> CobolString.Store/SpliceInto/Compare carry the category); SR5/SR12/SR13/SR20 usage resolution (0881/0899);
> N"/B" literals at every funnel incl. the NEW SUB_NATLIT/SUB_BOOLLIT lexer tokens (the SUBSCRIPT-mode
> misbind cure — first grammar change under the grant); MOVE Table 16 (0819+SR7); equality-only boolean
> relations via the ONE CheckedRelational factory (0844); national ordinal comparison (never the alphanumeric
> PCS — the &0xFF alias is unreachable); VALUE/88 validation (0898/SR29); INITIALIZE GR6c fills; the D-N2
> byte-surface guards (REDEFINES/cells/FD records/SORT keys reject loud; display-form boolean passes
> deliberately). BOTH goldens byte-exact + ENABLED; national_data ISO-re-baselined TWICE-in-one (the N2A leg
> cut per Table 16 :28847 — verified in-spec — + full-width §14.9.11.4 GR6) with a LegacyDivergent entry;
> 6 negative cases; 4 new test batteries (the parallel worktree test-author's blind tests caught FIVE real
> holes: the 88-renderer raw-splice, ADD-of-B-literal raw emit, INSPECT's over-tight SR1 guard, the EXTERNAL
> silent-skip → 0899-with-reason, RefFailure now names WHY on rejected-class references). Deep-dive currency:
> COBOLNET_DATA_MODEL_DESIGN D8 (D-N1..D-N4/D-B1; supersedes the PIC 1→bool sketch). Battery: conformance
> 1760 · unit 159 · legacy ConformanceTests 55 · FULL guard on the .g4 change. The M2-DATA-3/4 design +
> as-built + the INCREMENT-2 boolean-operators design (recon'd CONCURRENTLY, ready) live in the reconciliation.
> **RESUME = the remaining Phase-4 tracks, sized against truth (docs/PHASE4_RECONCILIATION.md).** A fresh
> session: ① confirm CI green on HEAD; ② pick the next track and run the PROVEN
> cadence — a RECON WORKFLOW (parallel readers: goldens/spec/seams/model → xhigh synthesis) producing a
> decision-complete design INTO the reconciliation doc, spot-verify its anchors, implement, then an
> ADVERSARIAL find→verify workflow over the diff with every confirmed finding fixed/staged/documented in the
> SAME change set, full battery + legacy conformance, commit+push (the DEVLOG 615/617/619 pattern). ⭐ NEXT UP:
> **(a) increment 2: the boolean OPERATORS B-AND/B-OR/B-XOR/B-NOT** (§8.8.2 + COMPUTE Format 2 + the boolean
> relation/simple-condition legs — the DECISION-COMPLETE design is already in the reconciliation beside
> M2-DATA-3/4, recon wf_600c5a02; grammar pre-authorized; golden `boolean_ops` will need GreenfieldOnly).
> Then **(d) sharing/lock/retry** (M2-FILE-1 — grammar now granted; no SHARING/LOCK MODE/RETRY in CobolIO.g4;
> runtime CobolFile.Locked primitive exists); OO riders queued (interface GET/SET PROPERTY prototypes,
> group-valued property refs, FACTORY OF usage, object views). Phase 5 intrinsics / 6 (OCCURS DYNAMIC, TYPEDEF) / 7 (2023) per the reconciliation.
> Deferred-from-review (queued, documented): the §13.18.40.6 PICTURE precedence-table pass; Tier-A/BINARY
> figurative-MOVE receivers stay runtime-loud; exit-window conforming witnesses need METHOD-ID/FUNCTION-ID
> units (Phase 3/4c); rounded_mode_prohibited's SIZE-ERROR leg (Phase 5/7); options_paragraph awaits
> ARITHMETIC IS STANDARD (Phase 4e). W3-④ residues: the full '85 debug facility (DEBUG-ITEM registers,
> trigger invocation — the golden-less DB1xx/2xx series) stays deferred; binder-side WITH-DEBUGGING-MODE
> detection is per-unit (nested inheritance validator-side only).**
>
> **(superseded) STATE (DEVLOG 590, 2026-07-03 21:10): ⛔🎉 ROADMAP PHASE 1 COMPLETE — the EditionValidator Wave 1 landed
> END-TO-END in one autonomous session (DEVLOG 583–590, commits c0cf723…): P2.1 channels + `Removed()` seam ·
> P2.2 validator pass (fail-fast pre-Emit) · P2.3 the 0900–0903 band · P2.4 the FOUR-SOURCE reserved-word
> tables (GnuCOBOL per-standard lists disk-to-disk + spec §8.9 + VCR row 32; the funnel LIVE, token-type
> restricted per the RW104A hazard; THREE recall corrections incl. the third re-reserved word END-RECEIVE and
> the EC words being 2002-reserved) · pre-P2.5 scrub + the TYPE `{is2002()}?` gate fix · P2.5 registry + both
> drift disciplines · P2.6 the full Wave-1 gate batch (15 rows, 12 overrides, the STOP-literal BoundStopLiteral
> fix, the 0873 FD+SD migration, the 0903 archaic flags) · P2.7 THE FLIP (permissive continuity restated;
> **INV-1-STRONG AT 2023: 349/349 goldens BYTE-EXACT at `--std 2023 --permissive`, zero behavioral diffs** —
> the roadmap's fatal-challenge criterion, seeded via `COBOLNET_NIST_STD`/`COBOLNET_NIST_PERMISSIVE`) · the
> permissive sweep 419 OK/0 BREAKS wired into CI · `docs/COBOLNET_VALIDATION_DESIGN.md` (as-built + the
> measurable G7 exit criteria) · the corpus runner shells + manifests (2002: 0/33, 2014 seeded, negative/
> shell). Suites: conformance 1195/1195 + unit 38/38 + guard 353 MATCH. ⚠ Content filter tripped a 4TH time
> (even file-Writes of word lists) — the standing rule is now ABSOLUTE: word lists move disk-to-disk only.
> ⚠ guard-fast showed 2 transient single-program flakes (re-run clean; JOBS=32 race suspected, watched).
> **RESUME = roadmap Phase 2: W2 (MOVE rows SR5 · the loud-guard misbind sweep + national/boolean skeleton ·
> negative-corpus seeds · position-aware token checking · VCR flips · adversarial review) → W1.5 (~24 0900
> upgrades) → W3 (the serialized grammar batch + FULL legacy guard). Then Phase 3 = the M2 OO port (the
> refreshed `docs/COBOLNET_OO_DESIGN.md` carries the regenerated port map).**
>
> **(superseded) STATE (DEVLOG 582, 2026-07-03 14:28): ✅ roadmap Phase 0 COMPLETE — the DEVLOG-579 baseline reproduces
> green after the idle gap (fresh full build 0W/0E; 1074 conformance + 29 unit; guard-fast 353 MATCH,
> 0 regressions). ✅ The roadmap is now ISO-VALIDATED against `specs/ISO_COBOL.md` (12-agent workflow: 39
> claims — 30 confirmed / 9 partial / **0 refuted**; 10 serious coverage gaps) — corrections D1–D16 + minors
> APPLIED INLINE (newly-owned mandatory surface: boolean OPERATIONS, `&`-concat, CONSTANT, DYNAMIC-LENGTH
> items, §4.2.2 suboption, EXTERNAL cluster; >>PROPAGATE re-editioned ≤2014; the §4.2.16 seven-leg conformance
> doc; the Phase-1 scrub gains the float-edge split + CONSTANT/`&` row seeds; word tables designed per-unit
> overridable for COBOL-WORDS); the audit report is the roadmap's Appendix. RESUME superseded above.**
>
> **(superseded) STATE (DEVLOG 581, 2026-07-03): ⛔ the G7→G8 EXECUTION ROADMAP is `docs/COMPLETION_ROADMAP_COUNCIL.md` —
> RATIFIED by the owner (all 11 §5 decisions resolved: #1 = NO standards acquisition, the in-repo 2023 spec
> [`specs/` PDF + MD] is the sole ISO authority with Annex E + the legacy inventory adjudicating prior-edition
> edges; #2–#11 = council defaults, incl. the #6 OO/M2 grammar grant). Docs-only checkpoints (580/581); the
> tree is otherwise at the DEVLOG-577 code state. The roadmap upholds the SSOT §16 spine + 7 ratified deltas
> (its §3) incl. the behavioral leg at `--std 2023` on the P2.7 flip, the pre-P2.5 constructs.json scrub
> [JSON/XML rows are non-ISO → vendor-dialect post-G8; the CobolData.g4:246 `{is2023()}?` TYPE gate is
> provably wrong → provisional 2002; XOR = 2023 per Annex E], and G8 in 3 serial cuts. **RESUME = the roadmap's
> Phase 0 (clean full rebuild + baseline re-verification after the idle gap) → Phase 1 (EditionValidator Wave 1
> = P2.1–P2.7 per the VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" section + the roadmap's Phase-1
> additions).** ⚠ The `/e/tmp/g7` + `/e/tmp/nc-sweep` convenience copies are VERIFIED GONE — the in-repo doc
> sections are the ONLY source.**
>
> **(superseded) STATE (DEVLOG 579, 2026-06-11 13:42): ② G7 Phase-2 EditionValidator — DESIGN COMPLETE, IMPLEMENTATION NOT
> STARTED (docs-only checkpoints 5be1983 + this; tree otherwise at the DEVLOG-577 state). ⛔ RESUME = read
> **`docs/VERSION_TEST_MATRIX_DESIGN.md` → "Phase-2 implementation plan" (below its §8)** — the IN-REPO canonical
> plan (P2.1 EditionContext warning-channel + Permissive axis + Removed() seam; P2.2 the visitor hook [no listener
> — `CobolParserCoreBaseVisitor<object?>`]; P2.3 the COBOLNET0900–0903 band; P2.4 the cobolWord single-funnel
> reserved check + scripted §8.9 table generation incl. OCR fixes/METHOD omission + the conservative-confidence
> policy; P2.5 registry+drift; P2.6 the Wave-1 construct checklist; P2.7 harness changes + the
> NIST-continuity→permissive flip; P2.8 Waves 2–3 incl. the latent bugs: STOP-literal mis-bind, MOVE ALL-digit
> loud gap, the XOR/EXCLUSIVE-OR 2023-words-as-unconditional-tokens hole). Convenience copies + raw scout
> artifacts: `/e/tmp/g7/` (RESUME-G7-PHASE2.md, words-2023.json, removal-inventory.json, validator-arch.md) —
> the doc section is SSOT if they diverge. ⚠ Do NOT delegate word-list output to agents — content-filter trips
> on it (2nd occurrence, DEVLOG 578); build tables in-session by script from spec §8.9 (lines 10306–10788).
> Then implement Wave 1 exactly per P2.1–P2.7.**
>
> **(superseded) STATE (DEVLOG 577, 2026-06-11 12:09): ✅ the EC EXCEPTION-CONDITION MODEL (SSOT §16 step ①) is DONE END TO
> END — ISO §14.6.13 + §7.3.25 >>TURN (the compile-time TurnState fold; zero-scaffolding when off) + §14.9.29
> RAISE / §14.9.33 RESUME (both forms, the ResumeSignal/dispatch-result protocol) / §14.9.49 Format-3 USE
> (GR3c–g `__EcDispatch` tiers) / §14.9.18+§14.9.14 GOBACK/EXIT RAISING propagation (CALL-site pickup + the
> ProgramRegistry boundary default; ¶27403 SR2) / the §9.1.13.1 status→EC bridge (`__IoCheckEc`) / the EC-SIZE,
> EC-OVERFLOW, EC-PROGRAM, EC-ARGUMENT-FUNCTION raise points / §15.28–33 EXCEPTION-* functions / SET LAST
> EXCEPTION TO OFF / the full 0710–0719+0875–0879 diagnostics band with per-edition gating. The deep-dive
> (`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md`) carries the as-built section. **1074 conformance (48 new EC
> facts) + 29 unit (13 new TurnState/catalog facts) ALL GREEN; FULL legacy guard ALL GREEN (353 MATCH + 11
> LEGACY_DIVERGENT, 0 regressions) on the shared-frontend grammar change.** Later-wave EC remnants (listed in
> the deep-dive's as-built tail): PERFORM…WHEN + >>PROPAGATE (2023), exception OBJECTS (OO wave), the
> EXCEPTION-FILE 2023 file-connector arg, national -N twins, GLOBAL-walkable F3.
> **RESUME AT: ② G7 per-edition correctness — Track-1 Phase-2 EditionValidator (removal/reserved-word gating
> per `docs/VERSION_CHANGE_REFERENCE.md` + the negative corpus) and the M2 2002 catalog (OO is the largest —
> reuse the legacy OO design per `project_oo_reuse_legacy`; then UDF prototypes, national/boolean,
> pointers/ALLOCATE, SHARING/LOCK) → M3 2014 (dynamic tables, TYPEDEF, JSON/XML) → M4 2023 deltas;
> ③ reserved-word tables (scout failed on content-filter — re-run; the `RF` find → 0900+ band); ④ G8 cut-over.**
> *(The Phase-1 banner below is history.)*
>
> **(superseded) STATE (DEVLOG 575, 2026-06-11 02:15): ⛔🎉 PHASE 1 COMPLETE — the COBOL-85 corpus drive (G5/G6) is CLOSED.
> Every golden-bearing NIST program is locked byte-exact: 318 = 93 NC + 29 ST + 32 RL + 40 IX + 23 IC + 69 SQ
> + 42 IF + 15 SM + 4 RW + 2 OBSQ. The final census (403 programs, `/e/tmp/nc-sweep/census-phase1-final.txt`):
> 357 GREEN, ZERO DIFF/CMPL_FAIL; residue is golden-less by NIST design (33 NO_GOLDEN, 6 chain-intermediate
> NO_REPORT, the 5-program cross-assembly subprogram-as-main RUNERR family + IC401M's no-golden companion, and
> IC113A = a CONFORMING infinite loop per §14.9.14 GR2). 1026 conformance + 16 unit; legacy guard 353 MATCH +
> 11 `LEGACY_DIVERGENT` (citations in `scripts/guard.sh`), 0 regressions. Landed in Phase 1 (DEVLOG 571–575,
> six parallel scouts + five implementation agents): SM/COPY (the NIST default copy library §7.2.3.4 GR3),
> the FULL intrinsic catalog (§15, `IntrinsicCatalog` + `CobolIntrinsics.*`/`CobolDate`), the Tier-C
> mixed-usage record codec (zoned digit images §13.18.60 GR4), LINAGE (§13.18.34 end-to-end + 3 re-baselines),
> the IC residue (CALL ON EXCEPTION gate, EXTERNAL FD `::EXT::` connectors, cross-program GLOBAL USE,
> cross-assembly CALL sibling probe, GR4 group-sender MOVE), PROGRAM-ID IS-noise-words (§11.4.2, guard-gated),
> and the REPORT WRITER subsystem (§13.24 `CobolReport` engine + USE BEFORE REPORTING).
> **RESUME AT (the SSOT §16 order): ① the EC exception-condition model (§11 / the conditions-exceptions
> deep-dive: >>TURN, the EC-* hierarchy, RAISE/RESUME, USE AFTER EXCEPTION CONDITION; the seams are placed,
> checking is OFF per §18.16); ② G7 per-edition correctness — Track-1 Phase-2 EditionValidator
> (removal/reserved-word gating per `docs/VERSION_CHANGE_REFERENCE.md` + the negative corpus) and the M2
> 2002 catalog (OO is the largest; then UDF prototypes, national/boolean, pointers/ALLOCATE, SHARING/LOCK)
> → M3 2014 (dynamic tables, TYPEDEF, JSON/XML) → M4 2023 deltas; ③ reserved-word tables (scout failed on
> content-filter — re-run; the `RF` find → 0900+ band); ④ G8 cut-over. Removed-from-residue notes: NC214M/
> NC303M/RW301M/302M/IF401M-403M/SM301M/401M etc. are NO_GOLDEN run-only; the sign-off/divergence protocol
> is established (verify → re-baseline → `LEGACY_DIVERGENT` + citation → lock).**
> *(Pre-Phase-1 banner below for history.)*
>
> **(superseded) STATE (DEVLOG 570, 2026-06-10 22:30): 273 NIST programs locked (92 NC + 25 ST + 32 RL + 40 IX + 18 IC +
> 64 SQ + 2 OBSQ — the authoritative `[InlineData]` census; earlier running tallies were undercounts);
> 827 conformance + 15 unit; FULL legacy guard ALL GREEN = 356 MATCH + 8 `LEGACY_DIVERGENT` (the owner-approved
> ISO re-baselines, DEVLOG 569/570: IX111A/IX210A/IX214A/IX215A/NC235A/NC236A/SQ207M = legacy HOLES;
> ST146A = an UNDEFINED-CHOICE — §14.9.30 GR18 leaves the record area undefined after an unsuccessful READ and
> COBOL.NET's canonical refinement is UNCHANGED, the spec's own pattern for the other I-O verbs; the list +
> per-program citations live in `scripts/guard.sh`). The fresh chain-aware census
> (`/e/tmp/nc-sweep/census-current.txt`, pre-567): 268/361 GREEN — ⚠ regenerate it: the sweep's binary
> false-green is fixed (`diff -a`) and chains have grown. Landed in the prior session
> (560–570): ① CHAIN-CONSUMER harness support —
> `tests/nist/chains.tsv` is the ONE chain topology (consumer→producers), `NistDifferentialTests.RunNist` runs
> predecessors in the consumer's own temp dir, the sweep gained CHAINERR + the `-s` nonempty-dll check → 22
> programs locked with zero compiler change (560); ② secondary-record SORT keys SR6e + SR6g diag 0874 + the
> ONE record-area accessor `FileModel.AreaRecord` (§13.4.2 — sequential READ, both KeyedIo sites, sort RETURN,
> WRITE-filename fallback) → ST111A/ST124A (561); ③ I-O-CONTROL SAME RECORD AREA → synthesized cross-file
> REDEFINES (§12.4.6.4 GR2; Formats 1/3 conformant no-ops; SR2–SR11 staged to EditionValidator) → ST131A/
> IX205A/IX206A (562); ④ variable-length records END-TO-END §13.18.43 — FD RECORD VARYING/m-TO-n binding,
> GR13a DEPENDING length on WRITE/REWRITE, GR15 read-back stores, '44' GR14/GR16/GR20 checks, 4-byte-LE length
> framing on varying connectors, GR8a ODO-minimum record sizes, OPEN I-O ReadWrite + logical-offset rewrites,
> the LAST-trivial-exit termination-tail fix → SQ212A/SQ228A/RL206A/RL211A/IX106A (563); ⑤ qualified
> RECORD/ALTERNATE KEY operands (§8.4.2.2 `FindQualified`) → IX215A compiles, all 39 PASS (564); ⑥ no 0-byte
> dll on failed emit + the Tier-C record fence → hostpolicy-phantom class retired, SQ203A locked (565);
> ⑦ XXXXX064 + ST144A re-baselined from the legacy (566); ⑧ the IX109A chain family — TF024 has TWO producers
> with different key universes; 8 status-test consumers locked behind IX109A (567); ⑨ FILE-NAME qualification
> §8.4.2.2 + ADVANCING mnemonic-name = 0 lines (568 — the legacy DROPS the AFTER-mnemonic write, a hole; SQ207M
> DIFF 4 = exactly those rows → sign-off queue).
> **RESUME AT — the census residue by family** (regenerate `census-current.txt` first — the driver below):
> ① NC105A last rows (`nc105a-brief.md`, untouched); ② SQ101M (DIFF 11, extra blank lines — print-control
> corner) + the LINAGE subsystem (§13.18.36 + LINAGE-COUNTER §8.4.3.14 — SQ201M/208M/209M/210M all loud on it);
> ③ the Tier-C COMP-record codec (COBOLNET_DESIGN §4.2/§8 — ST108A/127A/133A/134A, honestly loud now);
> ④ IC residue: cross-FILE dynamic CALL (IC109A/110A/117M/205A/210A `EC-PROGRAM-NOT-FOUND` — the pre-G8
> cross-assembly CALL item), IC113A timeout, IC222A (CALL ON EXCEPTION gated 2002+ yet used by a CCVS-85
> program — version-targeted investigation), IC233A/234A cross-program GLOBAL USE (designed §5.6 of the
> declaratives brief), IC207A/227A EXTERNAL FD, IC401M parse; ⑤ SM = COPY support (the whole suite);
> ⑥ ~~owner sign-off queue~~ DONE (569/570 — all eight re-baselined + locked, ST146A included after the
> definitive spec search; the legacy guard reports them as `LEGACY DIVERGENT`, list + citations in
> `scripts/guard.sh`);
> ⑦ reserved-word tables (scout FAILED on content-filter — re-run; the `RF` find → 0900+ band);
> ⑧ CALL follow-ups: GR3a subscripted-BY-REFERENCE capture, ContainsNextSentence arm for ON-phrase bodies,
> OMITTED args + header mode phrases (grammar). Then steps ④–⑦ (EC model → Phase-2 EditionValidator → 2002
> OO/UDF → 2014 JSON/XML → 2023 → G8). ⚠ Apply agent edits with the index-based python pattern; the Bash
> transport mangles backslash escapes — never inline them in heredocs.
> **SESSION ARTIFACTS (off-repo, E:\tmp):** scout briefs in `/e/tmp/verb-briefs/`; sweep infra + censuses in
> `/e/tmp/nc-sweep/` (driver: `xargs -P 14 -I {} bash sweep-one.sh {} < all-census-list2.txt` — sweep-one.sh
> is now chain-aware via `tests/nist/chains.tsv` and judges compiles by NON-EMPTY dll); a frozen CLI at commit
> e591da2 in `/e/tmp/cobolnet-frozen/` (pre-560; rebuild or use the live CLI for new scouting). Memory
> `project_greenfield_state.md` mirrors this map.**
>
> > **STATE (DEVLOG 552, 2026-06-10 17:30): 88/95 NC byte-match + 14 ST locked; 605 conformance + 15 unit; legacy
> guard ALL GREEN (re-proved on the RETURN grammar change). Landed since 545: ref-mod completion (548 — numeric
> items via NumericImagePlace + the parsed refModSpec form), null-table benign chains (549), RENAMES/REDEFINES
> layout closeout (550 — nested-class dissolution, no-THRU alias forward, StoreDisplay storage-form bridge,
> redefiner-excluded group width), ODO (551 — wave-2 agent; NC235A spec-pinned EXCEEDING the golden), SORT/MERGE
> (552 — wave-2 pipeline; 6 facts spec-pinned over a legacy GO-TO-loop hole; VERSION_CHANGE_REFERENCE Table 7 =
> the first 85→2002 rows). **RESUME AT: integrate the two PARKED wave-2 families from `/e/tmp/wave2-hold/`** —
> (1) KeyedIO (RELATIVE/INDEXED files; 19 anchored edits in `/e/tmp/verb-briefs/wave2result-keyedio.json`;
> targets RL/IX suites), then (2) CALL (11 STRUCTURAL edits in wave2result-call.json — multi-unit emission,
> instance program classes/__Activate, FieldEmitter instance fields; targets the IC suite; integrate ALONE and
> verify hard). Then: NC107A/108M (DECIMAL-POINT COMMA — brief at `/e/tmp/verb-briefs/wave2result-decimalPointBrief.txt`),
> NC105A's last 8 group-move rows, ST chain-consumer harness support, the ST105A/110A residuals (chain + an
> over-broad SR6 key check), then steps ④–⑦. Apply edits with the index-based python pattern (the Bash transport
> mangles backslash escapes — never inline `
` in heredoc scripts).**
>
> > **STATE (DEVLOG 545, 2026-06-10 13:20): 82/95 NC byte-match (locked), 564 conformance + 15 unit green. STEP ①
> ('85 verbs) COMPLETE: EVALUATE (542), the SIX-FAMILY VERB WAVE (543 — INSPECT, STRING/UNSTRING, INITIALIZE,
> ACCEPT, CORRESPONDING, ALTER-85-only + switches/SET-F3/switch-conditions, via 6 parallel agents over disjoint
> `StatementBinder.<Verb>.cs`/`CSharpEmitter.<Verb>.cs` partials), the ARITHMETIC WAVES (544 — §14.7.7 single
> evaluation/Snapshot, edited SIZE ERROR TryFormat, P-scale images, BFS sign-condition operands, figurative 88s),
> and the MOVE/PICTURE closeout (545 — CR/DB classification, group VALUE distribution, BLANK WHEN ZERO, AN→NE
> editing, P-scaled edited masks). Plus: JUSTIFIED, user-defined classes, zoned IS NUMERIC on character-backed
> views, level-66 RENAMES places, de-editing GR5, VARYING-AFTER augment order. Remaining 13 NC: collating
> NC215A/219A, DECIMAL-POINT COMMA NC107A/108M, ODO NC235A/247A, REDEFINES-tier NC252A (REDEF11 emission),
> NC105A/224A/401M louds, NC236A spec-pinned, NC214M/303M no-golden. **RESUME AT: the wave-2 scout briefs
> (CALL/inter-program, SORT/MERGE, RL/IX files, collating, ODO, DECIMAL-POINT COMMA) — workflow w7euzpx9q output at
> `C:\Users\brent\AppData\Local\Temp\claude\E--CobolSharp\de401450-b4ae-4dd4-acdd-ad95a132ce21\tasks\w7euzpx9q.output`;
> repeat the DEVLOG-543 pattern: 6 implementation agents over disjoint partial files, serial integration,
> per-cluster sweeps (`/e/tmp/nc-sweep/sweep-one.sh`, pipes .dat + COBOL_SWITCH_1), suites, locks, commit+push.**
> Then steps ④–⑦: EC model → Phase-2 EditionValidator + 85→2002 ledger → 2002 OO/UDF → 2014 JSON/XML → 2023 →
> G8 cut-over.** (Pre-verb-wave STATE below for history.)
>
> **(superseded) STATE (DEVLOG 530, session of 2026-06-10):** G1 ✅, G0 ✅, **G2 ✅, G3-core ✅, G4 ✅, G5 SEQUENTIAL FILE I/O ✅,
> G6-core ✅, REDEFINES Tier A+B ✅, ON SIZE ERROR ✅, PICTURE P ✅, de-sign ✅, **SIGN-clause inheritance ✅ (525),
> SET + index machinery + USAGE INDEX ✅ (526), sections-as-procedure-targets + qualified procedure-names +
> TIMES-once ✅ (527), PERFORM VARYING complete + ALL-to-group repeat + benign OOR subscripts via CobolTable.At ✅
> (528), CobolEdit numeric-edited receivers + DIVIDE REMAINDER + alphanumeric→numeric MOVE ✅ (530).**
> **54 NC programs byte-match the golden, all locked into `NistDifferentialTests`. 437 conformance + 15 unit
> green (DEVLOG 533–538: B2 Tier-B layout + GAP-1 subscripted views + NEXT SENTENCE + GAP-2 qualified subscripts
> + qualified/subscripted 88s + B3 + alphanumeric-edited pictures + Int128 division kernel + SEARCH F1+ALL +
> literal-vs-group comparisons + §14.9.12 GR6c subsidiary quotient + print-file plain WRITE). Legacy-oracle hole
> #4: legacy SEARCH VARYING-other-index falls through to DE-LETE — NC236A SPEC-PINNED, golden re-baselines at G8.
> **The 10-agent DIFF-diagnosis output (full per-row maps) is at
> `C:\Users\brent\AppData\Local\Temp\claude\E--CobolSharp\de401450-b4ae-4dd4-acdd-ad95a132ce21\tasks\wl4xosa5z.output`
> — remaining DIFFs:** NC172A/173A/175A/170A = expression-pipeline products beyond long (the FULL CobolInt/Int128
> carrier in NumericRenderer — multiplications and aligned sums, not the kernels); NC114M (16) edited corners;
> NC219A (16) collating; NC215A (34), NC104A (34), NC235A (44, +ODO), NC250A (50), NC124A (28). RUNERR remainder
> (~29): CORR (NC202A/207A/208A/209A/253A), INSPECT/STRING/UNSTRING/EVALUATE/INITIALIZE/ACCEPT verbs, ALTER,
> switch-status conditions (NC174A/254A — SPECIAL-NAMES SWITCH + COBOL_SWITCH_n env, see legacy SwitchRuntime),
> NC105A group-in-numeric. Then Track-1 Phase 2 EditionValidator.** **TRACK-1 PHASE 1 ✅ (531):** canonical
> `tests/version-matrix/constructs.json` (12 rows, 48 cells green) + `EditionHarness` + the FULL INV-1 continuity
> sweep (`scripts/version-continuity-sweep.sh`) — **342/342 85-compiling NIST programs clean at 2002/2014/2023,
> zero breaks**; first matrix catch fixed the placeholder JSON/XML grammar stubs to the real seam surface (legacy
> guard re-ran ALL GREEN). **Serial SEARCH F1 ✅ (532)** — label-loop emission; its cluster now gates on the
> wave-4 resolver gaps (subscripted Tier-B views needs B2 first) + NEXT SENTENCE. **NEXT: Phase 2
> EditionValidator** (removal/reserved-word gating + edition-NAMING diagnostics, the diagnose-correctly half). The docs corpus was GOAL-ALIGNED (529) to the
> owner's four restated goals (4-compilers-in-one + diagnostics co-equal; commercial/.NET 10/C#14-or-later with
> .NET 11-preview pre-authorized; legacy = reference/oracle ONLY; ICodeGenBackend dual-backend discipline).
> Greenfield-only; the shared front-end + legacy are untouched (re-run `scripts/guard-fast.sh` before touching
> `Cobol.Net.Frontend` or `CobolSharp.*`). **Default `--std` is COBOL-2023** (`--nist` ⇒ 85).
> ⚠ Legacy-oracle holes found this session (use SPEC-PINNED tests for these): same-section duplicate paragraph
> resolution (§8.4.2.2.1 r6), omitted-BY multi-AFTER VARYING (legacy binder crashes), DISPLAY trailing-trim (known).
>
> **VERSION-CORRECTNESS FRAMEWORK (the path to multi-edition support — NEW, DEVLOG 512–520):**
> - **The investigation rule (`feedback_version_targeted_semantics`):** when the legacy oracle ≠ ISO-2023, deep-
>   investigate whether it's a cross-EDITION change; if so gate by `DialectLevel`; only pin-to-spec for version-invariant
>   legacy bugs. (Investigated DEVLOG 517: DISPLAY-trim / comparison-de-sign / group-de-sign are all version-invariant
>   bugs → pinned to spec, all dialects.)
> - **`docs/VERSION_CHANGE_REFERENCE.md`** — 130-row checklist of every edition change the 2023 spec documents (Annex
>   E.2/E.3 = 2014→2023, Annex F archaic/obsolete, FLAG-02/14, inline NOTES). Scope limit: 2014→2023 complete; 85→2014
>   under-documented (harvest those from the grammar `is2002/2014/2023` gates + the legacy `DialectStrictnessChecks`).
> - **`docs/VERSION_TEST_MATRIX_DESIGN.md`** — test as N per-edition compilers: (construct × edition) matrix, expected
>   outcome computed from introducedIn/removedIn/behaviorVariants; 3 invariants (continuity / introduction-gating /
>   behavior-correctness); phased rollout. **Phase 0 DONE** (`VersionMatrixTests.cs`): introduction-gating proven both
>   ways (DELETE FILE rejected <2023, compiles @2023) + continuity (NC101A/211A/136A compile at later editions).
> - **KEY FINDING (DEVLOG 520):** the greenfield's edition-gating is almost entirely "ENABLE post-85 features"
>   (introduction, via 39 grammar predicates); **REMOVAL / reserved-word gating is essentially ABSENT** — an 85 program
>   using a 2023-removed construct or later-reserved word compiles unchanged at 2023 (continuity sweep: 89/89 NC compile
>   at both 85 and 2023, zero breaks). That ABSENCE is the Phase-2 worklist: a greenfield **`EditionValidator`** (port
>   the legacy `DialectConfig` + `DialectStrictnessChecks` validator pattern) that rejects-strict/warns-permissive per
>   the reference doc. (Owner policy: removed → error strict / warn permissive.)
> **CI fix (DEVLOG 512):** the recurring Linux-CI flake was a compiler data race — `BoundTreeValidator` held its
> `SemanticModel` in a STATIC field, clobbered under the suite's parallel in-process compilation → a spurious CBL1603
> on START-with-KEY programs → compile fail. Fixed by instance-izing the validator; `ConcurrentCompilationTests`
> reproduces+pins it (reproduce Linux-only flakes via WSL — see memory `reference_wsl_linux_repro`).
> **CI gap CLOSED (DEVLOG 515):** the greenfield `Cobol.Net.Tests.Conformance`/`Unit` now run in CI (both the Linux
> guard job and the Windows job) — they were legacy-only before; validated green on Linux. ⚠ **Don't interleave WSL
> (Linux) and Windows builds in the same tree without a clean — it contaminates `obj/bin` and a Windows incremental
> `dotnet test` can silently reuse a pre-edit Linux compiler (bit me in 516; CI is unaffected, fresh per-platform).**
>
> **THIS SESSION (498→504), the G5 milestone + the corpus-drive start:** (498) nested Tier-B REDEFINES backing path
> qualified (the NC101A blocker — `ReferenceResolver.BackingPath`); (499) **the sequential file I/O subsystem** —
> `Cobol.Net.Runtime/IO/` `SequentialFile` connector (OPEN INPUT/OUTPUT/EXTEND/I-O + OPTIONAL, CLOSE, WRITE plain +
> AFTER ADVANCING, READ, REWRITE; ISO §9.1.13 status + §14.9.30/35 read-state machine ported, string-image substrate,
> no byte State) + `CobolFile` facade; `DataBinder` binds FILE-CONTROL SELECT + FILE SECTION FD records (`FileModel`;
> multi-01 shared area = synthesized REDEFINES → existing tier machinery); `BoundOpen/Close/Write/Read/Rewrite` +
> binder + emitter (register at Main, CloseAll in finally). (500) **PICTURE P scaling** as ONE signed scale (removed
> the vestigial `NumProfile` P fields + the FractionScale clamp; `UnscaledAtScale` negative-scale) → **NC101A GREEN**.
> (501) mapped the NC series + locked the 7 green + `Pow10D` negative-scale. (502) **fixed 3 compiler HANGS** — the
> deeply-nested-group O(2^depth) emission blowup, now memoized (`FieldEmitter.PhysicalChildrenOf`). (503) **level-77 is
> a root** (independent item) — cleared 12 NC compile-errors. (504) **figurative constants in numeric / VALUE contexts**
> (ZERO in arithmetic → 0; `ALL ZEROS` VALUE init) — cleared 5 more. (505) **figurative ZERO in a level-88 VALUE**
> (numeric) — cleared NC250A's `ZERO00L`; **identified the systematic Tier-B `.BB` blocker** (RESUME AT #1).
>
> **✅ OWNER DIRECTIVE DELIVERED (DEVLOG 539–541): "numerics properly implemented for each language version, end
> to end" — ALL FOUR WAVES SHIPPED.** N1 Int128 carrier (D1+D2); N2 wide tier (PIC 9(19..31) → Int128); N3
> edition gates (digit/literal caps 18@85 / 31@2002+ / never>31, OPTIONS paragraph + ROUNDED MODE IS 2014+,
> §14.7 composite-31 — the 85-tightening-to-18 was REFUTED by CCVS-85/NC101A itself); N4 ARITHMETIC modes
> (NATIVE = documented Int128 engine; **STANDARD-DECIMAL implemented** — CobolDec SDIDI, decimal128/34-digit
> per-op semantics with all four INTERMEDIATE ROUNDING modes; STANDARD-BINARY documented-unsupported
> COBOLNET0806; plain STANDARD dropped@2023 COBOLNET0807). **474 conformance + 15 unit green; 64 matrix cells.**
> Numerics follow-ups (small, when touched next): COMP-5 binary-wrap store discipline; float↔fixed mixed
> expressions (D7); de-editing senders (§14.9.25 GR5); FLOAT-BINARY/-DECIMAL OPTIONS clauses. Resume the corpus
> waves below.
>
> **RESUME AT (refreshed after DEVLOG 530 — the 6-agent diagnosis workflow's wave plan is the worklist; its full
> output is in the session log of 2026-06-10):**
> - **(1) The remaining DIFF programs' single root causes:** NC203A/251A/170A/172A/173A/175A + NC171A = the
>   **deferred G3 Int128 intermediate-arithmetic** (18-digit dividends, π-precision quotients — build the CobolInt/
>   Int128 carrier per the numeric design); NC114M = **alphanumeric-EDITED insertion** (B/0,// in PIC X-edited —
>   extend CobolEdit/alphanumeric path); NC219A = the **PROGRAM COLLATING SEQUENCE subsystem** (ALPHABET … ALSO,
>   custom ordinals, HIGH/LOW-VALUE re-association — diagnosis wave 12 has the full inventory); NC124A (diff 382).
> - **(2) The RUNERR clusters (49 programs), by yield:** SEARCH F1+F2 (§14.9.37, ~8 programs, needs OCCURS KEY
>   capture for SEARCH ALL); subscripted/qualified condition-names (NC235A/250A); the remaining reference-resolver
>   gaps (diagnosis wave 4: subscripted Tier-B views GAP-1 — REQUIRES the B2 offset×OCCURS fixes first —, qualified
>   subscripts GAP-2, ref-mod-on-numeric GAP-3, RENAMES) → NC125A/132A/204M/206A/210A/224A/246A/252A; CORR +
>   NEXT SENTENCE (wave 10) → NC202A/207A/208A/209A/253A/174A/254A; INSPECT/STRING/UNSTRING/EVALUATE/INITIALIZE/
>   ACCEPT verbs (NC115A/216A/217A/218A/223A/225A/109M/214M/221A/122A); DECIMAL-POINT IS COMMA + BLANK WHEN ZERO
>   (wave 11) → NC107A/108M; ALTER (NC302M/303M); remaining Wave-0 bugs B2/B3/B4 (Tier-B accounting — mandatory
>   before GAP-1) + NC105A/103A/126A/215A singles.
> - **(3) Track 1 Phase 1** (unstarted this session): full INV-1 continuity sweep SM/IC/SQ/RL/IX/ST × editions;
>   `tests/version-matrix/constructs.json`; the `GetDiagnostics(source, edition)` harness; then Phase 2
>   EditionValidator (the diagnose-correctly half of the four-compilers mission — co-equal, owner-emphasized).
>
> **The original two-track framing (kept for context):**
>
> **TRACK 1 — VERSION-CORRECTNESS ROLLOUT (the systematic path to multi-edition support):**
> - **Phase 1 (next):** (a) the FULL INV-1 continuity sweep — every NIST-85 program × {2002,2014,2023} asserts "still
>   compiles" (the NC sweep already shows 89/89 at 2023; extend to SM/IC/SQ/RL/IX/ST and classify any break as a
>   reference-doc removal/reserved-word row vs a regression); (b) move the inline catalogue to the canonical
>   `tests/version-matrix/constructs.json` (doc §10 #5) and seed the ~12 highest-value reference-doc rows; (c) port the
>   greenfield **diagnostic-assertion harness** (`GetDiagnostics(source, edition)` / `AssertHasDiagnostic`).
> - **Phase 2:** build the greenfield **`EditionValidator`** + `ConstructDialectStatus` registry (port the legacy
>   `DialectConfig` two-axis model + `DialectStrictnessChecks` validator pattern) so removed/obsolete constructs and
>   later-reserved words are REJECTED (strict) / WARNED (permissive) per the reference doc — this is the currently-absent
>   half (DEVLOG 520 finding). First target: standalone `END` removal (the grammar already over-restricts it to 85-only
>   via `{is85()}?` though it should be valid through 2014 — a logged matrix red to fix); then the reserved-word and
>   removed-construct rows. Thread `DialectLevel` into bind/emit for the (future) behavior-variant rows.
> - The matrix IS the worklist: each red cell = a reference-doc-driven gating task. (`feedback_version_test_matrix`.)
>
> **TRACK 2 — FEATURE / NIST CORPUS DRIVE (`NistDifferentialTests` is the net; add each newly-green program's
> `[InlineData]`):** Snapshot after 520: **89/95 NC compile; 15 byte-match the golden** (513–514 found free wins + the
> PERFORM-THRU-range control fix; 516 completed the de-sign rule). **The closest single green is NC116A — ONE failure
> left (diff 10), GF-17 "PRECEDENCE OF SUBORDINATE SIGN CLAUSE" (ISO §13.18.45):** a subordinate group's `SIGN LEADING
> SEPARATE` must OVERRIDE an ancestor's `SIGN TRAILING` for the items below it (nearest-ancestor wins), and a SIGN
> SEPARATE item stores its sign in its own leading/trailing CHARACTER position so a REDEFINES over it reads the `+`/`-`.
> The fix = **inherit the nearest-ancestor SIGN clause** in `DataBinder` (it currently reads only the item's OWN
> `signClause`, line ~326): capture the group-level SIGN on every `DataItem`, then before the REDEFINES classification
> pass re-derive each signed-numeric item's SignKind (and the SEPARATE +1 image width, which feeds the redefines class
> width — mind the pass ORDERING). The separate-sign IMAGE storage already works (`S9(5) SIGN LEADING SEPARATE` →
> image `+91275`); only the inheritance is missing. **Other closest mismatches:** NC219A (diff 16 — the PROGRAM
> COLLATING SEQUENCE subsystem: `ALPHABET … ALSO … ALSO`, custom ordinals, redefining figurative LOW/HIGH-VALUE,
> applied to alphanumeric comparison — a sizable but single-root-cause feature that greens NC219A); NC114M (diff 34 —
> PICTURE insertion editing `B`/`0`/`/` in alphanumeric-/numeric-edited, plus the de-sign already done for MOVE-TEST-16);
> NC171A (diff 34 — DIVIDE INTO needs **Int128 intermediate arithmetic**: `Divide` scales the dividend by 10^9 and
> overflows `long` for 18-significant-digit operands → wrong result + spurious size error; this is the deferred G3
> Int128 work); NC124A (diff 850). Plus **67 RUNERR** (a runtime loud guard for an unimplemented verb — the INSPECT /
> EVALUATE / SEARCH / STRING / UNSTRING / INITIALIZE / ACCEPT backlog) and **6 CMPL_FAIL** (backend C# type-mismatch:
> NC104A/107A/108M/222A/247A/252A, e.g. NC252A's numeric Tier-B-view `string == long`). Recommended order: **(1) TARGET THE NEXT NC PROGRAM** —
> pick one (or two) compile-but-mismatch NC programs (run the compile/run-vs-golden sweep to pick the closest), implement
> the UNION of verbs/features they need, finish at a NEW GREEN program. NC211A's stack is DONE (506 level-88 over a
> REDEFINES view / group; 507 abbreviated combined relation conditions §8.8.4.12; 508 whole-group image over OCCURS
> §14.9; 509 signed→alphanumeric de-sign §14.9.25.4 GR6a; 510 `ALL "literal"` §8.3.3.6.4; 511 IS NUMERIC rule 2
> §8.8.4.4). **Still-open known gaps (each a likely next-program blocker):** the genuine Tier-C mixed-USAGE group
> (`byte[]`+`RedefCodec`, COBOLNET_DESIGN §4.2 — a COMP/binary leaf moved as a whole, currently loud); NEXT SENTENCE
> (loud); NC252A's numeric Tier-B-view 88 (`string == long` today) + nested-Tier-B-through-suppressed-parent
> [`REDEF11`/`RDF3`] + level-66 RENAMES. **(2) Then the high-frequency string/table VERBS** (the ~60 compile-but-mismatch
> programs each hit an
> unimplemented verb's loud guard): **INSPECT** (TALLYING/REPLACING/CONVERTING), **EVALUATE**, **SEARCH**/SET-for-index,
> **STRING**/**UNSTRING**, **INITIALIZE**, **ACCEPT** — each via `CobolStrings`/the bound tree + a differential test.
> ⚠ **A single verb greens NO program (each MISS needs several) — so TARGET A PROGRAM: pick one (or two) NC programs,
> implement the UNION of verbs they need, and finish at a NEW GREEN program** (a real integration milestone), not a
> stack of individually-tested verbs. **(3) Then G5 relative+indexed files** (`FileConnector` for SQ/RL/IX) + SORT/MERGE,
> and **CobolEdit** numeric-edited (needed once a FAIL path prints COMPUTED=). **Pattern that works:** map via the
> compile/run sweeps (compile-only first — a few NIST programs hang at *runtime*: use `timeout -k`), implement to the
> spec + design, differential-test, guard-green, commit, tick. **Known-latent (Int128/G3):** intermediate overflow
> beyond the long range / additive-scaling overflow / COMP-5 width bounds not size-error-checked; no-phrase EC-SIZE-fatal
> awaits the EC model. **OPTIONS clauses parsed but not yet applied:** ARITHMETIC mode, ENTRY-CONVENTION,
> FLOAT-BINARY/DECIMAL, INTERMEDIATE ROUNDING, INITIALIZE. Earlier history since 488:
> (489) deep-dive doc-sync to SSOT §14; (490) **whole-group MOVE/DISPLAY/compare over numeric-DISPLAY leaves** — a
> numeric-DISPLAY leaf in a whole-group-referenced group stores its CHARACTER IMAGE (`DataItem.StoreAsImage`, no
> byte[]; ISO §14.9 MOVE GR4 — line 28901: group move = char copy, no conversion); (491–493) **REDEFINES/RENAMES the
> 4-tier one-canonical-backing model** — classification pass (anchor closure SR7/11, tier cascade D>C>B>A, class-max
> width SR8, view suppression SR9) + **Tier A** (same-storage alias: numeric-over-numeric shares one `long`, views
> forward) + **Tier B** (string-canonical: `RedefViewPlace` `(offset,width)` window over ONE backing, numeric views
> ride `StoreAsImage`; root-level AND in-group via `FieldEmitter.PhysicalFields`); (494) ADD…TO…GIVING includes the
> TO operand. **`CobolNum.ParseDisplay`/`FormatDisplaySigned` confirmed implemented** (the design's "overpunch
> deferred" note is discharged). **(The RESUME AT that was here — (1) ROUNDED — is DONE; the current RESUME AT is in
> the STATE banner at the top: (1) ON SIZE ERROR → (2) G5 file I/O.)** **REDEFINES follow-ups (off NC101A's path, the
> design's later commits):** RENAMES no-THRU forward + THRU composition (`RenamesSpanPlace`, binder already binds
> level-66 + resolves FROM/THRU); Tier C (a class-scoped `byte[]` + `RedefCodec` for genuine mixed-USAGE COMP puns —
> currently loud-rejected, conformant interim); explicit Tier-D reject reasons. The decision-complete REDEFINES plan
> is in the DEVLOG-493 workflow output. Also pending: `CobolInt`/Int128 (>18 digits), INSPECT/STRING/UNSTRING
> (`CobolStrings`), `CobolEdit` numeric-edited. Known-latent: MOVE-signed→alphanumeric de-signing (§14.9.24 GR4d);
> EXIT SECTION / NEXT SENTENCE / ALTER / GO-TO-out-of-inline-PERFORM (loud). The (now-outdated) STATE blocks below are
> earlier snapshots, kept for architecture detail.
>
> ## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — repeatedly corrected this session; obey these BEFORE writing any code)
> These govern HOW you work and are the #1 way to go wrong if ignored. Durable copies:
> `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`, `feedback_spec_scopes_not_tests`.
> 1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines the correct behavior for EVERY case.** Whenever any
>    question of semantics / syntax / output / edge-case arises, READ the spec and CITE the § in code+DEVLOG — never
>    guess, never infer behavior from the legacy oracle (it is a regression net, NOT authority; it has known
>    non-conformances, e.g. the DISPLAY trailing-trim).
> 2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists all of
>    them — pipeline/data-model/redefines/control-flow/numeric/strings/files/interprogram/OO/conditions/intrinsics/
>    project-org). Read the doc, FOLLOW it; do NOT improvise an approach. The deep-dives are decision-complete SSOTs;
>    the SSOT `COBOLNET_DESIGN.md` wins for locked invariants (§1), cross-cutting (§14), settled decisions (§18),
>    build order (§16).
> 3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references.** The NIST /
>    differential corpus VERIFIES correctness; it does NOT bound what to build. Do not grep a test (e.g. "which
>    REDEFINES views does NC101A use") to decide scope — read the spec section + the deep-dive and implement the whole
>    feature (e.g. REDEFINES = all 4 tiers + RENAMES + every SR/GR rule). Legitimate STAGING is by spec/design
>    structure (e.g. the design's own G/commit order), never by test coverage.
> 4. **Keep the deep-dive docs CURRENT.** Whenever the SSOT (or a new decision/finding) supersedes a deep-dive's
>    design, UPDATE that deep-dive in the SAME change set — state the current design AND why the original was not
>    followed (cite the SSOT §/DEVLOG). A reader following a stale deep-dive would implement the rejected approach.
>
> **Standing operating rules (already in memory — still in force):** guard-green (`scripts/guard-fast.sh`, or the
> CobolNet differential+unit suites for greenfield work) before EVERY commit; a `tests/...` conformance/differential
> test + a DEVLOG entry ship in the SAME commit as each feature (**`DEVLOG.md` is DESCENDING — add the new entry at the
> TOP, just under the preamble's `Ordering: DESCENDING` note, with a real date+time header stamp
> `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` from `date "+%Y-%m-%d %H:%M %Z"`; the latest entry is always there,
> never hunt for it**); commit AND push every checkpoint (never ask "should
> I continue/push"); run autonomously and continue immediately when work is pending (don't stop to ask, don't
> ScheduleWakeup to wait); no byte `ProgramState` substrate (typed-native; a `byte[]` only at a genuine REDEFINES
> Tier-C / file boundary); adversarially-review non-trivial features. (Memories: `feedback_fully_autonomous_push`,
> `feedback_continue_dont_wait`, `feedback_conformance_tests_per_feature`, `feedback_devlog_per_commit`,
> `feedback_complete_dotnet_migration_no_byte`, `feedback_commercial_quality_north_star`.)
>
> **(superseded) STATE (DEVLOG 485):** G1 ✅, G0 ✅, **G2 FOUNDATION ✅, G3-core (partial) ✅, G4 ✅.** Differential harness LIVE,
> **116 G2/G3/G4-scope tests green.** G3-core landed: **figurative-constant operands** (MOVE/DISPLAY/comparison ZERO/
> SPACE…) + **compiler crash-proofing** (a group-in-numeric-context NPE → loud-failure; §1.4). **G4 = the PC
> dispatcher is DONE** (`__Dispatch(start,exit)` with paragraphs as pc cases; GO TO / GO TO DEPENDING / fall-through /
> out-of-line PERFORM [THRU/TIMES/UNTIL] as recursive bounded dispatch / EXIT PARAGRAPH; dead-code suppression;
> dispatcher internals `__`-prefixed so they never collide with COBOL fields). **The out-of-line-PERFORM
> double-execution known-latent is FIXED.** Empirical scope (the advisor's "pull real NC programs in"): **NC101A now
> compiles with only 3 unsupported statements** (OPEN/CLOSE/WRITE = file I/O, G5) + 106 whole-group MOVE (G6) + 2 group
> DISPLAY (G6) — G4 cleared all ~88 GO TO/EXIT. **RESUME AT → G5 (file I/O: `FileConnector`/`IRecordCodec`/FILE STATUS/
> OPEN-CLOSE-READ-WRITE-REWRITE/the file state machines, COBOLNET_DESIGN §8) + G6 (whole-group MOVE/compare/DISPLAY via
> the generated `AsImage()`/`FromImage()` per record struct, §14.4)** — together these unblock the FIRST full NC
> program through the differential harness. Known-latent now: MOVE-signed→alphanumeric de-signing (ISO §14.9.24 GR4d);
> >18-digit numerics (Int128 deferred); EXIT SECTION / NEXT SENTENCE / ALTER / GO-TO-out-of-inline-PERFORM (loud).
> Class conditions (IS NUMERIC/ALPHABETIC/-UPPER/-LOWER) DONE (DEVLOG 486, `CobolClass`); the one remaining small G2
> tail is ref-mod `(s:l)` (needs `CobolString.RefMod`/`SpliceInto`; the binder already detects the depth-0 SUB_COLON).
> The lines below are the earlier (DEVLOG 483) snapshot, kept for the architecture detail.
>
> **STATE (DEVLOG 483):** G1 ✅, G0 ✅. **G2 FOUNDATION COMPLETE (DEVLOG 475–483):** the parse-tree-walk emitter the
> DESIGN superseded is RETIRED and replaced by the real **bound semantic tree** (`Binding/Bound/` — `BoundProgram` +
> `StatementBinder` + bound nodes + `Bound*Error` loud-failure) rendered by a §17 §2.2-decomposed backend
> (`CodeGen/Emit/` — `EmissionContext` + `NumericRenderer` / `ConditionRenderer` / `OperandText` / `FieldEmitter` +
> a 239-line orchestrator; no god class). The **data model** is typed-native: groups→`record struct`,
> OCCURS→`T[]` + subscripts (ported SUB_* split), the **`Place`**/`ReferenceResolver` lvalue (unqualified +
> OF/IN-qualified), figurative VALUE, signed-DISPLAY over-punch/separate/binary-minus (`NumProfile.SignKind`),
> level-88 + sign conditions, INDEXED BY fields, loud-failure (§1.4 `NotImplemented`). **Verification backbone:** the
> **differential harness is LIVE** (`tests/Cobol.Net.Tests.Conformance/` — `LegacyCompiler` oracle vs
> `CobolNetCompiler`, compared on the NIST acceptance basis; **93 hand-picked G2-scope tests green**). ⚠ ISO finding
> (memory `feedback_use_the_spec`): legacy DISPLAY trims trailing spaces of an alphanumeric operand — **non-conforming
> per ISO §14.9.11.4 GR6**; COBOL.NET emits the full field (spec-pinned where the quirk shows). Numerics are still
> `long`-only (Int128/`CobolInt` deferred to the wave that first needs >18 digits per §18 #4). **RESUME AT →** (a)
> small G2 tails: **ref-mod** `(s:l)` (G2-1c — needs `CobolString.RefMod`/`SpliceInto` runtime; binder already detects
> the depth-0 `SUB_COLON`) + **class conditions** (IS NUMERIC/ALPHABETIC); then **G3** (the `CobolInt`/`Int128`
> value engine + `TryStore` + ROUNDED + ON SIZE ERROR + INSPECT/STRING/UNSTRING + `CobolEdit` numeric-edited) and
> **G4** (the PC dispatcher — replaces the sequential-paragraph stopgap; out-of-line PERFORM + fall-through is
> double-executing today) → **G5** drive the 364-NIST corpus via the differential harness. Both G3 and G4 now land
> against the bound tree, written once. Reuse ONLY the front-end + the clean typed substrates.
>
> ⚠ **VERIFICATION-CONFIDENCE (advisor, DEVLOG 483):** the 93 green tests are **hand-picked G2-scope fragments** —
> "the slice I chose works," NOT "G2 is correct." By construction the net dodges the known-wrong spots. The REAL
> verification is the 364-program NIST corpus via the differential harness. **Highest-leverage next move = G3-core +
> G4 so the FIRST real NC program runs through the harness** (the frontend already does NIST X-card preprocessing via
> `CompilerDriver.NistTestName`); the moment G4 lands, pull a handful of real NC programs in rather than waiting for
> all of G5 — that's what converts the net from "what I thought to test" into "what the corpus exercises." Do the
> small G2 tails (ref-mod, class conditions) opportunistically, not as the focus. **KNOWN LATENT (a real NC program
> will trip these):** (1) out-of-line PERFORM + fall-through **double-executes** (the sequential-paragraph stopgap —
> G4 fixes it); (2) **MOVE of a signed numeric → alphanumeric** moves the sign-aware image, but ISO §14.9.24 GR4d
> wants the **de-signed** digits (`OperandText.FieldAsString` is shared by DISPLAY [correct] and the MOVE source
> path [wrong for signed]); (3) numerics >18 digits (Int128 deferred, §18 #4).
>
>
>
> *(The historical byte-engine kickoff §0–§9 was removed 2026-06-09 — it is obsolete and superseded by the
> greenfield pivot; the live plan is THIS file + `docs/COBOLNET_DESIGN.md`. See DEVLOG 521.)*
