## The skills and briefs, measured and redesigned for fewer tokens

The owner, 2026-10-25 ~15:40 PDT: "Let's devote some Mythos time to evaluating and optimizing our skills for less
token use for more gain, where meaningful", with the Mythos work allowed past the day's quota. A Sonnet locator
measured 24 hours of telemetry (both accounts, $849), the 105 subagent transcripts of the operator session and the 46
loop-unit logs; the Mythos analyst spot-checked the numbers it built on and designed the changes. The record is
`docs/rearchitecture/evidence/fleet-optimization/2026-10-25-skills-token-optimization.md` (+ `.json`); the notes are
kb/Work PB3776-PB3782 in the PB1957 campaign, the first three at tier 1. Nothing was edited but the record, the
notes and this entry.

What the measurement overturned: the hypothesis that the instruction files are the cost. MANDATORY-PRACTICES.md is
1.27 % of agent USD; all instruction and brief files together 3.6 %; no agent ever loads a skill (two reviewers
loaded brent-tools:review). The cost is the 55K-token turn-1 baseline, re-read on every turn (24 % of agent USD), and
the transcript's own growth (54 % of an implementer's input). Inside the baseline, every role's first request carries
the same 16 tool schemas, and four tools no agent ever called (`Artifact` at 37.9K chars, `SendFeedback`,
`ReportFindings`, `ListAgents`) are 47.1K of the 71.9K chars: 12-17K tokens a turn, $220-310 a week (PB3776, a
`tools:` allowlist per role with a drift test). Then: the implementer reads a spec and a brief that restate each other
and four other places ($90 a week, PB3777 folds the brief into the rendered spec and deletes it); the briefs say "read
MANDATORY-PRACTICES WHOLE", which the harness cuts to a 2 KB preview for implementers (16 of 22 saw only that and
grepped their row ids) and which finishers read twice whole for rows worth 4K tokens (PB3778: the renderer slices the
role's rows into its dispatch and `check_practices.py` proves none is dropped); MEMORY.md's 19.5K chars ride every
agent turn ($75 a week, PB3779); the loop units load agent-fleet + workstream + session-start, 12.4K tokens a unit,
for a base no agent loads and an overlay that restates it ($50 a week, PB3780); CLAUDE.md's Testing paragraph is the
gate rule a fourth time and PROMPT.md §5 still describes a lander ($20 a week and a rule-6 correction, PB3781); and
P13's LSP rule cannot be followed as dispatched: the tool is deferred, no spec loads it, 0 of 105 agents used it, the
checker enforces the words (PB3782).

The owner's Stats question is answered in the record: agent-fleet's 277.7m tokens are the label inherited by every
agent of the workflows a loaded thread launched ($263.5 of $267.9 third-party dollars sit in such sessions); the load
itself costs $2.57 a day. P10's reading list was followed by 4 of 27 implementers and no finisher; followed as written
it would cost each implementer about 25K tokens at turn 2, so PB3778 turns it into a two-line checklist in the spec.
Together the notes are worth about $550-640 a week of a $5,900 week, 9-11 %; one more lever, about $65 a week, is the
owner's setting (disable the never- or rarely-invoked skill listings for this repository) and was asked as a bare
question, not filed.
