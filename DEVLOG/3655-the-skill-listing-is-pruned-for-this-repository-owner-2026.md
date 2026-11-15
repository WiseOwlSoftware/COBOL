## Entry 3655 — 2026-11-14 22:34 PST — The skill listing is pruned for this repository (owner, 2026-11-14)

The owner ran `/skill-doctor` for account 2 on 2026-11-14 and decided, on its usage table, what the model no longer
needs to be told about in this repository. `.claude/settings.json` now carries the decision, so every checkout,
worktree, loop unit and subagent of this repository gets the shorter listing:

- **`superpowers@claude-plugins-official` is disabled here.** Its thirteen skills cost about 860 tokens of listing on
  every turn and its session-start hook injects a block that orders a skill invocation before any action, which
  competes with this project's own process (CLAUDE.md, the `workstream` and `session-start` skills). Its last use on
  the account was more than thirty days ago (three uses in all).
- **Eight `brent-tools` skills are `user-invocable-only`** (`automating-agent-guardrails`, `claude-cloud-sessions`,
  `cross-platform`, `devlog`, `engineering-standards`, `roslyn-analysis`, `spec-compliance-audit`,
  `variant-analysis`): never invoked on the account, about 1,370 tokens of listing a turn, and no file under
  `.claude/`, `scripts/`, `CLAUDE.md` or `PROMPT.md` names one of them. The owner can still type them; the model no
  longer sees them. The `brent-tools` skills the project's own skills build on (`agent-fleet`, `review`, `test-gate`,
  `spec-oracle`) and the ones in use are untouched.
- **`anthropic-skills:xlsx` and `obfuscate` are `off`** here (a claude.ai-synced skill and a user skill of another
  project, neither used in this repository).

The seven project skills and workflows the same table lists as never invoked (`kb-sync`, `land-a-fix`,
`new-construct`, `fuzz-triage`, `spec-grammar-conformance`, `spec-grammar-impact`, `spec-reconcile`) stay: the
registrar and golden-lane briefs and the session-start hook name the first three, and the other four are the named
workflows of `.claude/workflows/`, which the Workflow tool runs without counting a skill use.

A setting takes effect in sessions started after it; nothing in a running session changes.

