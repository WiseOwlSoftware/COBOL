## Entry 2656 — 2026-10-27 10:13 PDT — PB3868: cobol-implementer runs Sonnet by default, measured cheaper per landed note; the default has one home

The owner asked to put `cobol-implementer` on a cheaper model, then added: "Ensure that any change is cost-effective. A
cheaper model isn't cost-effective if it fails." A locator measured 789 groups (1,060 implementer agents, 2026-09-27 to
10-27; `docs/rearchitecture/evidence/fleet-optimization/2026-10-27-implementer-model-cost.md`). From 10-17, where landing is
measurable, Sonnet groups cost 0.16 weekly points per landed note against Opus's 0.27, with reviews, fix rounds, finishers
and retries charged in. They landed 95 % against 94 % and reached DONE on the first pass 92 % against 82 %; `runtime/io`,
the one area with both, gives 0.16 against 0.21. Opus gets the harder groups by routing, and Sonnet never ran grammar, OO or
CI/build work, so `routing.opus_when` stays as it is.

The planner already defaulted to Sonnet, but four places stated the default and disagreed: `model_rules.json`
`routing.default` and `wave.implementer_model` said Sonnet, while the agent definition (`model: opus`, every hand
dispatch's default) and `wf_rolling_wave.js` (`implementer_model || 'opus'`) said Opus, and P1 and O4 called the implementer
an Opus role. Now the agent definition's `model: sonnet` is the one home: `coord.agent_model` reads it for the planner and
the ledger's estimate, `model_rules.json` keeps only the Opus routing, the rolling wave passes a group's own routed `model`
or none, and a drift test in `test_plan_wave.py` holds the default to the definition. Decision note kb/Work PB3868.

