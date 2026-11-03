## Entry 3034 — 2026-11-03 06:35 PST — kb/Work PB4462: every semgrep invariant rule is text, and verify.py reads the scan's errors; PB4473, PB4474, PB4475: a lane agent writes in its own scratch directory and a hook enforces it; the semgrep comment prefix reads whole literals and each rule is proven inside an unread span; the PB4453 loose ends

### kb/Work PB4462: every semgrep invariant rule is text, and verify.py reads the scan's errors

`scripts/semgrep/verify.py` compared the invariant rules' finding counts with `baseline.json` and read only the scan's `results`,
never its `errors[]`. semgrep's C# grammar predates C# 12 and skips a span it cannot read, so an AST rule is blind there and a
rule that matches nothing in an unread span looks like one that matches nothing in a clean one. PB2352 re-baselined the raw
diagnostic-code literal rule 225 to 229 when 19 literals moved from an unread file into readable ones, and PB2355's DEVLOG blamed
225 to 226 on an "interpolated string with an ellipsis" when the line was byte-identical elsewhere.

**What the re-probe showed.** The old scan had 160 errors: 213 product files are read only in part and five not at all
(`ReportSectionBinder.cs`, `VersionConformancePass.cs`, `ConditionalPhrases.cs`, `Frontend.cs`, `BTree.cs`), in every project, not
three binder files. The constructs are valid C# 11-14 the product writes on purpose: a primary-constructor class header, a list
pattern with a slice designation, `extension` members, `allows ref struct`, a raw interpolated string. After a failure early in
a class semgrep drops the span to the next member it can read, which was `DataBinder.cs` lines 14-4492.

**The mechanism, found by experiment, decided the fix.** semgrep parses a file only when a rule needs the parse or has a hit. A
single `pattern-regex` rule is matched over the file's characters, and a hit inside an unread span is still reported (the new
regexes and an independent Python pass agree on all 248 raw-literal hits file by file, 18 of them inside `DataBinder.cs`'s unread
span). A `patterns:` block, even of regexes, parses first. A file that fails to parse wholly loses every hit to every rule. So
the blind rules were the three AST rules (`no-decimal`, `no-persistent-byte-storage`, `raw-diagnostic-code-literal`) and the one
`patterns:` regex rule (`integer-n-parsed-outside-host-value`); the other five regex rules were never blind.

**Fix.** `invariants.yml`: all nine rules are one `pattern-regex`. The AST rules match the same declarations and calls as text, a
`//` comment ruled out by a tempered prefix, and the `patterns:` rule's `pattern-not-regex` is a lookahead. `verify.py` has four
checks now. Check 0 refuses any rule that is not one `pattern-regex`, naming its keys, so the next AST rule cannot reintroduce the
blindness. Check 1 fire-tests each rule in `testdata/Violations.cs` and in the new `testdata/Unparsed.cs`, a file semgrep reads
only in part. Check 2 reads `errors[]`: a `PartialParsing` span is counted and listed by `--gaps`, and anything else (a whole-file
"Syntax error", a timeout, a rule error) fails naming the file; it also compares the files semgrep cannot read at all with
`WHOLE_FILE_UNREADABLE`, the five above with their first failing member, and a missing or stale entry fails. Check 3 is the old
ratchet. `--self-test` covers the pure parts (the real config is text, each rule shape is refused, every error kind is classified,
the exception drift, the ratchet) and needs no semgrep, so CI's audits run it. The five files are named and not reshaped: they
are valid C#, and a lint tool's parser does not decide the code. A violation added to one fails `verify.py`, because the dropped
hit surfaces as that file's "Syntax error".

**Re-measured.** The raw-code-literal count is 248, not 230: the 18 hidden sites are all in `DataBinder.cs` (COBOL0898 x6, COBOL1625
x4, COBOL1530 x2, COBOL1537 x2, COBOL1532 x2, COBOL1536, COBOL1538). The other three counts are unchanged (43, 3, 2). Of the 248, 11
sites already had a catalog descriptor that nothing used (COBOL1625 `ValueNumericOutOfRange` had no reference at all; COBOL1571 x3,
COBOL1627, COBOL1664 x3) and now name it, which changes no diagnostic (`Edition.Error(descriptor, ...)` forwards the code). The
baseline is 237, a measurement. The 237 sites carry 86 codes with no descriptor, where PB4463 named 19: its note carries the count,
and the drift test that should have caught them, `DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor`, skips every
dotted call in its regex.

