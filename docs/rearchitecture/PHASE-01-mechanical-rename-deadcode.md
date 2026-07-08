# PHASE 01 — Mechanical namespace rename + dead-grammar / JSON-XML removal

- **Phase:** P1
- **Track:** foundation
- **Risk:** MEDIUM (touches every project's `using` graph and the ANTLR regen path; behavior-neutral by construction except one narrowly-scoped catch change)
- **Depends on:** **P0** (migration safety net — the `Cobol.Net.Tests.Characterization` project + `Snapshots/` seeded from the pre-refactor emitter, `tests/nist/corpus.tsv`, and the cached Roslyn reference set). P1 uses P0's snapshots as the behavior-neutrality proof. **If P0 is NOT done**, P1 is still executable, but substitute the neutrality proof in §5 (git-diff of emitted `.g.cs` over a corpus) for the snapshot gate.
- **Related design (READ FIRST):**
  - `docs/rearchitecture/DESIGN-module-topology.md` — "Pull the `CobolSharp.Compiler.* → CobolNet.*` namespace rename FORWARD to Wave 0"; ANTLR package name single-sourced via an MSBuild property; delete 5 dead grammars + committed `.antlr` caches; strip non-ISO JSON/XML.
  - `docs/rearchitecture/DESIGN-frontend-grammar.md` — the frontend M-steps: generated-namespace rename via MSBuild property, delete the 5 unreferenced top-level grammars + `.antlr` caches, hard-delete JSON/XML rules and move `inlineMethodInvocationStatement` into `Core/CobolOO.g4`, fix the stale `Frontend.cs` banner, narrow the `catch(Exception)`.
  - `docs/COBOLNET_DESIGN.md` §1.4 (the "namespaces stay `CobolSharp.Compiler.*` until G8" banner this phase supersedes), §16 (G0–G8).
- **STATUS:** IN PROGRESS @ step 5 — steps 0–4 DONE. Step 4: generated ANTLR package → `CobolNet.Frontend.Generated`
  via a csproj `<AntlrNamespace>` single-source (threaded into the `Exec` + both `.ps1` `-PackageName` params);
  repo-wide flip of `CobolSharp.Compiler.Generated` (59 files incl. `CobolParserCoreBase.cs` decl + legacy consumers).
  ⚠ Beyond the recipe: two legacy files (`GenericClauseNode.cs`, `SemanticBuilder.cs`) referenced the parser via a BARE
  `Generated.` prefix (namespace-relative from `CobolSharp.Compiler.*`), which the fully-qualified sed missed — fixed
  with a `using Generated = CobolNet.Frontend.Generated;` alias each (consistent with §2.2's kept `Core` alias). Clean
  regen → parser declares `CobolNet.Frontend.Generated`; whole-sln build 0-err; conformance 2036/2036; grep-clean 0.
- **(history — steps 0–3)** Baseline verified green at P0 close-out (sln build 0-err · 2036
  conformance · 213 unit · 32 characterization · guard NIST 353 MATCH). Step 1: the 5 dead top-level grammars
  (`CobolParserJsonXml`/`Generics`/`OO`/`Dialect`/`Preprocessor`.g4) `git rm`'d — verified unreferenced (only an inert
  comment in `CobolExtensionsJsonXml.g4`, itself deleted at step 3); forced clean ANTLR regen + frontend build 0-err.
  Step 2: the 9 committed `.antlr` IDE caches (`Grammar/.antlr/` + `Grammar/Core/.antlr/`) `git rm -r`'d + `**/.antlr/`
  gitignored; 0 tracked `.antlr` remain; frontend build 0-err (caches are IDE-only). Step 3: `Core/CobolExtensionsJsonXml.g4`
  hard-deleted (JSON/XML non-ISO) — `jsonStatement`/`xmlStatement`/`jsonXmlExceptionPhrases` rules + the two `{is2014()}?`
  dispatch arms + the import removed; the live 2023 `inlineMethodInvocationStatement` relocated into `Core/CobolOO.g4`.
  ⚠ Required a fix beyond the plan: `EditionGateHints` dropped its `procedureDivision` rule-stack guard for the JSON/XML
  branch (the rule removal unwinds the stack before the error surfaces — see the step-3 correction note; `JSON`/`XML` are
  hard-reserved so the token alone is the signature). Battery: whole-sln build 0-err · conformance 2036/2036 · FULL
  legacy guard NIST 353 MATCH (ALL GREEN); `EditionGateDiagnosticTests` 13/13.
  *(The executing session updates this line: `NOT STARTED` → `IN PROGRESS @ step N` → `DONE`. Keep the battery green at every ★ COMMIT BOUNDARY.)*

---

## 1. Goal (one paragraph)

Pull the historical `CobolSharp.Compiler.* → CobolNet.*` namespace rename **forward** from the G8 big-bang to a mechanical, zero-behavior foundation step, and delete the dead / non-ISO grammar surface — so every later rearchitecture phase edits files already living under their final names, and G8 becomes a pure *deletion* of the legacy tree rather than a rename-plus-deletion. Concretely: (1) rename the five frontend sub-namespaces `CobolSharp.Compiler.{Common,Diagnostics,Generated,Parsing,Preprocessor}` → `CobolNet.Frontend.{…}` across **all** consumers (greenfield src + greenfield tests + the still-live legacy tree that shares the front-end + build scripts), single-sourcing the generated ANTLR package name through an MSBuild `<AntlrNamespace>` property; (2) delete the five unreferenced top-level grammars and the committed `.antlr` IDE caches (and gitignore the latter); (3) hard-delete the non-ISO JSON/XML grammar rules, relocating the one live 2023 `inlineMethodInvocationStatement` rule into `Core/CobolOO.g4`; (4) fix the stale `Frontend.cs` "reuses the legacy assembly" banner and narrow its broad `catch(Exception)` to the ANTLR bail exceptions. The full battery (greenfield conformance + unit + the FULL legacy guard NIST 353 MATCH) stays green throughout, and the emitted-C# characterization snapshots stay byte-identical.

---

## 2. Rationale — the problems this phase fixes

From the AS-IS dossier (Grammar/lexer + Edition-framework + Driver surveys) and verified against the code:

1. **Deferred-rename split (`namespace` ≠ assembly).** The front-end assembly is `Cobol.Net.Frontend` (`RootNamespace CobolNet.Frontend`) but its types are emitted into the **legacy** `CobolSharp.Compiler.*` namespaces, and ANTLR's generated parser lands in `CobolSharp.Compiler.Generated` — a hard-coded string in `src/Cobol.Net.Frontend/Invoke-Antlr4CSharp.ps1:29`. This forces `using CobolSharp.Compiler.Generated;` on 16 greenfield-compiler files plus a `using Core = CobolParserCore;` alias on 34 files, and keeps a name that G8 would otherwise have to sweep in a risky big-bang. (DESIGN-module-topology "Pull the rename forward"; DESIGN-frontend-grammar M-steps.)
2. **Dead / mislabeled grammar files.** Five top-level grammars — `Grammar/CobolParserJsonXml.g4`, `CobolParserGenerics.g4`, `CobolParserOO.g4`, `CobolDialect.g4`, `CobolPreprocessor.g4` — are **not generated and not referenced** by any C# (the ACTIVE grammar is exactly `Grammar/Core/CobolLexer.g4` + `Grammar/CobolParserCore.g4` importing nine `Core/` fragments). They are stale duplicates that mislead every grammar survey. (Grammar survey SMELL "Dead / mislabeled grammar files"; REORG "DELETE the five dead top-level grammars".)
3. **Committed ANTLR java-target caches.** Nine `Grammar/**/.antlr/*.{java,interp,tokens}` IDE-cache files are checked in — build output that belongs nowhere in source control. (Grammar survey REORG "Remove the committed `.antlr` caches".)
4. **Non-ISO JSON/XML in a LIVE fragment.** `Core/CobolExtensionsJsonXml.g4` (imported by `CobolParserCore.g4`) carries `jsonStatement` / `xmlStatement` — constructs with **zero** occurrences in ISO/IEC 1989:2023 (chair-verified; the former "ISO 2014 §14.9.x" citations were fictional — DEVLOG 586, `tests/version-matrix/vendor-constructs.json`). A hard invariant of the rearchitecture is that JSON/XML are out of scope. The fragment also carries the one genuinely-live 2023 rule, `inlineMethodInvocationStatement`, which must survive. (Hard invariant #5; DESIGN-frontend-grammar "hard-delete the non-ISO JSON/XML rules … move the one live `inlineMethodInvocationStatement` into `Core/CobolOO.g4`".)
5. **Stale banner + over-broad catch.** `Frontend.cs:16-25` claims the front-end "reuses the legacy `CobolSharp.Compiler` assembly" (false — it IS `Cobol.Net.Frontend`), and `Frontend.cs:135` catches `Exception` on the SLL bail, silently retrying **any** failure (including predicate/lexer-action bugs) under LL instead of surfacing it. (Driver/Grammar surveys; DESIGN-frontend-grammar "narrow `Frontend.cs:135` catch(Exception) to `ParseCanceledException` + `RecognitionException`".)

### 2.1 CRITICAL discovered dependency (corrects the digest)

The design digest frames the rename as "greenfield src only, legacy tree untouched." **The as-built code does not permit that literally.** The legacy compiler consumes the shared front-end:

- `src/CobolSharp.Compiler/CobolSharp.Compiler.csproj:25` has `<ProjectReference Include="..\Cobol.Net.Frontend\…" />`, and **17** legacy-compiler `.cs` files `using CobolSharp.Compiler.{Common,Diagnostics,Generated,Parsing,Preprocessor}` — which resolve to **`Cobol.Net.Frontend` types**, not legacy ones (the legacy tree has **no** `Common/Diagnostics/Generated/Parsing/Preprocessor` folders or namespace declarations of its own — verified). Also `src/CobolSharp.CLI/Program.cs:4` and **11** legacy test files import these five sub-namespaces.
- Therefore renaming the five frontend sub-namespaces **must** update every consumer of the shared front-end in the **whole solution** — otherwise the legacy compiler/CLI/tests fail to build and the legacy guard (which runs `src/CobolSharp.CLI/bin/…/cobolsharp.dll`) goes red.

Reconciliation of the exit criterion: the legacy tree's **own** namespaces (`CobolSharp.Compiler` root, `.Semantics`, `.CodeGen`, `.IR`, `.FlowAnalysis`) and all its logic/types stay **untouched**; only its `using` **imports of the shared front-end** flip to `CobolNet.Frontend.*`. "Grep-clean of `CobolSharp.Compiler.*` in greenfield src" holds fully; the legacy tree keeps its own `CobolSharp.Compiler.*` identity. This is a mechanical import rename, not a rearchitecture of the frozen oracle.

### 2.2 Decision: KEEP the `using Core = CobolParserCore;` alias (correcting the digest)

The scope text and DESIGN-module-topology assert the namespace rename "removes every `using Core =` alias." **This is factually incorrect for the as-built code and is intentionally NOT followed** (documented here per the process rule to keep design docs current when the original isn't followed):

- `using Core = CobolParserCore;` is a **type alias to the generated parser class**, orthogonal to the namespace. Renaming `CobolSharp.Compiler.Generated → CobolNet.Frontend.Generated` leaves `CobolParserCore` (the class) unchanged, so the alias keeps working verbatim and is not "removed" by the rename.
- `Core.` prefixes **473 references** to *nested* context types (e.g. `Core.StatementContext`). A nested type cannot be named without its containing type, so dropping the alias means rewriting `Core.X` → `CobolParserCore.X` (473 sites) — **longer**, not cleaner, pure churn with real risk and zero behavioral/clarity gain, contrary to P1's mechanical-zero-behavior mandate.

The alias is therefore preserved. (A future typed-`Cst/` façade — **P4** — is the real removal of these stringly `Core.*Context` walks; deleting the alias here would be wasted motion P4 undoes.)

---

## 3. Target end-state for this phase (concrete)

When P1 is DONE:

- **Namespaces.** No `.cs` file **outside** the frozen legacy tree's own types declares or imports `CobolSharp.Compiler.{Common,Diagnostics,Generated,Parsing,Preprocessor}`. The front-end declares `CobolNet.Frontend.{Common,Diagnostics,Generated,Parsing,Preprocessor}` (matching its `RootNamespace CobolNet.Frontend`). The generated parser + its hand-written `superClass` (`Parsing/CobolParserCoreBase.cs`, which declares the *Generated* namespace) both live in `CobolNet.Frontend.Generated`.
- **ANTLR package single-sourced.** `src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj` defines `<AntlrNamespace>CobolNet.Frontend.Generated</AntlrNamespace>`; `GenerateIfNewer.ps1` and `Invoke-Antlr4CSharp.ps1` take a `-PackageName` param defaulting to that value; the csproj `Exec` threads `$(AntlrNamespace)` through. The literal `'CobolSharp.Compiler.Generated'` no longer appears in any script.
- **Grammar tree.** `Grammar/` contains only `CobolParserCore.g4` + `Core/{CobolLexer,CobolExpressions,CobolData,CobolSpecialNames,CobolReportWriter,CobolIO,CobolControlFlow,CobolOO,CobolScreen}.g4`. Deleted: the 5 top-level dead grammars, `Core/CobolExtensionsJsonXml.g4`, and every `Grammar/**/.antlr/` file (now gitignored).
- **JSON/XML gone; inline-method preserved.** `CobolParserCore.g4` no longer imports `CobolExtensionsJsonXml` nor dispatches `jsonStatement`/`xmlStatement`. The `{is2023()}? inlineMethodInvocationStatement` dispatch arm is unchanged; the rule itself now lives in `Core/CobolOO.g4`. JSON/XML **lexer tokens** (`JSON`, `XML`, `END_JSON`, `END_XML`) and the `cobolWord` context words `PARSE`/`PROCESSING` are **retained** (removing them is a reserved-word behavior change out of scope, and `EditionGateHints` still keys the vendor-hint diagnostic off `CobolLexer.JSON`/`XML`).
- **Frontend.cs.** The banner accurately describes `Cobol.Net.Frontend` (no "reuses the legacy assembly" claim); the SLL-bail `catch` is `catch (ParseCanceledException)` (the type `Antlr4.Runtime.Misc.ParseCanceledException` that `BailErrorStrategy` throws), with `RecognitionException` also caught for defense.
- **Docs.** The `Cobol.Net.Frontend.csproj` and `Cobol.Net.Compiler.csproj` "namespaces stay `CobolSharp.Compiler.*` until G8" comments are corrected; `docs/COBOLNET_DESIGN.md §1.4` and `docs/DOC_INDEX.md` note the rename landed at P1.
- **Battery.** Greenfield conformance (2028+) + unit (213+) + the FULL legacy guard (`guard-fast.sh` → NIST **353 MATCH**) green; P0 characterization emit-snapshots **byte-identical** (no `.g.cs` change).

---

## 4. STEP-BY-STEP

> **Working discipline.** Commit at each ★ boundary; run the relevant verification before committing; keep every commit green. Use the Bash tool (Git Bash) for `git`/`sed`/`grep`; ANTLR regen requires `java` + `pwsh` on PATH (build prerequisites). All paths are absolute-from-repo-root `E:/CobolSharp`.

### Step 0 — Baseline: prove the battery is green and capture the neutrality reference
**Why:** every later step must preserve this exact state; you need a known-green starting point and a byte reference for the emit snapshots.
**Do:**
```bash
cd /e/CobolSharp
git status                      # expect a clean tree (stash the stray *.dat/*.csv/t8.cob if present)
dotnet build CobolSharp.sln -c Debug           # WHOLE solution (greenfield + legacy) must build
dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --no-build   # greenfield conformance (2028+)
dotnet test tests/Cobol.Net.Tests.Unit        -c Debug --no-build   # greenfield unit (213+)
dotnet test tests/Cobol.Net.Tests.Characterization -c Debug        # P0 snapshots (if P0 done)
bash scripts/guard-fast.sh                     # FULL legacy guard → "ALL GREEN" / NIST 353 MATCH
```
**Expected:** all green; guard prints `ALL GREEN` (353 MATCH). Record the counts in the DEVLOG working note.
**Not a commit boundary.** If anything is red *before* you start, STOP — do not begin P1 on a red tree.

---

### Step 1 — Delete the five dead top-level grammars  ★ COMMIT BOUNDARY
**Files (delete):**
`src/Cobol.Net.Frontend/Grammar/CobolParserJsonXml.g4`, `…/CobolParserGenerics.g4`, `…/CobolParserOO.g4`, `…/CobolDialect.g4`, `…/CobolPreprocessor.g4`.
**Change:** `git rm` all five. They are neither generated (only `Grammar/Core/CobolLexer.g4` and `Grammar/CobolParserCore.g4` are fed to ANTLR — see `Invoke-Antlr4CSharp.ps1`) nor referenced by any C#.
**Why:** removes the misleading dead surface (Rationale #2).
**Verify (they are truly unreferenced, then build):**
```bash
cd /e/CobolSharp
# Prove no ANTLR/C#/csproj reference to any of the five (expect NO matches beyond the files themselves, now deleted):
grep -rn "CobolParserJsonXml\|CobolParserGenerics\|CobolParserOO\|CobolDialect\|CobolPreprocessor" \
  src --include="*.g4" --include="*.cs" --include="*.csproj" --include="*.ps1" | grep -v "/bin/\|/obj/\|/Generated/"
git rm src/Cobol.Net.Frontend/Grammar/CobolParserJsonXml.g4 \
       src/Cobol.Net.Frontend/Grammar/CobolParserGenerics.g4 \
       src/Cobol.Net.Frontend/Grammar/CobolParserOO.g4 \
       src/Cobol.Net.Frontend/Grammar/CobolDialect.g4 \
       src/Cobol.Net.Frontend/Grammar/CobolPreprocessor.g4
dotnet build src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj -c Debug   # regen triggers; must succeed
```
> Note: `CobolPreprocessor` will still legitimately match the *committed `.antlr` cache* (`Grammar/.antlr/CobolPreprocessor.java`) — that is deleted in Step 2. It must NOT match anything under `Parsing/`/`Preprocessor/` `.cs` (those are the hand-written preprocessor, a different thing).
**Expected:** the grep shows only the soon-deleted cache; the front-end builds (ANTLR regenerates cleanly from the two real grammars).
**Commit:** `chore(cobolnet): P1 — delete 5 dead unreferenced top-level grammars (DESIGN-frontend-grammar)`

---

### Step 2 — Delete the committed `.antlr` IDE caches and gitignore them  ★ COMMIT BOUNDARY
**Files (delete, tracked):** all nine under `src/Cobol.Net.Frontend/Grammar/.antlr/` and `…/Grammar/Core/.antlr/` (`CobolLexer.{interp,java,tokens}`, `CobolPreprocessor.{interp,java,tokens}`).
**File (edit):** `.gitignore` — add an ignore rule for `.antlr/` under the grammar tree.
**Change:**
```bash
cd /e/CobolSharp
git rm -r src/Cobol.Net.Frontend/Grammar/.antlr src/Cobol.Net.Frontend/Grammar/Core/.antlr
```
Add to `.gitignore` (next to the existing `src/Cobol.Net.Frontend/Generated/` rule, ~line 45):
```
# ANTLR IDE-plugin java-target caches — never source (build/IDE output)
**/.antlr/
```
**Why:** removes build/IDE output from source control (Rationale #3); the ignore rule stops it recurring.
**Verify:**
```bash
git ls-files | grep "\.antlr/"     # expect ZERO output
dotnet build src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj -c Debug   # unaffected (caches are IDE-only)
```
**Expected:** no tracked `.antlr` files; build succeeds (the C# generator writes to `Generated/`, never `.antlr/`).
**Commit:** `chore(cobolnet): P1 — remove committed Grammar .antlr IDE caches; gitignore **/.antlr/`

---

### Step 3 — Hard-delete JSON/XML grammar; relocate `inlineMethodInvocationStatement` to `Core/CobolOO.g4`  ★ COMMIT BOUNDARY
**Files:**
- edit `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4`
- edit `src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4`
- delete `src/Cobol.Net.Frontend/Grammar/Core/CobolExtensionsJsonXml.g4`

**Change 3a — `CobolParserCore.g4` import list (line 16).** Remove `CobolExtensionsJsonXml,`:
```
import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter, CobolIO, CobolControlFlow, CobolOO, CobolScreen;
```
**Change 3b — `CobolParserCore.g4` statement dispatch (lines 716-717).** Delete the two JSON/XML arms:
```
    | {is2014()}? jsonStatement      <-- DELETE
    | {is2014()}? xmlStatement       <-- DELETE
```
Leave the neighbors — `{is2002()}? invokeStatement` and `{is2023()}? inlineMethodInvocationStatement` — **unchanged** (the inline-method rule is still referenced here; it now resolves from `CobolOO.g4`).
**Change 3c — move `inlineMethodInvocationStatement` into `Core/CobolOO.g4`.** Append to the end of `CobolOO.g4` (its dependency `argumentList` lives in `Core/CobolExpressions.g4`, already merged into the composite grammar):
```antlr
// ── INLINE METHOD INVOCATION (COBOL-2023, ISO §8.4.3 in-line method invocation) ──
// Relocated from the deleted JSON/XML fragment (P1). `argumentList` is defined in Core/CobolExpressions.g4.
inlineMethodInvocationStatement
    : dataReference LPAREN argumentList? RPAREN
    ;
```
**Change 3d — delete the emptied fragment.** `git rm src/Cobol.Net.Frontend/Grammar/Core/CobolExtensionsJsonXml.g4`.
**Do NOT touch:** the JSON/XML **lexer tokens** in `Core/CobolLexer.g4`, nor the `PARSE`/`PROCESSING` entries in the `cobolWord` rule (retained — see §3; removing them is an out-of-scope reserved-word change and would break `EditionGateHints`).
**Why:** removes non-ISO surface (Rationale #4, hard invariant #5) while preserving the live 2023 OO rule and the vendor-hint diagnostic path.
**Behavioral note (expected + intended):** at `--std ≥ 2014` a `JSON GENERATE`/`XML …` statement previously *parsed* then bound to a runtime-loud stub; now it fails at *parse* with the `EditionGateHints` vendor message ("not an ISO/IEC 1989 construct") at **every** edition. At `--std < 2014` behavior is unchanged (the `{is2014()}?` predicate already rejected it into the same `NoViableAlternative` → `EditionGateHints` path). This is the intended hardening.
**Verify:**
```bash
cd /e/CobolSharp
grep -rn "CobolExtensionsJsonXml\|jsonStatement\|xmlStatement\|jsonXmlExceptionPhrases" \
  src/Cobol.Net.Frontend/Grammar --include="*.g4"     # expect ZERO
dotnet build src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj -c Debug     # ANTLR regen must succeed (no undefined-rule error)
# The existing vendor-disposition test must still pass (JSON GENERATE at 85 → vendor hint, no COBOLNET0900):
dotnet test tests/Cobol.Net.Tests.Conformance -c Debug \
  --filter "FullyQualifiedName~EditionGateDiagnosticTests.JsonGenerate_Below2014_VendorDisposition_Not0900"
```
**Expected:** grep empty; regen succeeds; `JsonGenerate_Below2014_VendorDisposition_Not0900` **passes**.
> ⚠ **Correction to the original assumption (found in execution, DEVLOG 668).** This step's design claimed the test
> passes because `EditionGateHints` is "untouched and still keys off `CobolLexer.JSON`". That was WRONG: deleting the
> `jsonStatement` grammar arm changed the parse error from a `NoViableAlternative` reported DEEP inside
> `procedureDivision` (where the old `EditionGateHints` guard `InRule(ruleStack, "procedureDivision")` matched) to an
> unexpected-token reported AFTER those frames unwind — so the guard no longer matched and the message regressed to a
> bare `COBOL0001: unexpected 'JSON'`. **Fix (same change set):** `EditionGateHints.Recognize` drops the
> `procedureDivision` rule-stack guard for the JSON/XML branch — the `JSON`/`XML` tokens are HARD-RESERVED (absent from
> the `cobolWord` user-word set; only `PARSE`/`PROCESSING` are user words), so an offending `JSON`/`XML` token is an
> unambiguous JSON/XML-statement signature on its own. With the fix the test passes at every edition.
**Matrix rows:** `tests/version-matrix/constructs.json` has **no** JSON/XML rows (already parked in `tests/version-matrix/vendor-constructs.json`, DEVLOG 586). So "drop the json/xml-generate matrix rows" is already satisfied — **no matrix edit needed**; leave `vendor-constructs.json` in place (it is a parked catalogue, not a live matrix input).
**Commit:** `feat(cobolnet)!: P1 — hard-delete non-ISO JSON/XML grammar; move inlineMethodInvocation to CobolOO.g4`

---

### Step 4 — Single-source + rename the generated ANTLR package → `CobolNet.Frontend.Generated`  ★ COMMIT BOUNDARY
This is the first namespace-rename slice. The generated parser namespace is set **only** by the `-package` argument in `Invoke-Antlr4CSharp.ps1`; the hand-written `superClass` file declares that same namespace and must move in lockstep; all consumers' `using` lines flip.

**Files:**
- edit `src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj` (add `<AntlrNamespace>`, thread it into the `Exec`)
- edit `src/Cobol.Net.Frontend/GenerateIfNewer.ps1` (accept `-PackageName`, pass it through)
- edit `src/Cobol.Net.Frontend/Invoke-Antlr4CSharp.ps1` (default `-PackageName` → new value)
- edit `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs` (namespace decl `CobolSharp.Compiler.Generated` → `CobolNet.Frontend.Generated`)
- edit **every** consumer's `using CobolSharp.Compiler.Generated;` → `using CobolNet.Frontend.Generated;` (16 greenfield-compiler files, greenfield tests, 17 legacy-compiler files, legacy tests — the repo-wide replace below covers all).

**Change 4a — csproj property + Exec threading.** In `Cobol.Net.Frontend.csproj`, add to the main `<PropertyGroup>`:
```xml
<AntlrNamespace>CobolNet.Frontend.Generated</AntlrNamespace>
```
and change the generation `Exec` command to pass it:
```xml
<Exec Command="pwsh -ExecutionPolicy Bypass -File &quot;$(MSBuildProjectDirectory)/GenerateIfNewer.ps1&quot; -PackageName $(AntlrNamespace)"
      WorkingDirectory="$(MSBuildProjectDirectory)" />
```
**Change 4b — `GenerateIfNewer.ps1`.** Add a param block at the top and forward it:
```powershell
param([string]$PackageName = 'CobolNet.Frontend.Generated')
...
$result = Invoke-Antlr4CSharp -PackageName $PackageName
```
**Change 4c — `Invoke-Antlr4CSharp.ps1:29`.** Change the default:
```powershell
[string]$PackageName = 'CobolNet.Frontend.Generated'
```
**Change 4d — the repo-wide `using`/namespace flip for the Generated segment.** Run (Git Bash), replacing the exact segment string across all tracked `.cs`, **excluding** build output and the regenerated `Generated/` folder:
```bash
cd /e/CobolSharp
grep -rlZ "CobolSharp\.Compiler\.Generated" --include="*.cs" src tests \
  | grep -zZv "/bin/\|/obj/\|/Generated/" \
  | xargs -0 sed -i 's/CobolSharp\.Compiler\.Generated/CobolNet.Frontend.Generated/g'
```
This edits `Parsing/CobolParserCoreBase.cs` (its `namespace` decl) and every `using CobolSharp.Compiler.Generated;` in both trees. The `using Core = CobolParserCore;` alias lines are **untouched** (they name the class, not the namespace) and keep working (§2.2).
**Why:** ends the hard-coded package string, decouples the rename from the G8 big-bang, keeps `superClass` resolution intact (the base class must share the generated namespace — verified: `CobolParserCoreBase.cs` declares `…Generated`, not `…Parsing`).
**Verify:**
```bash
cd /e/CobolSharp
# No stray Generated-namespace references outside build output:
grep -rn "CobolSharp\.Compiler\.Generated" --include="*.cs" --include="*.ps1" --include="*.csproj" src tests \
  | grep -v "/bin/\|/obj/\|/Generated/"          # expect ZERO
rm -rf src/Cobol.Net.Frontend/Generated          # force a clean regen under the new package
dotnet build CobolSharp.sln -c Debug             # WHOLE solution: greenfield + legacy must build
grep -n "namespace" src/Cobol.Net.Frontend/Generated/CobolParserCore.cs | head -1   # → CobolNet.Frontend.Generated
```
**Expected:** grep empty; the freshly-regenerated parser declares `namespace CobolNet.Frontend.Generated`; the whole solution builds.
**Commit:** `refactor(cobolnet)!: P1 — generated ANTLR package → CobolNet.Frontend.Generated via <AntlrNamespace>`

---

### Step 5 — Rename the four hand-written frontend sub-namespaces  ★ COMMIT BOUNDARY
Flip `CobolSharp.Compiler.{Common,Diagnostics,Parsing,Preprocessor}` → `CobolNet.Frontend.{…}` — both the **declarations** (in `Cobol.Net.Frontend/{Common,Diagnostics,Parsing,Preprocessor}/*.cs`) and every **consumer** `using` across the whole solution (greenfield compiler/tests + the shared-front-end legacy compiler/CLI/tests — see §2.1).

**Change — one scoped, segment-literal replace per name.** Run (Git Bash):
```bash
cd /e/CobolSharp
for seg in Common Diagnostics Parsing Preprocessor; do
  grep -rlZ "CobolSharp\.Compiler\.$seg" --include="*.cs" src tests \
    | grep -zZv "/bin/\|/obj/\|/Generated/" \
    | xargs -0 --no-run-if-empty sed -i "s/CobolSharp\.Compiler\.$seg/CobolNet.Frontend.$seg/g"
done
```
**Safety of the pattern (why it cannot corrupt the legacy tree):** the replacement is the exact segment `CobolSharp.Compiler.<Name>`. The legacy tree's own namespaces are `CobolSharp.Compiler` (root), `.Semantics`, `.CodeGen`, `.IR`, `.FlowAnalysis` and the type `CobolSharp.Compiler.Compilation` (used via `using LegacyCompilation = CobolSharp.Compiler.Compilation;` in `tests/Cobol.Net.Tests.Conformance/CompilerUnderTest.cs`) — **none** match `…Compiler.{Common,Diagnostics,Parsing,Preprocessor}`. Verified: no file outside the front-end declares these four sub-namespaces, so the flip is unambiguous.
**Why:** completes the frontend namespace rename to match its `RootNamespace CobolNet.Frontend` (Rationale #1); together with Step 4, no shared-front-end type remains under `CobolSharp.Compiler.*`.
**Verify:**
```bash
cd /e/CobolSharp
# Greenfield src/tests must be fully clean of ALL five frontend sub-namespaces:
grep -rn "CobolSharp\.Compiler\.\(Common\|Diagnostics\|Generated\|Parsing\|Preprocessor\)" \
  src/Cobol.Net.Frontend src/Cobol.Net.Compiler src/Cobol.Net.Runtime src/Cobol.Net.Cli \
  tests/Cobol.Net.Tests.Conformance tests/Cobol.Net.Tests.Unit tests/Cobol.Net.Tests.Characterization \
  --include="*.cs" | grep -v "/bin/\|/obj/\|/Generated/"      # expect ZERO
# The legacy tree keeps its OWN CobolSharp.Compiler.* types but has flipped its front-end imports:
grep -rn "using CobolSharp\.Compiler\.\(Common\|Diagnostics\|Generated\|Parsing\|Preprocessor\)" \
  src/CobolSharp.Compiler src/CobolSharp.CLI tests/CobolSharp.Tests.Unit tests/CobolSharp.Tests.Integration \
  --include="*.cs" | grep -v "/bin/\|/obj/"                   # expect ZERO (imports flipped)
dotnet build CobolSharp.sln -c Debug                          # whole solution builds
```
**Expected:** both greps empty; the whole solution builds. (The legacy tree still *declares* `namespace CobolSharp.Compiler.Semantics` etc. — that is correct and intended; only its front-end **imports** changed.)
**Commit:** `refactor(cobolnet)!: P1 — frontend namespaces CobolSharp.Compiler.{Common,Diagnostics,Parsing,Preprocessor} → CobolNet.Frontend.*`

---

### Step 6 — Fix `Frontend.cs` banner + narrow the SLL-bail catch  ★ COMMIT BOUNDARY
**File:** `src/Cobol.Net.Frontend/Pipeline/Frontend.cs`.
**Change 6a — banner (the `<remarks>` block, ~lines 15-26).** Replace the "This is the ONE place COBOL.NET reuses the legacy `CobolSharp.Compiler` assembly …" text with an accurate statement, e.g.:
```csharp
/// This is the COBOL.NET front-end (assembly <c>Cobol.Net.Frontend</c>): the source preprocessor
/// (reference-format normalization, conditional compilation, COPY expansion, NIST placeholder
/// substitution) and the ANTLR lexer/parser. The parse tree it returns
/// (<see cref="CobolParserCore.CompilationUnitContext"/>) is a pure syntactic artifact — no semantic
/// analysis, storage layout, or emission is involved. It is shared, unchanged, by both the greenfield
/// COBOL.NET pipeline and (until the G8 cut-over) the legacy differential oracle.
```
Keep the accurate second paragraph about mirroring the SLL→LL two-stage parse and the `ZERO`→`ZERO_ARITH` rewrite.
**Change 6b — narrow the catch (line 135).** Ensure `using Antlr4.Runtime.Misc;` is present (for `ParseCanceledException`), then:
```csharp
        catch (Exception e) when (e is ParseCanceledException or RecognitionException)
        {
            // SLL bailed on a genuine parse ambiguity/mismatch — retry with full LL prediction and the
            // diagnostic-collecting error strategy. A non-parse exception (predicate/lexer-action bug) now
            // propagates instead of being silently retried under LL.
            tokens.Seek(0);
            parser.Reset();
            parser.Interpreter.PredictionMode = PredictionMode.LL;
            parser.ErrorHandler = new CobolErrorStrategy();
            tree = parser.compilationUnit();
        }
```
**Why:** the banner is false post-rename (Rationale #5); `BailErrorStrategy` throws `ParseCanceledException` (wrapping a `RecognitionException`) on the SLL pass — catching exactly those preserves the two-stage behavior while surfacing real bugs. This is the **one** non-mechanical behavior change in P1; it is on the error path only and is isolated to its own commit so it can be reverted independently if any corpus program regresses.
**Verify:**
```bash
cd /e/CobolSharp
dotnet build src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --no-build   # every program still parses identically
bash scripts/guard-fast.sh                                          # NIST 353 MATCH — the SLL→LL path is exercised broadly
```
**Expected:** all green; NIST 353 MATCH. (If any program regresses, the culprit is a genuine non-recognition exception previously masked — investigate it, do NOT widen the catch back to `Exception`.)
**Commit:** `refactor(cobolnet): P1 — correct Frontend banner; narrow SLL-bail catch to ParseCanceled/RecognitionException`

---

### Step 7 — Fix the stale csproj/design banners  ★ COMMIT BOUNDARY
**Files:**
- `src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj` (header comment lines ~5-8: "namespaces stay `CobolSharp.Compiler.*` through G0–G7 … only the project/assembly is renamed here; the cosmetic namespace rename is the G8 big-bang").
- `src/Cobol.Net.Compiler/Cobol.Net.Compiler.csproj` (comment line ~26: "namespaces stay `CobolSharp.Compiler.*` until G8").
- `docs/COBOLNET_DESIGN.md` §1.4 (the "namespaces stay `CobolSharp.Compiler.*` until G8" clause).
- `docs/DOC_INDEX.md` (note P1 landed the rename, if a relevant row exists).
**Change:** update each to state the front-end namespaces are `CobolNet.Frontend.*` as of P1 (rearchitecture roadmap `docs/rearchitecture/PHASE-01…`), and that G8 is now a pure *deletion* of the legacy tree. Keep the note that the legacy tree retains its own `CobolSharp.Compiler.*` identity until G8.
**Why:** the process rule (`feedback_session_context`, `feedback_update_adr_on_design_corrections`) requires design/banner text to track the code in the same change set; a stale "rename at G8" banner would mislead every later phase.
**Verify:** `dotnet build CobolSharp.sln -c Debug` (comment-only edits — must still build). No functional test needed.
**Commit:** `docs(cobolnet): P1 — correct namespace-rename banners (frontend now CobolNet.Frontend.*; G8 = deletion)`

---

### Step 8 — Full battery + neutrality proof + STATUS → DONE  ★ COMMIT BOUNDARY (DEVLOG)
Run §5 in full. If green and snapshots byte-identical, set the STATUS line to `DONE`, add the DEVLOG entry, and commit.
**Commit:** `docs(cobolnet): P1 COMPLETE — mechanical rename + dead-grammar/JSON-XML removal (DEVLOG NNN)`

---

## 5. Verification — the full battery + behavior-neutrality checks

Run from `E:/CobolSharp` after Step 7:

```bash
# 1. Whole-solution build (greenfield + legacy share the renamed front-end)
dotnet build CobolSharp.sln -c Debug

# 2. Greenfield battery
dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --no-build     # 2028+ green, 0 fail
dotnet test tests/Cobol.Net.Tests.Unit        -c Debug --no-build     # 213+ green, 0 fail

# 3. P0 characterization — the behavior-neutrality gate (byte-identical emit)
dotnet test tests/Cobol.Net.Tests.Characterization -c Debug           # snapshots MATCH (no re-baseline)
#    A gate-3 (emitted .g.cs) diff with NO production-logic change = a FAILED neutrality proof: STOP and find the leak.
#    Do NOT set COBOLNET_UPDATE_SNAPSHOTS=1 in this phase — P1 changes zero emitter behavior.

# 4. FULL legacy guard (the frozen oracle, on the shared renamed front-end)
bash scripts/guard-fast.sh                                            # "ALL GREEN" / NIST 353 MATCH, 0 regressions

# 5. Grep-clean invariants
grep -rn "CobolSharp\.Compiler\.\(Common\|Diagnostics\|Generated\|Parsing\|Preprocessor\)" \
  src/Cobol.Net.Frontend src/Cobol.Net.Compiler src/Cobol.Net.Runtime src/Cobol.Net.Cli \
  tests/Cobol.Net.Tests.* --include="*.cs" | grep -v "/bin/\|/obj/\|/Generated/"       # ZERO
git ls-files | grep "\.antlr/"                                        # ZERO
ls src/Cobol.Net.Frontend/Grammar/*.g4                                # ONLY CobolParserCore.g4
grep -rn "jsonStatement\|xmlStatement\|CobolExtensionsJsonXml" src/Cobol.Net.Frontend/Grammar --include="*.g4"  # ZERO
grep -rn "CobolSharp.Compiler.Generated" src/Cobol.Net.Frontend/Invoke-Antlr4CSharp.ps1 src/Cobol.Net.Frontend/GenerateIfNewer.ps1  # ZERO
```

**Portable-regen check (both OSes).** The ANTLR regen must succeed on Windows AND Linux (DEVLOG 554 was a separator-keyed output break). Confirm a clean regen locally:
```bash
rm -rf src/Cobol.Net.Frontend/Generated && dotnet build src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj -c Debug
```
and rely on the OS-matrix CI (P0's `build-and-test.yml`) for the Linux leg. The `Invoke-Antlr4CSharp.ps1` "generate each grammar from its own directory with a bare filename" portability fix is untouched by P1.

**Behavior-neutrality summary.** Steps 1-2, 4-5, 7 are provably zero-behavior (dead-file deletion, cache removal, namespace/string renames, comment edits). Step 3 changes JSON/XML from parse-then-loud to parse-error (intended hardening; no ISO program affected). Step 6 narrows an error-path catch (the one behavior change; guard + conformance prove no regression).

---

## 6. Rollback / resumability

- **Resuming mid-phase.** The STATUS line records the last completed step. Each step is an independent green commit, so `git log --oneline` shows exactly where you are; resume at the next unstarted step. Re-run Step 0's battery first to confirm the tree is green before continuing.
- **Per-step rollback.** Every step is one commit → `git revert <sha>` restores the prior green state without disturbing later steps (the steps are order-independent *except* Step 4 before Step 5 is preferred only for clean bisecting — both are pure renames and either order builds).
- **Risks + mitigations:**
  - *Missed consumer of a renamed namespace* → the whole-solution `dotnet build` in Steps 4/5 fails loudly (CS0246). Mitigation: the repo-wide `grep -rlZ | sed` covers `src` + `tests` uniformly; the §5 grep-clean is the backstop.
  - *Legacy guard red after Step 4/5* → almost certainly an unflipped legacy-tree front-end import (§2.1). Re-run the Step 5 legacy grep; flip the straggler.
  - *ANTLR regen fails after Step 3* → an undefined-rule reference (e.g. a leftover `jsonStatement` arm or `CobolExtensionsJsonXml` in the import list). Re-check Changes 3a/3b; ensure `inlineMethodInvocationStatement` was appended to `CobolOO.g4` (3c) and the import list no longer names the deleted fragment.
  - *Emit-snapshot diff in §5.3 with no logic change* → a real (unexpected) behavior leak, not a re-baseline candidate. STOP; do not update snapshots; bisect the offending step.
  - *`using Core = CobolParserCore;` "cleanup" temptation* → do NOT (see §2.2); it is P4's job via the `Cst/` façade.

---

## 7. ISO feature work in this phase

**None** — P1 is foundation/cleanup, not a conformance step. No spec section is implemented and no golden is added. The one ISO-adjacent decision is a **subtraction**: JSON GENERATE/PARSE and XML GENERATE/PARSE are confirmed non-ISO (zero occurrences in `specs/ISO_COBOL.md`; DEVLOG 586 chair verification; parked in `tests/version-matrix/vendor-constructs.json`), so their grammar is removed with no conformance obligation created or lost. The existing `EditionGateDiagnosticTests.JsonGenerate_Below2014_VendorDisposition_Not0900` continues to assert the correct outcome (a vendor-disposition hint, never a COBOLNET0900 edition-introduction diagnostic) and is the regression guard for the removal. No new tests or goldens are required; the neutrality gate (§5.3) plus the FULL legacy guard (§5.4) are the phase's proof.
