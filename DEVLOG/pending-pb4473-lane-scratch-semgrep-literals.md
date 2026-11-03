## PB4473, PB4474, PB4475: a lane agent writes in its own scratch directory and a hook enforces it; the semgrep comment prefix reads whole literals and each rule is proven inside an unread span; the PB4453 loose ends

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
