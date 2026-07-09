# HANDOFF — Local-Model Work While Claude Is Paused (~3 days, resume Sunday)

> **Read this ENTIRE file before doing anything.** Claude (the primary engineer) is paused until its quota resets
> (Sunday). A local model (Ollama on a 5090) may make **low-risk, additive** progress on the COBOL.NET compiler
> **under the strict guardrails below**, on a **dedicated branch**, so Claude can review and resume cleanly. The
> architecture is **LOCKED**. You are a careful *executor*, not a designer.

---

## 0. THE ONE OVERRIDING RULE

**You execute precise recipes; you do NOT make design decisions and you do NOT improvise.**
The design is decided and frozen in `docs/COBOLNET_DESIGN.md` and `docs/rearchitecture/DESIGN-*.md`. You MAY read those.
You MUST NOT edit them, and you MUST NOT invent a new approach. **If a recipe does not work exactly as written: STOP,
run `git checkout -- .` to revert, and write what happened in `LOCAL-MODEL-JOURNAL.md`. Never patch your way around a
problem.** A reverted attempt with a good journal note is a SUCCESS. A creative fix that "seems to work" is a FAILURE
that will be discarded.

---

## 1. NON-NEGOTIABLE GUARDRAILS

1. **Work on a branch, never `main`.** First: `git checkout main && git pull && git checkout -b local-model-wip`
   (if it already exists, `git checkout local-model-wip`). **Never commit to `main`. Never push to `main`.** You may
   push the branch as a backup: `git push -u origin local-model-wip`. Claude reviews this branch on resume and merges
   only what is correct — so `main` cannot be corrupted.
2. **Battery-green-or-revert.** Before EVERY commit, run the full verification in §5. If ANY step is red, revert
   (`git checkout -- .`) and journal it. **NEVER commit a red battery.** No exceptions, no "I'll fix it next commit."
3. **One change per commit.** Small, single-purpose commits, each with a `LOCAL-MODEL-JOURNAL.md` entry (§6).
4. **DENYLIST — do NOT edit any of these (read-only):**
   - Any design/plan doc: `docs/COBOLNET_DESIGN.md`, `docs/COBOLNET_REARCHITECTURE_PLAN.md`,
     `docs/rearchitecture/DESIGN-*.md`, `docs/rearchitecture/PHASE-*.md`, `resume-prompt.md`, `CLAUDE.md`, `PROMPT.md`.
   - Any grammar file: `src/Cobol.Net.Frontend/Grammar/**/*.g4`.
   - The parse-layer gating recogniser: `src/Cobol.Net.Frontend/Parsing/ReservedWordEditionHints.cs`.
   - Any binder edition-gating logic (`ConstructRegistry.Check` call sites) in `src/Cobol.Net.Compiler/Binding/**`.
   - Anything in **§4 FORBIDDEN TASKS**.
5. **STOP-and-journal** the moment you are unsure, hit a red battery you can't fix by reverting, or feel tempted to
   touch a denylisted file. Do not guess.

---

## 2. CONTEXT TO READ FIRST (read-only)

- `resume-prompt.md` (top banner) — current state: the **version-conformance pipeline** (superset parse →
  edition-agnostic bind → one `VersionConformancePass` → emit-if-clean); **5 of 7 residue gates already migrated**.
- `docs/rearchitecture/DESIGN-version-conformance-pipeline.md` — the migration design (LOCKED). §4 = per-construct
  classification; §5 = the migration recipe.
- `DEVLOG.md` entries **709–713** — the 5 completed migrations (UNLOCK, PROPERTY, PD-RAISING, XOR, SHARING). **This is
  the exact PATTERN.** Do not deviate from it.
- `CLAUDE.md` + `PROMPT.md` — the project's non-negotiable process rules (they apply to you too).

Current battery at head of `main` (your green baseline): **greenfield conformance 3112 · unit 227 · characterization
32 · full legacy guard ALL GREEN.** Any task that lowers or reddens these is wrong — revert it.

---

## 3. ALLOWED TASKS (do them in this order; each is low-risk + additive)

