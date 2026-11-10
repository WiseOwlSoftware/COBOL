## Entry 3400 — 2026-11-09 18:45 PST — PB5058: the quota estimator prices Fable at 2x and Sonnet at half the Opus cost (the 2026-11-09 price page)

The owner pasted the current prices. Fable 5.2 costs twice Opus 5.5 per input, output and cache-write token (cache reads
cost the same), Sonnet 5.5 half, and Haiku 5.5 an eighth at the prompt sizes agents use. `model_rules.json` still carried
the 2026-10-06 ratios (Fable 2.5x, Sonnet 1/2.5), so the weekly-quota estimate overstated a Fable dispatch by a quarter and
understated a Sonnet group by a fifth. The rates are now Fable 800,000, Sonnet 3,200,000 and Haiku 12,800,000 counted
tokens per weekly point and Mythos 800,000 (Mythos 5.2, now in the model selector, is priced as Fable 5.2) beside Opus's 1,600,000, with the prices and date in the source string, and MANDATORY-PRACTICES
P1 points at that file instead of restating an older figure. `test_budget.py` passes 60 of 60.

