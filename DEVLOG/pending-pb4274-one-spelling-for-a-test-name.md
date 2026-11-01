## PB4274: the population check compares test names in the trx logger's spelling

The trx logger rewrites every UTF-16 code unit outside XML's character range as `\u` and four hex digits, so a theory
case whose argument holds a supplementary-plane character is listed literally by `--list-tests` but named with two
escaped surrogates in the trx. The population check compared the two spellings, so every such case read NEVER RAN and
NOT IN THE POPULATION on any plan that put it in leg 1. Whole-tier-1 plans did, and two front-end campaign branches
(PB4230, PB4302) gated RED only on that. Now `test_population.py#trx_spelling` and its C# twin
`GateNameKey.TrxSpelling` put a name in the logger's spelling as the name key's first step, so both sides meet. The
pipeline redesign's whole-population gate needs the same rule.

The fix was written on `worktree-wf_2201113e-54d-26`, whose PB4240 half the branch review refused. It lands on its own
here and answers that review's two low findings on it: the escapes are written as ASCII text in the design doc, the
note and `GateLegs.cs`, and both spellings take a compiled-regex fast path, so a gate no longer tests every character of
every listed, mapped and timed name. The branch's added CI population assertions were left out. The owner ruled out
added CI time, and the redesign deletes `test_population.py` (step 5) and is rewriting that workflow (step 7). The fix
closes kb/Work PB4338, the same defect filed from the PB380 report.