**Merged first.** The PB2352 branch (PR #1045, worktree-wf_7d06ed65-e14-4) was merged in with main's PB2104 work resolved by hand in
five files (`OoDataBinder.cs` took `m.Definition` and `Binding.FormalName`, the pointer claim scan in `PtrBindBasedAndAddressablesPass.cs`
took `m.Definition`, `RepositoryCodec.cs` kept the expansion records over `State.Configuration`, two inventory rows kept main's
verdicts); the build is 0 warnings, 0 errors.

**Self-review.** Architecture: one mechanism per job, the rule set is data and the verifier derives what it may contain. Full code:
the regexes were compared with semgrep's own AST results on every parseable region before the AST rules were removed (no
difference in 230 sites) and with a Python pass afterwards (no difference in 248). Performance: `verify.py` adds one parse-all
semgrep run (about 15 s). Duplication: the raw-literal debt is PB4463's, not a second list.

**Branch review, round 1.** The reviewer found `scripts/arch/r2_inputs.py` (`run_semgrep`), the R2 review fleet's input step, reading
only `results` from the same scan: a file semgrep rejects wholly was reported to every finder as having no invariant hit, and a
timeout or rule error was dropped. It now reads `errors[]` through `verify.py`'s helpers, names the wholly unreadable files in each
shard input (`semgrep_unread`, from a forced parse, so a file with no hit is named too) and stops on any other error; its
self-test covers the three cases. The determination that a `PartialParsing` span is counted, not fatal (the owner asked for fatal)
is now recorded in the PB4462 note for the owner to confirm or overturn. The branch was merged again with main, which had moved
the OO method symbol's origin to a type (PB4459): its uses in `OoDataBinder.cs`, `PtrBindBasedAndAddressablesPass.cs` and
`FlagConformancePass.cs` were ported to the extracted binders.

