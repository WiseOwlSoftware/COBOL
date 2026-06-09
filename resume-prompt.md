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
> **STATE (DEVLOG 488):** G1 ✅, G0 ✅, **G2 FOUNDATION ✅ (+ ALL small tails), G3-core (partial) ✅, G4 ✅, G6-core
> ✅.** Differential harness LIVE, **148 tests green.** Since 485: **class conditions** (IS NUMERIC/ALPHABETIC/-UPPER/
> -LOWER, `CobolClass`), **ref-mod** `(s:l)` read+write (`CobolString.RefMod`/`SpliceInto` + `RefModPlace`), and
> **G6-core whole-group MOVE/DISPLAY/compare** for DISPLAY-homogeneous groups (generated `AsImage()`/`FromImage()` per
> all-character `record struct`, §14.4). **NC101A loud guards now 70** (was 95 statements pre-G4 → 70): just **66
> mixed-usage group MOVEs (Tier-C byte island)** + **3 file I/O** + a couple misc. **RESUME AT → G5 (file I/O,
> COBOLNET_DESIGN §8: `FileConnector`/`IRecordCodec`/`CobolKey`/FILE STATUS/OPEN-CLOSE-READ-WRITE-REWRITE-DELETE-START/
> the open-mode + position state machines / SORT-MERGE) + the Tier-C mixed-usage-group byte path (§4.2/§14.4: a
> class-scoped `byte[]` for a group with numeric/COMP leaves)** — together these unblock the FIRST full NC program
> through the differential harness; then drive the 364-NIST corpus. Also pending: the G3 numeric hardening
> (`CobolInt`/Int128 value engine + `TryStore` + ROUNDED + ON SIZE ERROR), INSPECT/STRING/UNSTRING (`CobolStrings`),
> `CobolEdit` numeric-edited. Known-latent: MOVE-signed→alphanumeric de-signing (ISO §14.9.24 GR4d); >18-digit
> numerics; EXIT SECTION / NEXT SENTENCE / ALTER / GO-TO-out-of-inline-PERFORM (loud). The (now-outdated) STATE
> blocks below are earlier snapshots, kept for architecture detail.
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
> *Everything below is the HISTORICAL byte-engine kickoff, retained for reference on the COBOL feature surface +
> conformance corpus (still the oracle) — but the architecture/backend/data-model in it is SUPERSEDED.*

---

*Drive ISO/IEC 1989:2023 implementation **and** validation to completion — one large, autonomous, maximally-parallel
session. (This file IS the prompt; updated 2026-06-07, DEVLOG 443.)*

---

## 0. Your mission this session

You are continuing a multi-session build of a **commercial-quality, production-level, decades-sustainable, full
ISO/IEC 1989:2023 COBOL compiler** — `CobolSharp` (to be renamed **COBOL.NET**, exe `cobol.exe`), written in **C# 14 on
.NET 10**, compiling COBOL → **.NET CIL via Mono.Cecil**.

**Continue the COBOL-2002 → 2014 → 2023 implementation AND its conformance validation from the current state through to
completion, in this one (likely large) autonomous session, with maximum practical parallelism.** There is **no
backward-compatibility constraint** — break, rewrite, and re-architect anything when the cleanest long-term design
demands it (quality + longevity beat minimal blast radius, always). Run **continuously**; with pending work, continue
immediately — never end a turn to "wait." Only stop for a genuine **owner-only** decision (and prefer a sensible
default + proceed).

## 1. Read first (in order)

1. **`docs/MASTER_PLAN.md`** — THE top-level SSOT + execution playbook (North Star, phased roadmap A–F, parallelism
   map §4, grounded assessment §10, early-resolve decisions §11).
2. **`docs/DOC_INDEX.md`** — the map of all 126 docs (subject · type · maintenance guide). Use it to find the right
   reference fast, and keep it in sync.