### ✅ Task A — Prepare Batch C for Claude (READ-ONLY analysis; ZERO code risk; DO THIS FIRST, highest value)
The 2 remaining residue gates (**RETRY #4**, **boolean family #2**) are the hard ones; Claude will migrate them on
resume. Your job is to make that fast by doing the legwork. **Change NO code.** Create a NEW file
`docs/rearchitecture/LOCAL-MODEL-ANALYSIS-batchC.md` (this is a scratch analysis file you own — NOT a design doc) and
fill it with:
- **RETRY (#4):** grep and list every `retryPhrase` site in the grammar (`src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4`)
  and every place `RetryPhrase2002` / `BindRetry` appears in `src/Cobol.Net.Compiler/Binding/**`. Write, as PLAIN COBOL
  test snippets (do not compile-commit them — just author the text), the collision cases the design warns about:
  `OPEN INPUT RETRY FOREVER.` and `OPEN INPUT RETRY BALANCE SECONDS.` where `RETRY`/`FOREVER`/`BALANCE` are file names
  at COBOL-85, plus the real cases `READ F RETRY 3 TIMES` / `OPEN INPUT F RETRY 5 TIMES`. For each, note what the
  CORRECT parse should be. (Do NOT try to fix anything — just catalogue.)
- **Boolean family (#2):** list every `{is2002()}?` boolean site in `CobolExpressions.g4` (booleanExpression /
  booleanXorTerm / booleanAndTerm / booleanFactor + the `primaryCondition` ENTRY) and the `CobolParserCore.g4` COMPUTE
  F2 alt; list every `BooleanOperators2002` / `BindBoolExpr` / `boolExprAhead()` site in the binder. Author (as text)
  the test cases: `01 B-AND PIC 9. … IF B-AND = 5` (a plain comparison — must NOT be treated as boolean) vs
  `IF A B-AND B` / `COMPUTE C = A B-AND B` (real operators). Note that this touches the **shared comparison DFA**
  (DEVLOG 621) and is the highest-risk change in the project.
- End the file with an explicit note: "Prepared by the local model for Claude; analysis only, no code changed."
**This task alone is worth 3 days — it hands Claude a ready-to-execute Batch C.**

### ✅ Task B — Harden test coverage for the 5 MIGRATED constructs (low-risk, additive, battery-gated)
Only additive test fixtures; the battery catches any mistake. For each already-migrated construct, ensure a negative
fixture exists at EVERY edition below its introduction (UNLOCK/PROPERTY/PD-RAISING/SHARING introduced 2002 → reject at
85; XOR introduced 2023 → reject at 85, 2002, 2014). **Recipe (imitate exactly, per DEVLOG 709–713):**
1. Copy an existing fixture as a template, e.g. `tests/conformance/negative/xor_below_2023.cob` and its `.err`.
2. Create `tests/conformance/negative/<name>.cob` (with a `*> reject-at: <editions>` header naming the editions that
   must reject) and `<name>.err` (containing the single expected code, `COBOLNET0900`).
3. Add `"<name>"` to the `enabled` array in `tests/conformance/negative/manifest.json` (keep it valid JSON).
4. Run ONLY the conformance suite first (fast): `dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug`.
   It must PASS with a count exactly one higher than before. If it fails, the fixture is wrong — revert and journal.
5. Then run the FULL §5 verification before committing.
Do NOT change any `.cob` under `tests/conformance/2014/` or the golden `.out` files. Fixtures only.

### ✅ Task C — constructs.json citation cleanup (mechanical; do LAST, only if A and B are done)
Some migrated constructs' diagnostic text still says `{isXXXX()}?-gated in …`, which is stale (the predicate is gone).
This is a TEXT-ONLY fix in `tests/version-matrix/constructs.json`. **This regenerates code + runs a drift test**, so:
1. Edit ONLY the human-readable `notes`/citation string of a migrated construct's row (UNLOCK/PROPERTY/PD-RAISING/XOR/
   SHARING) — remove the `{isXXXX()}?`-predicate wording, leave the ISO §, the edition, and the id UNCHANGED.
2. Regenerate: `pwsh scripts/gen-constructs.ps1` (or the documented regen command).
3. Run the FULL §5 verification. The `ConstructRegistryDriftTests` must pass. If anything is red, revert and journal.
Change the DISPLAYED WORDING ONLY. Never change an `id`, `introducedIn`, `removedIn`, `diagnosticCode`, or add/remove a
row.

---

## 4. ⛔ FORBIDDEN TASKS (reserved for Claude — do NOT attempt, even if you think you can)

- **The RETRY migration (#4).** It needs a `retryPhraseAhead()` forward-lookahead helper, and `FOREVER` is both a
  retry keyword and a legal user word — the disambiguation needs human-level judgment. Prepare it (Task A); do not
  execute it.
- **The boolean-family migration (#2).** It touches the **shared comparison DFA** that has regressed subscript/ref-mod
  comparisons before (DEVLOG 621). Highest risk in the project. Prepare it (Task A); do not execute it.
- **The Stage-0 skeleton** (building the dedicated `VersionConformancePass`, making the binder edition-agnostic,
  splitting bind/emit) — a large architectural change.
- **Deleting `ReservedWordEditionHints.cs`** or removing any of its remaining arms.
- **ANY edit to a `.g4` grammar file, any binder `ConstructRegistry.Check` site, or any denylisted doc (§1.4).**
- Anything requiring a design decision, or anything not spelled out as an ALLOWED task above.

---

## 5. VERIFICATION PROTOCOL (run ALL of this, green, before EVERY commit)

Environment: Windows; `.NET 10`; run these from the repo root (a `bash` shell is available via Git Bash). A grammar
change would need `bash scripts/guard.sh` — but you are FORBIDDEN from changing grammar, so for the allowed tasks the
key gates are the greenfield suites + (for Task C) the drift test. Run **all** of the following and require green:

```
dotnet build CobolSharp.sln -c Debug                                                              # 0 errors
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug         # >= 3112 passed, 0 failed
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug                       # 227 passed, 0 failed
dotnet test tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj -c Debug   # 32 passed, 0 failed
bash scripts/guard.sh                                                                             # ends with "=== ALL GREEN ==="
```

If EVERY line is green, you may commit. If ANY line is red: `git checkout -- .`, then journal. (Task A changes no code,
so for Task A you only need the build to still be green — but run the suites anyway to be safe.)

---

## 6. JOURNAL PROTOCOL

After EVERY attempt — whether it committed or you reverted — append an entry to `LOCAL-MODEL-JOURNAL.md` (newest at the
bottom):

```
## <YYYY-MM-DD HH:MM> — Task <A/B/C> — <one-line what>
- Did: <exactly what you changed / analysed>
- Verify: <the §5 results — counts + ALL GREEN, or which line went red>
- Outcome: committed <sha>  |  REVERTED because <reason>  |  analysis-only (no code)
- Notes for Claude: <anything Claude should know / double-check>
```

This journal is how Claude reviews your 3 days of work. Be honest and specific — a "REVERTED because X" entry is
valuable, not a failure.

---

## 7. RESUME PROTOCOL (for Claude, on Sunday — you, the local model, do NOT do this)

1. `git checkout main && git pull`; read `LOCAL-MODEL-JOURNAL.md` and `git log --oneline main..local-model-wip`.
2. Treat `local-model-wip` as **UNTRUSTED** until reviewed. For each commit: diff it, check it against the design docs
   and the DEVLOG-709–713 pattern, and confirm it only did an ALLOWED task and left no denylisted file touched.
3. Cherry-pick / merge the correct commits to `main` (re-running the full battery + `scripts/guard.sh` on `main`
   after). **Discard** any commit that violated a guardrail or changed the design.
4. Read `docs/rearchitecture/LOCAL-MODEL-ANALYSIS-batchC.md` (if the local model wrote it) and use it to execute
   **Batch C** — RETRY #4 then the boolean family #2 — per `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`
   §4/§5, one construct per commit with the full guard. Then delete `ReservedWordEditionHints`, then the Stage-0
   skeleton.
5. Update `resume-prompt.md` + the plan banner to reflect the new state, and delete this handoff file + the journal +
   the local-model branch once their contents are absorbed.
