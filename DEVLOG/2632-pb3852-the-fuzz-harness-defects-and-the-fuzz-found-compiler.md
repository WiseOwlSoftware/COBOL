## Entry 2632 — 2026-10-26 16:15 PDT — PB3852: the fuzz harness defects and the fuzz-found compiler notes become one campaign due before the night's run

The owner handed the operator lane the open fuzz-found compiler notes (2026-10-26 ~16:00, "Give them to the operator")
and the fuzz harness defects (~16:05, "We want as many fixed as possible before tonight's fizz run"). None of them sat in
a cluster, so neither the fix lane (none is a wrong answer or a crash except PB3839) nor a campaign could plan them.
PB3852 is the campaign's lead (a `decision` note with the owner's two standing requirements); the harness notes PB3537
and PB3561 are its tier 1, and PB3839, PB3569, PB906 and PB3468 its tier 2.

Five of the six notes had no `**Sites:**` block a campaign requires (`work.py check`), or one the reader could not
parse (PB3561's put its prose inside the code span). A locator found the members; where the fix is still a judgment
(PB3569's spec question, PB906's two candidate mechanisms, PB3468's open analysis) the block claims every candidate, so
the partition holds more rather than less. The wave was planned from this branch (`plan_wave.py --cluster PB3852`,
wave 20261711, four groups; PB3569 follows group C on GeneralFormatViolationTests.cs in the next wave).