3. **`docs/ISO2023_CONFORMANCE_PLAN.md`** — THE conformance work-breakdown (ranked M2/M3/M4 + execution waves). **This
   is your worklist. Execute it; do NOT re-run the gap analysis. Tick items in §3 and update §1 status as work lands.**
4. **`docs/DATA_MODEL_ARCHITECTURE.md`** (the typed-native ADR — settled) + **`docs/RECORD_STRUCT_STORAGE_DESIGN.md`**
   (the 7-stage migration roadmap).
5. **`docs/OO_IMPLEMENTATION_DESIGN.md` §6.6** — the turnkey OO semantic/emit plan (your first big feature, B1).
6. **`PROMPT.md`** — non-negotiable architectural doctrine + the anti-pattern catalog.
7. Recent **`DEVLOG.md`** (entries 439–443) — exactly what just landed and why.

## 2. Current state (honest baseline — 2026-06-08, DEVLOG 456; latest commit `a10bda9`, branch `main`, tree clean)

> ⛔ **OWNER DIRECTIVE (2026-06-08, emphatic): OO is built TYPED-NATIVE per ADR §7 — object data → per-instance typed
> .NET fields, methods → real .NET methods, `INVOKE` → `callvirt`. Do NOT bolt OO onto the byte `ProgramState` image.**
> The `EnableTypedFields` default-OFF gate is **migration-safety for the legacy corpus**, NOT design intent — OO is a
> new subsystem with no legacy corpus, so it is an **always-on consumer of the typed pipeline**. "We long ago said
> give up the byte substrate and migrate completely to .NET data types everywhere possible." Tests may break
> **temporarily** mid-migration; the bar is ALL green at completion. (Memory `feedback_oo_typed_native_not_byte`.)

- **Stack:** **.NET 10 / C# 14**. **Backend: CIL-only via Mono.Cecil** (a Roslyn C# backend is a FUTURE additive
  Stage-5 option — Cecil stays the shipping backend + the differential oracle; deferred beyond OO). **Pointers = ONE
  `ManagedPointer`** (GC-tracked; no native heap / no `unsafe` / no 8-byte handle / no `PointerRegistry` — settled).
- **Guard ALL GREEN: 1204 unit / 535 integration / 364 NIST** (DEVLOG 456). Iterate with
  **`bash scripts/guard-fast.sh`** (~3.3 min, parallel, **proven byte-identical** to the serial `scripts/guard.sh`
  via `scripts/guard-verify.sh`).
- **Phase A DONE** (DEVLOG 445–446): CLI `--standard` verified to reach the parser `DialectLevel` + a regression net;
  CI enabled (`.github/workflows/build-and-test.yml` gates the full guard on push/PR); parallel-dev = patch-integrate.
- **M1 (COBOL-85) COMPLETE** — NIST CCVS85 (364 baselines) is the '85 validation backbone.
- **Data-model migration CORE done through Stage-4** (for PROGRAMS, gated behind `EnableTypedFields`, **default OFF**
  → corpus byte-identical; each flip pinned by a flag-on≡flag-off differential test): character→`string`,
  numeric→`long`/`decimal`, flat+nested groups→`record struct`, fixed OCCURS→`T[]`, pointers→`ManagedPointer`. Every
  byte-trigger (REDEFINES/RENAMES/edited/file/EXTERNAL/LINKAGE/ref-mod/ODO) correctly stays byte. The global flip-on
  (EnableTypedFields default-ON) + island-the-byte-engine is Stage-6/B3 (still future).
