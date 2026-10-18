## Entry 2203 — 2026-10-18 05:18 PDT — kb/Work PB2294 gains its second witness: the planner routes the PB3330 storage engine to Sonnet, and naming Opus at dispatch cannot override it

The PB3330 design report's second lead said `plan_wave.py --cluster PB3330` plans PB3331 to PB3336 with
`model: default`, which `model_rules.json` resolves to Sonnet, and PB3330's Route answered it by saying the Mythos lane
names Opus at dispatch through the rolling wave's `implementer_model`. That answer does not hold. `as_group_json` writes
`"model": "sonnet"` into every group, and `wf_rolling_wave.js` dispatches `g.model || IMPL_MODEL`, so a group's own model
overrides `implementer_model`. The enqueue unit verified both sites at main `642d403f7` and warned the operator at 04:59
PDT. The operator relayed the warning to the Mythos lane, which confirmed that its planner script already rewrites
`model: opus` into every group of `groups.json` and the wave args. Wave 1118, the first engine wave, runs on Opus for
that reason alone.

The mechanism is the one kb/Work PB2294 already holds: no note can declare its model, so `choose_model` routes by
patterns alone. In PB2294's first witness this routes the Delete program UP a tier. Here it routes a crash-consistent
storage engine DOWN one. PB2294 now carries this second witness. Its fix shape covers both directions and deletes the
hand rewrite, and its test also pins the group-over-wave precedence. PB3330's Route sentence is corrected to say how
the engine waves actually reach Opus.

