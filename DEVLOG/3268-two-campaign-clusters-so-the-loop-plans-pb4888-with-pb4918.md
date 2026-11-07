## Entry 3268 — 2026-11-07 13:25 PST — Two campaign clusters, so the loop plans PB4888 with PB4918, and PB4892 with PB4894

The fix lane skips process_only notes. Twice on 2026-11-07, a wave unit declined the operator's steer to add PB4888 and
PB4918 as a hand group, because a campaign is planned and `groups.json` is never hand-edited (rolling-wave.md). So the
notes now name clusters: PB4888 and PB4918 are `cluster: ["PB4888"]`, and PB4892 and PB4894, whose harm is still
unknown, are `cluster: ["PB4892"]`. The loop runs them with `orchestrate.ps1 -Cluster PB2293,PB4888,PB4892`. The site
lines that `plan_wave.py --dry-run` refused now name the members (PB4918) or claim the template whole with a reason
(PB4888).