- **OO TYPED-NATIVE — object data model COMPLETE + multi-method + INVOKE SELF (DEVLOG 453–456; THIS session):**
  - **Object data → per-instance typed .NET fields (453 char, 454 numeric, 456 groups+OCCURS):** `PIC X`→instance
    `string`, `PIC 9`→instance `long`/`decimal`, `01` group→instance `record struct`, `OCCURS`→instance typed array.
    The byte `State` is **empty** on a fully-typed class. `CollectTypedFields` is **always-on for a CLASS**; the typed
    field defs/init/access gained a per-instance dimension on the `StateIsInstance` flag via one chokepoint
    `CilDataEmitter.EmitTypedFieldOwner` (typed analogue of the byte engine's `EmitLoadBackingArray`). Also filled the
    general **typed-numeric→byte MOVE cell** (`EmitMoveWithStandardSignature` materializes a typed source).
  - **Multi-method classes + INVOKE SELF (455):** a `CLASS-ID` hosts N `METHOD-ID` units, each its own public .NET
    method with an **exit-bounded** dispatch range (method A can't fall into B); per-method paragraph scope (fixes
    CBL3104) threaded through SemanticBuilder/ReferenceResolver/BoundTreeBuilder/ProcedureNameResolver/Binder;
    `IrModule.ClassMethods` roster; `INVOKE SELF` → `callvirt` on `this` (COBOL0112 lifted), incl. **polymorphic SELF**
    across INHERITS (verified `oo_self_polymorphic`).
  - **Adversarially reviewed (3-agent panel, DEVLOG 455):** confirmed core correct; edge gaps now fail LOUD —
    **COBOL0116** (SECTION inside a METHOD — was a silent skip) and **COBOL0117** (multi-method class WITH method
    params — `FindLinkageField` is module-level → would cross-wire LINKAGE; per-method LINKAGE is a later slice).
  - Earlier OO (DEVLOG 447–451, now on the typed path): NEW, INVOKE USING/RETURNING, OBJECT REFERENCE, INHERITS +
    virtual dispatch, INVOKE SUPER. Deferred loud: COBOL0111 arg-forms, 0113 subclass-data, 0114 unknown-base,
    0115 root-SUPER, 0116/0117 above.
  - Conformance (9): `oo_hello`/`oo_method_perform`/`oo_method_args`/`oo_inherit`/`oo_super`/`oo_instance_data`/
    `oo_self`/`oo_self_polymorphic`/`oo_object_group` + `OoTests`.
  - **NEXT OO (in order):** (a) **subclass own typed OBJECT data** (lift COBOL0113 — emit the subclass's own typed
    instance fields + init in the subclass ctor; needs factoring the typed field-defs + typed-init out of
    `CilEmitter.EmitProgramState`, and detecting/rejecting byte-island subclass data which has no own State);
    (b) **per-method LINKAGE** (multi-method-with-args — per-method LINKAGE layout + `FindLinkageField`, lifts
    COBOL0117) + typed-field-by-reference INVOKE args (COBOL0600 today — no clean lowering/emit diagnostic path yet);
    (c) **FACTORY** (static) methods + `INVOKE Class "M"`; then 5 PROPERTY / 6 universal-ref+EC
    (`docs/OO_IMPLEMENTATION_DESIGN.md` §5). Then the rest of M2 → M3 → M4 (`docs/ISO2023_CONFORMANCE_PLAN.md` §3).
- **Architecture refactors already DONE** (do not treat as god classes): **M001** (IR-expression hierarchy),
  **M003** (`CilEmitter` → 11 focused emitters + `EmissionContext`), **M004** (`BoundTreeBuilder` → 9 focused binders),
  9 lowering classes.
- **M2 already partly landed** (see ISO2023_CONFORMANCE_PLAN): free-form source + `>>` directives + conditional
  compilation, UDF, NATIONAL (UTF-16), BOOLEAN/BIT, POINTERS, FLOAT-*/BINARY-*, INSPECT BACKWARD, ROUNDED MODE,
  `GOBACK RETURNING`, etc. — each conformance-tested under `tests/conformance/2002/`.
- **Docs** consolidated to 126 (one canonical per subsystem) + `DOC_INDEX.md`; design-references carry status banners.

## 3. The goal and the work sequence

**Goal:** every in-scope ISO-2023 feature **implemented + conformance-tested**; `cobol --standard cobol2023 prog.cob`
compiles to a working .NET assembly; the guard is all-green including the full M2/M3/M4 conformance corpus;
`ISO2023_CONFORMANCE_PLAN` §3 fully ticked.

Work in dependency order (high-level in MASTER_PLAN §3; exact items in ISO2023_CONFORMANCE_PLAN §3):

**Phase A — ENABLE MAXIMUM PARALLELISM (do this FIRST; ~hours).** The throughput lever for everything after.
- Stand up a reliable **parallel-dev harness**. In this repo `Workflow isolation:'worktree'` branches from a **STALE**
  commit (memory `feedback_worktree_workflows_stale`) — do **not** rely on it. **Default:** a *"agents implement on
  isolated copies and return patches; the main loop integrates them onto `main` one-at-a-time, guard-gated"* pattern.
  Prove it by building 2 independent features concurrently and integrating both guard-green.
- Fix the **CLI dialect bug**: `--standard cobol2002|2014|2023` may not reach the parser `DialectLevel` (only the
  conformance-harness path is known-good). Verify and fix so CLI users get the right feature set; add a regression test.
- **Enable CI**: flesh out `.github/workflows/build-and-test.yml.disabled` (already bumped to SDK `10.0.x`) to gate the
  guard on every push.

**Phase B — FINISH THE DATA-MODEL MIGRATION (mostly sequential — shared core).**
- **B1 — OO semantic/emit** (turnkey `OO_IMPLEMENTATION_DESIGN.md` §6.6; this is BOTH the largest M2 feature AND the
  Stage-4 OO step): route `classDefinition` through the existing semantic/binder (reuses the whole layer — no new
  `ClassSymbol` machinery); emit the class with **per-instance `ProgramState`** (one chokepoint:
  `EmissionContext.StateIsInstance`, ~`CilLocationEmitter:476` — verify) + a constructor (NEW) + instance methods;
  **bind + emit `INVOKE`** (`newobj`/`callvirt`, cross-type resolution via a two-pass / assembly-wide type+method
  registry); `OBJECT REFERENCE` storage. Ship a `tests/conformance/2002/` OO test in the same commit. Then OO slices
  2–6 (USING/RETURNING; INHERITS + SELF/SUPER; FACTORY; PROPERTY; polymorphism + EC-for-OO) — each guard-green +
  conformance-tested, parallelizable where independent.
- **B2 — Roslyn C# backend:** DEFERRED (owner: proceed later; Cecil stays shipping + oracle). Not required for
  conformance — skip unless conformance is done and time remains.
- **B3 — finalize:** once the typed flips are complete, decide/flip `EnableTypedFields` ON by default, island the byte
  engine (`RECORD_STRUCT_STORAGE_DESIGN.md` §1.6), prove the whole corpus stays correct, remove dead gated-OFF paths.
  (Can follow Phase C.)

**Phase C — FULL ISO-2023 CONFORMANCE (the bulk; MAX PARALLELISM).** Execute `ISO2023_CONFORMANCE_PLAN` §3. Independent
features (no shared-core conflict) fan out via the Phase-A harness, each integrated guard-green + a
`tests/conformance/<ver>/` test. Recommended order (value/tractability):
- **M2 (2002):** OO (B1) → **FILE-2002** (FILE STATUS as a variable, OPTIONAL, file sharing) → **arithmetic OPTIONS**
  (intermediate rounding / scale-preservation) → remaining **preprocessor** (PRE-1) → **UDF** completion →
  **VALIDATE** statement (+ validation clauses) → **EC / exception model** (the `EC-*` category codification, ON
  EXCEPTION, routing to declaratives/USE) → **SCREEN SECTION** (the 12-doc terminal design set — one owner-gated Q on
  form-level ACCEPT semantics: defer Screen to late Phase C and ask then) → **JSON/XML PARSE/GENERATE lowering +
  runtime** (the grammar overlay exists; emit + runtime are design-only).
- **M3 (2014):** dynamic-capacity tables (OCCURS DYNAMIC), TYPEDEF / user-defined types, file sharing & locking,
  Report-Writer-as-standard, function-pointers, misc.
- **M4 (2023):** new intrinsics, enhanced bit/boolean data, the remaining 2023 deltas; complete per-version gating.
Every post-'85 feature: **spec-cited** (`specs/ISO_COBOL.md`), **version-factored** grammar, **conformance-tested in
the same commit**, **adversarially reviewed**.

**Phase D (interleave):** targeted architecture cleanup — excise the dead overlay grammars
(`CobolParserOO/JsonXml/Generics/Dialect.g4`) + `RecordLayoutBuilder` vestiges; finish the single-PIC-semantics
pipeline. (The big "product surface" — debugger/LSP/packaging/security/perf — is Phase E, out of scope for
*conformance* completion; note gaps but don't block on them.)

**Phase F — rename** `CobolSharp` → **COBOL.NET** (exe `cobol.exe`) only after full conformance + a quality gate.

## 4. Operating rules (non-negotiable)

- **The guard is law.** `guard-fast.sh` GREEN before every commit; nothing lands red. After any change to the test
  corpus or the guard's grouping, re-prove equivalence with `guard-verify.sh`.
- **A conformance test ships with every post-'85 feature, in the same commit** — `tests/conformance/<2002|2014|2023>/
  <name>.cob` + `.out` (auto-discovered by `ConformanceTests`, run inside the guard). This is the post-'85
  NIST-equivalent (memory `feedback_conformance_tests_per_feature`).
- **A DEVLOG entry per commit** (memory `feedback_devlog_per_commit`); tick `ISO2023_CONFORMANCE_PLAN` §3 + update its
  §1 status/NEXT-UP. **Do NOT create new resume/handoff/plan docs — update the existing ones** (THIS file, MASTER_PLAN,
  the conformance plan, DEVLOG, DOC_INDEX).
- **Grammar discipline:** version-factor each post-85 feature into a `Core/CobolXXXX.g4` fragment with a minimal
  `{isYYYY()}?`-gated hook in the core rule; add rules **INCREMENTALLY, guard-fast after EACH**; never inline a
  post-85 feature into the COBOL-85 base (memory `feedback_grammar_version_factoring`; inlining all of OO at once once
  regressed ~9 NC tests). A clean ANTLR regen needs `rm -rf src/CobolSharp.Compiler/Generated` (the timestamp check can
  ship a corrupt partial generate). `is2002()/is2014()/is2023()` = `DialectLevel >= N` in `Parsing/CobolParserCoreBase.cs`.
- **Adversarially verify** every non-trivial feature — a 2–3-agent skeptic panel catches the silent-corruption a
  conformance test misses (the dispatch-site / `IsGroup`/`Category` mis-classification class of bug).
- **Doc hygiene:** one canonical per subsystem (see `DOC_INDEX.md`); when you change a subsystem, update its
  design-reference + its `DOC_INDEX` row + keep its status banner accurate; a NEW subsystem ⇒ a new doc (with a status
  banner) + a new index row. Reference docs **stand on their own** — no provenance / dead-end / deleted-doc mentions.
- **No stale facts:** `.NET 10` / `C# 14`, CIL-only via Mono.Cecil, one `ManagedPointer`. Never reintroduce `.NET 9` /
  `C# 13` / custom-VM / 8-byte pointer handle / `PointerRegistry` / `CobolDataPointer` / "CilEmitter god class".

## 5. Parallelism playbook (maximize it)

- **Always parallel (safe, read-only):** investigation/mapping, design panels, adversarial review — fan out via
  `Workflow`. No isolation needed.
- **Parallel feature implementation** (the big lever): once the Phase-A harness is up, build independent conformance
  features concurrently and integrate them onto `main` **one-at-a-time, guard-gated**.
- **Sequential spine on `main`:** anything touching the grammar, `CilEmitter`, `Binder`, `SemanticBuilder`, or the
  pipeline — serial, kept tight by the fast guard, made confident by parallel review.
- **Workflow dispatch lesson (memory `feedback_workflow_agent_dispatch`):** when fanning out N agents over a
  partitioned worklist, give **each agent its OWN input file** (e.g. `mc0.json … mcN.json`) — **never** "read element
  `[k]` of a shared array" (agents miscount → double-process / skip; this cost rework in DEVLOG 442). Write unicode
  slices via Python; **reconcile returned results against the worklist** before acting; prefer whole-file Writes over
  blind appends; agents editing **disjoint** files need no worktree.

## 6. Settled — do NOT re-litigate

- The typed-native data-model ADR (owner co-authored + adversarially reviewed).
- Pointers = `ManagedPointer`; `PointerRegistry` REJECTED; no 8-byte handle.
- Backend CIL-only via Mono.Cecil; Roslyn = future additive Stage-5 (Cecil = oracle), deferred beyond OO.
- **High-subset only** (NO `--subset` flag); CM / DB / SG modules are **parse + flag only** (no runtime).
- Target the **latest .NET / C#** (currently .NET 10 / C# 14; memory `feedback_target_latest_dotnet`).

## 7. Early-resolve decisions (settle in the first hour; defaults given — MASTER_PLAN §11)

1. **Parallel-dev harness** — *default:* the patch-integrate pattern (don't trust worktrees here).
2. **CI** — *default:* enable the existing template in Phase A.
3. **Screen form-level ACCEPT semantics** (one-field-at-a-time vs whole-form loop) — owner Q; *default:* defer Screen
   to late Phase C and ask then; everything else proceeds without it.
4. **Test-maturity targets** + fuzz oracle — *default:* use the existing internal differential oracles; GnuCOBOL
   optional.
5. **Parallel conformance-test authoring** — author in disjoint per-feature dirs, integrate one-at-a-time guard-gated.

## 8. Definition of done

- Every in-scope ISO-2023 (M2 + M3 + M4) feature **implemented + conformance-tested**; `ISO2023_CONFORMANCE_PLAN` §3
  fully ticked.
- `cobol --standard cobol2023 prog.cob` → a working .NET assembly; `--standard {cobol85|cobol2002|cobol2014|cobol2023}`
  selects the correct feature set + per-version diagnostics.
- **Guard ALL GREEN:** 1196+ unit / 509+ integration / 364 NIST **+ the complete M2/M3/M4 conformance corpus**.
- Typed-native flips complete + the `EnableTypedFields`-on decision made; byte engine islanded.
- **Honest scope note:** full ISO-2023 is large. **Drive to completion, but stay guard-green and resumable at EVERY
  commit** so no progress is ever lost. If you cannot finish, leave `ISO2023_CONFORMANCE_PLAN` §1 status + the latest
  DEVLOG entry pointing **exactly** at the next item to pick up.

## 9. Quick reference

- **Iterate:** `bash scripts/guard-fast.sh` (~3.3 min). **Authority:** `bash scripts/guard.sh`. **Equivalence proof:**
  `bash scripts/guard-verify.sh`.
- **Build:** `dotnet build CobolSharp.sln` — .NET 10 SDK is installed; `TreatWarningsAsErrors=true` (a new C# 14
  analyzer warning fails the build). **Clean ANTLR regen:** `rm -rf src/CobolSharp.Compiler/Generated`.
- **Tests:** post-'85 → `tests/conformance/<ver>/`; COBOL-85 → `tests/nist/`. Spec authority → `specs/ISO_COBOL.md`
  (submodule: `git submodule update --init --recursive`).
- **Branch:** `main` — this project commits work **directly to `main`, guard-gated** (the established convention;
  parallel features integrate one-at-a-time).
- **Pipeline:** Preprocess → ANTLR Lex/Parse → `SemanticBuilder` → `StorageLayoutComputer` → `Binder` (bound tree) →
  Lowering (IR) → `CilEmitter` (CIL via Mono.Cecil).
