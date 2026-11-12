**The step order after the adversarial review (2026-11-12; supersedes the order in `2026-11-12-design.md`).** The
design is `docs/rearchitecture/DESIGN-orchestrator-loop.md` section 16 (section 16.10 holds this table with each step's
claim and oracle); `python scripts/spec/work.py next --cluster PB3726` is the order's one reader. ONE strictly serial
`blocked_by` chain, one step per wave, the case layout first; no pairs (the reasons are in section 16.10 and
`2026-11-12-design-review.md`, F6).

| Tier | Note | Step |
|---|---|---|
| 2 | PB5428 | the case layout with plan_wave's cases (supersedes PB3729) |
| 3 | PB5395 | the conformance reader |
| 4 | PB5396 | PublicAPI.Unshipped.txt in one order |
| 5 | PB5429 | the conformance document made regular in place (with PB5430, supersedes PB3738) |
| 6 | PB5430 | the conformance storage cut, stamp and guard, citation arm, every pointer moved |
| 7 | PB5431 | the published conformance statement |
| 8 | PB5432 | gate's cases out, `gate_fake_host.py` (supersedes PB3732) |
| 9 | PB5433 | gate module split (supersedes PB3733) |
| 10 | PB5411 | the `Gate` class split |
| 11 | PB5397 | inert_lane's cases out |
| 12 | PB5398 | inert_lane split |
| 13 | PB5434 | land_lane's cases out (supersedes PB3736) |
| 14 | PB5435 | land_lane split, `coord.Say` (supersedes PB3737) |
| 15 | PB5399 | audit_batch's test |
| 16 | PB5436 | enqueue's cases out (supersedes PB3734) |
| 17 | PB5437 | enqueue split (supersedes PB3735) |
| 18 | PB5438 | prune_worktrees' cases out (supersedes PB3730) |
| 19 | PB5439 | prune_worktrees split (supersedes PB3731) |
| 20 | PB5400 | self_tests.py's cases |
| 21 | PB5401 | `scripts/a*`–`d*` and the docchecks delegates |
| 22 | PB5402 | `scripts/e*`–`l*` |
| 23 | PB5403 | `scripts/p*`–`s*` |
| 24 | PB5440 | `scripts/t*`–`v*`, the two `test_*` tools renamed |
| 25 | PB5404 | `scripts/spec/a*`–`d*` |
| 26 | PB5405 | `scripts/spec/e*`–`l*` |
| 27 | PB5406 | `scripts/spec/m*`–`w*` |
| 28 | PB5407 | `scripts/orchestrator/[a-r]*` |
| 29 | PB5441 | `scripts/orchestrator/[s-z]*` and its standalone tests |
| 30 | PB5408 | arch, hooks, semgrep, models, the harness, `.claude/` |
| 31 | PB5409 | the end state |

Retired 2026-11-12 (frontmatter only; each `closes_rows_reason` names its successor): PB3729, PB3730, PB3731, PB3732,
PB3733, PB3734, PB3735, PB3736, PB3737, PB3738. PB4053 waits on PB5430, which retires it. PB5410 (analysis) is outside
the chain.
