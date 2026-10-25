# 2026-10-25 — the skills and briefs, measured for tokens paid against tokens used

> Frozen evidence (kb/Work PB785). Raw data: `2026-10-25-skills-token-optimization.json` (the measurement's tables
> and the analyst's spot checks). The measurement itself (`measure.md`, `measure.json`, the locator's scripts) was in
> `E:\COBOL-coord\scratch\mythos\skills-opt\` and is not kept. Analyst: the Mythos model (owner approval 2026-10-25
> ~15:40 PDT: "Let's devote some Mythos time to evaluating and optimizing our skills for less token use for more gain,
> where meaningful"; "The mythos work is allowed to draw past today's quota limit"). The decisions are the kb/Work notes
> PB3776-PB3782; nothing was edited here.

## Question

The owner's /plugin Stats tab (7 days, this machine) attributes 277.7m tokens to `brent-tools:agent-fleet` over 37
loads and 26.8m to `spec-oracle` over 4, and lists seven brent-tools skills and three superpowers skills never invoked
whose one-line listings still cost every turn. The owner asked which skill and brief content earns its tokens: "every
agent should load what its ROLE needs for THIS task, once, and nothing it never uses", ranked by tokens saved per
week, skipping anything under about 1 % of a role's input.

## Hypothesis

Stated before the measurement: the instruction preamble (MANDATORY-PRACTICES at 67 KB, the public skills P10 names,
the two implementer files) is a large share of an implementer's input, and a role-sliced render would cut it.

## Design

A Sonnet locator measured, read-only: telemetry api_request events for 24 h (2026-10-24 22:40Z to 2026-10-25 22:42Z,
14.3K calls, both accounts, real `cost_usd`); the 105 subagent transcripts of the operator session (11 workflows);
46 loop-unit stream logs; the main transcript. Prices per model were fitted to the telemetry by least squares (fit
check on the 105 agents: $233.31 by transcript against $232.84 by telemetry). The analyst then spot-checked the numbers
it built on against the same transcripts: the turn-1 tool schemas per role (from each role's first
`api-request-shape`), which prescribed commands each role actually ran, which `E:\claude-skills` files implementers
read and at which turn, and how MANDATORY-PRACTICES was accessed.

## Attempts

One measurement run and one spot-check pass. One correction to the measurement: its per-file table keyed on exact
paths and so showed one or two P10 reads; a pattern over every `claude-skills/(skills|agents)/` path found 25 of 27
implementers touching the clone, almost all of it `orient.py` and `status_delta.py` RUNS (P6, P4), which are not skill
reads. The P10 figure below is the corrected one.

## Result

**Where the money is (24 h, $849.1; the loop account $565.6, the operator session $283.5).** Implementers are 41.7 %
(84 agents, median 67 turns). Cache reads and writes are 79 % of the transcripts' dollars, fresh input 0 %. The fixed
turn-1 context ("baseline", about 55K tokens, re-read from cache on every later turn) is 24.0 % of agent USD; the
instruction and brief files agents read are 3.56 % (instruction files alone 2.42 %); every file read of any kind
13.1 %. Baseline share by role: implementer 18.4 % of USD, reviewer 31.4 %, finisher 32.1 %, adjudicator 30.8 %,
locator 70.4 %. The hypothesis was wrong about the preamble files and right about the baseline.

**The baseline, decomposed (one implementer's first request, chars):** tool schemas 71,892 · skills listing 23,614
(about 60 skills) · MEMORY.md 20,544 · CLAUDE.md 14,521 · agent definition 4,389 · deferred-tool list 3,617 ·
environment 2,895 · task prompt 3,261 · git status 1,059. Every role's first request carries the SAME 16 tool schemas
(the reviewer's `disallowedTools: NotebookEdit` removes nothing, NotebookEdit being deferred anyway). The `Artifact`
tool's schema is 37,943 chars, `SendFeedback` 5,618, `ReportFindings` 2,314, `ListAgents` 1,227: 47,102 chars
(12-17K tokens, 22-31 % of the baseline) for four tools that none of the 105 agents ever called.

**What each role actually executes (agents whose tool calls contain it):** implementers (n = 27): the gate 27, `work.py
check` 27, `drift_rules.py` 27, `orient.py` 23, `semgrep/verify.py` 21, `cite.py --check` 19, `where.py` 15,
`status_delta.py` 5, `linux-gate.sh` 5, the LSP tool 0, `git stash` 1. Finishers (n = 7): MANDATORY-PRACTICES 7,
`merge_drivers.py` 6, the gate 5, `drift_rules.py` 2, every public skill 0, `cite.py` 0. Reviewers (n = 28):
`review_findings.py` 28, `drift_rules.py` 26, `cite.py` 16, the Skill tool 2 (brent-tools:review). Adjudicators:
`cite.py` 12 of 12, the spec 11; nothing else. Clerks and locators: their brief only.

**P10 (the public-skill reading list).** Of 27 implementers, 4 read any public `SKILL.md` (engineering-standards 2,
dotnet-engineering, spec-oracle and variant-analysis 1 each, and those three at `head` length, 98 tokens) and 4 read the
self-review agent files (`pr-test-analyzer`, `silent-failure-hunter`, `comment-analyzer`, about 6.2K tokens each),
at turns 159-160 of 197 in the two that read all three. Every finisher read none. The rule is not followed; followed
as written it would cost each implementer about 25K tokens near turn 2 (10 files), about 2.3M token-turns per agent.

**MANDATORY-PRACTICES (67.7 KB, 16.9K tokens).** The briefs say "read it WHOLE". The harness truncates a `cat` of it to
a 2 KB preview (16 of 22 implementers that touched it saw only that), so implementers then grep the rows the spec
cites; one agent's own grep enumerates the implementer's row set: P2, P4, P5, P10, P11, P13, I1, I2, I7-I11, R3.
Finishers read it whole (7 of 7, twice in 5 of them, 17,484 tokens per read; 11-13 % of a finisher's USD), for rows
that total about 4K tokens.

**The two implementer files.** `fix-lane-implementer-brief.md` (3,418 tokens) is read by 25 of 27 at about turn 2 and
the rendered spec (3,363 tokens) by 27 of 27; they restate each other (the checkpoint protocol, the gate paragraph,
the DEVLOG entry, I9, citations, the STOP rule) and both restate the agent definition, CLAUDE.md's Testing paragraph
and MANDATORY-PRACTICES I1/I2/I8. The gate rule exists in six places an implementer is told to read.

**P13 (the LSP tool).** No agent used it and none loaded it: `LSP` is a deferred tool in every agent's request, the
spec's P13 paragraph never says to `ToolSearch` it first, and `check_practices.py` enforces the paragraph's presence.
The rule is dead as dispatched.

**The 21 % "agent-fleet" and spec-oracle (the owner's question).** Telemetry labels every plugin skill
`skill.name=third-party` (31.5 % of the 24 h USD; 19-22 % on the operator account, the owner's 21 %). Agents never
load agent-fleet; the orchestrator threads do (22 of 46 loop units, the operator session at 10:54 PDT), and the label
is inherited by every agent of the workflows those threads then launch: $263.5 of the $267.9 third-party dollars sits
in sessions whose own thread loaded it (workflow wf_ba46c1d4, started a minute after the load, 27 agents, $53.2, every
agent tagged). The load itself costs $2.57 a day (23 loads of 4.5K tokens, priced over the turns that follow), 0.3 %
of the total; workstream $4.05, session-start $0.86. spec-oracle's four loads were four days before the window and
unmeasured here; 6.7m tokens per load matches the same inheritance (a long adjudication thread's every later turn),
not the 2.6K-token skill. The Stats tab's per-skill figure is the cost of everything done while the skill was active in
the launching thread, not the skill's cost.

**Turn shape (the quadratic law).** The operator session grew from 72K to 609K tokens of context over 414 turns; its
loaded skills are 3.2 % of its input, instruction files 8.1 %, the turn-1 baseline 19.3 %, the thread itself about
70 %. For an implementer: baseline 26 % of input, instruction files 4 %, the work's own growth 54 %. The cost of a
transcript is the sum over turns of its context, so turns × growth dominates, then the baseline × turns; the
preamble files are a rounding error. What changes the most, in order: fewer turns per agent (outside this brief), a
smaller baseline (the one skills-side lever worth more than $200 a week), then the duplicated and unread preamble.

**Savings per week, from the measurement** (agent baseline: 24 % of $610 agent USD a day = $1,025 a week for 55K
tokens, so $18.6 a week per 1K baseline tokens across the agents, about $4 more per 1K on the main threads):

| Change | Tokens | USD/week | Note |
|---|---|---|---|
| Drop the four never-called tool schemas from every role (`tools:` allowlist in the agent frontmatter) | 12-17K per turn, every agent | 220-310 | PB3776 |
| One rendered dispatch file: the brief folded into the spec, the overlap removed | 3.4K read at turn 2 by 25/27, plus ~2.5K duplicated | ~90 | PB3777 |
| MEMORY.md index trimmed to the standing anchors | 19.5K chars → ~6K; ~3.4K tokens per turn, agents and main | ~75 | PB3779 |
| Disable the never/rarely invoked skill listings for this repository | ~12K of 23.6K chars; ~3K tokens per turn | ~65 | owner's setting |
| Loop units load the slimmed overlay only, the base on demand | 11.2K → ~2.5K per unit; 46 units × ~45 turns | ~50 | PB3780 |
| Role-sliced practices rendered into the spec and the finisher brief; P10 as a checklist | finisher 11-13 % of its USD; implementer preview+grep turns | ~35 | PB3778 (and the rules reach the agent) |
| CLAUDE.md / PROMPT.md duplicates and the stale PROMPT.md §5 | ~1.1K tokens per turn | ~20 | PB3781 |
| P13 made followable (`ToolSearch select:LSP` in the spec) or re-measured | 0 | 0 | PB3782 |

Together about $550-640 a week against about $5,900 (the 24 h total × 7), 9-11 %. Below the 1 % threshold and not
proposed: the report template (931 tokens, read late by 19/27), the agent definitions (4.4K chars, used), the
environment and git blocks, the loop units' `session-start` load ($0.86 a day).

## Limits

Transcripts exist only for the operator session's 105 agents (one account); the loop account's agents (about 66 % of
the 24 h USD) have telemetry and unit-log `task_progress` only, so their roles are inferred and their preamble is not
measured, though they run the same agent definitions and briefs. Tokens of a read are the model-visible result
length / 4; the chars-per-token ratio of JSON tool schemas is given as a range (2.7-4). Remaining-turn math ignores
compaction. The label-inheritance rule for the Stats tab was inferred from per-workflow tag constancy, not from
harness documentation. Whether a `tools:` allowlist in an agent's frontmatter removes a tool's schema from the request
is the hypothesis PB3776 tests first, by one dispatched agent's `api-request-shape`. The price fit for Mythos is
unreliable (few events) and its dollars come from telemetry.

## Decision

The analyst filed PB3776-PB3782 (cluster PB1957; PB3776, PB3777 and PB3778 at tier PB1957:1) and asked the owner one
question (disabling the unused skill listings for this repository, a setting that is the owner's). No skill, brief,
script or public skill was edited by the analysis. The measurement that MANDATORY-PRACTICES costs 1.27 % of agent USD,
that agents never load the base skills, and that the baseline is 24 % was the reason the design turned from the
preamble files to the baseline.

## Links

kb/Work PB3776-PB3782; the DEVLOG entry of branch `design-skills-opt`; the measurement brief and the analyst brief in
`E:\COBOL-coord\scratch\mythos\skills-opt\` (not kept); the owner's Stats paste of 2026-10-25 ~15:50 PDT; kb/Work
PB1699 (the public base pin), PB2561 and PB3701 (the agent definitions), PB3027 (the branch review).