**Merged with the DataBinder extract (#1054).** The reviewed branch was merged with origin/main after the extract landed as one
folded entry (3024), so the stamped entry 3015 the PB2352 branch carried is not in the tree (its content is 3024). The merge was
clean. Main then moved again: PB4463 (#1058) gave every emitted code a catalog descriptor, took the raw-code-literal rule's count to
zero and dropped its `baseline.json` key, and added a `$X.Report($EDITION, $CODE, ...)` arm (BoundRejected) to the then-AST rule.
Merging that conflicted in `invariants.yml` and `baseline.json`: the text rule keeps this branch's form and gains the same arm as a
regex alternative (`.Report(<edition>, "COBOL####"`; `Violations.cs`, which main extended, fires it at both lines 30 and 31), and
the baseline has no key for the rule, as on main. The 11 descriptor edits this branch made are the ones PB4463 made too, so the
merged tree has no `src/` difference from main (a duplicated `using` in `SpecialNamesBinder.Locales.cs` was dropped). `verify.py`
passes with the counts 3 / 43 / 2 (a scan of `src/` finds no raw-literal hit), and the build is 0 warnings, 0 errors. The module docstring of `scripts/arch/r2_inputs.py` now describes the semgrep step's
cached record (`hits` and `unread`) and the shard input's `semgrep_unread` (review lead R2-1).

### PB4473, PB4474, PB4475: a lane agent writes in its own scratch directory and a hook enforces it; the semgrep comment prefix reads whole literals and each rule is proven inside an unread span; the PB4453 loose ends

Three tooling notes, all minor, all loose ends the campaign's reviewers left on landed work.

**PB4473, scratch.** Re-probe: `review_lane.py` built the lane reviewer's prompt from the findings file alone, so a reviewer that
needed a probe program had no permitted place (`cobol-reviewer.md` requires "the scratch directory your prompt names"), and nothing
refused a dispatched agent's write into the session scratchpad. Fix: `scratch_path` gives each lane agent
`<coord>/scratch/<review|conflict>-<branch slug>/` (the findings file's name without `.jsonl`), `launch` creates it before the agent
starts and records it as `scratch` in the run, and `scratch_line` puts the rolling wave's reviewer sentence in both the reviewer's and
the conflict finisher's prompt (the finisher had the same gap). `check_practices.py` pins both prompt builders (`CODE_PROMPTS`) beside
the workflow's `BRIEFS` pin, so the two reviewers are tied to one rule; the standalone `finisher-brief.md` and the enqueue unit's
hand-dispatched reviewer (`units/enqueue.md`) now say the same. Structural half: a hook in an agent definition's frontmatter runs
only for that role, never for the operator (the mechanism `readonly_repo.py` already uses), which answers the note's open question of
how to tell a dispatched agent from the operator. `scripts/hooks/own_scratch.py` refuses a Write/Edit/NotebookEdit target or a
Bash/PowerShell command naming the harness's session folder, recognised by shape (`claude*/<slug>/<session uuid>/scratchpad`, the
Windows, Git Bash and Linux spellings, any case), and all seven `.claude/agents/` roles attach it. Tests: `test_own_scratch.py`
(33 cases, including a `..` that hides the folder, a relative target and a requirement that EVERY agent definition attaches the hook, so the
next role cannot forget it), the review lane's self-test (the prompt names a directory beside the findings file, created, distinct per
branch and per role), and `test_plan_wave.py` (the pin refuses a lane prompt that drops the sentence or one of its two callers).

**PB4474, semgrep.** (1) The comment-skipping prefix `^(?:(?!//)[^\n])*?` ruled out any match after a `//` even inside a string
(`var url = "http://x"; decimal d = 1;` was invisible). The three rules that used it now open with ONE prefix, `verify.CODE_PREFIX`,
which walks the line over ordinary characters, a `/` that starts no comment, and whole string, verbatim-string and char literals, so
only a comment's `//` stops the match. YAML cannot share a regex, so check 0 (`prefix_drift`) holds each copy to the constant and refuses
the old form; `testdata/Violations.cs` carries line controls (a trailing `FIRES <rule>` or `QUIET` marker, checked on semgrep's hits) and
the self-test runs the same cases through the rules' real regexes, which CI's audits do without semgrep. Counts on `src/` unchanged
(3 / 43 / 2). (2) The `Unparsed.cs` control read a file semgrep reads only at lines 14 and 22 and passed when a newer semgrep read it in
full. It is now `testdata/unparsed/<rule id>.cs`, one file per rule with its violation on a line semgrep reports as a `PartialParsing`
span; check 1 requires the rule to fire ON that line, and a file read in full FAILS as an obsolete control. One file per rule because
eight unreadable constructs in one file made semgrep give up on the whole file and report no span. (3) The `r2_inputs.py` docstring was
already corrected by the PB4462 finish. (4) The landed `PB4462.md` carries the "Determination (owner decision pending)" paragraph with
the overturn path; no owner decision exists, so no `decision` note.

**PB4475, PB4453 loose ends.** (1) `CLAUDE.md` Testing paragraph named `specs/ISO_COBOL.md` as the one READ exception; it now points at
`inert_lane.py`'s `CLASSIFICATION.read` as the one list. (2) `docchecks/register.py` cut each §7 determination to `LEAD_LENGTH` (64) on a
premise only a comment stated; `lead_covers_schema_prefixes` now fails the DOC AUDITS audit when a schema `determination-prefix` is longer,
so the C# selector (which reads the lead) cannot silently test nothing. The extract's bytes are unchanged. (3) `ROW_NUMBER` moves to
`docchecks/common.py`; `facts.py` and `vcr.py` import it. (4) Determined: the self-tests that read CONFORMANCE.md section 5, the
`DESIGN-test-build-ci.md` section `ci_bounds.py` cites and the fleet-optimization evidence stay an accepted residue. A `TOOLING_READS`
entry under an INERT path is refused by the INERT LANE audit as dead, and a `READ` entry would send every CONFORMANCE.md edit to the full
gate, which is what PB4453 removed; CI's Linux audits job runs those self-tests in each merge group.

Rows re-verdicted: none (process and tooling notes). Self-review: architecture (one prompt sentence, one prefix constant, one row-number
grammar, one hook for every role), full code (the prefix was fire-tested against `src/` with equal counts; the hook against the three path
spellings), performance (the prefix is linear: its alternatives start on disjoint characters), duplication (the lane's sentence and the
workflow's are pinned together; the prefix copies are held to one constant by check 0).

