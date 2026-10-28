## Entry 2691 — 2026-10-28 10:41 PDT — PB3330's close-out runs on Opus: the owner routes it away from Fable

All ten groups of the RELATIVE and INDEXED storage redesign have landed, which leaves only PB3330 open, the campaign
lead itself. On the morning restart the planner held it because its `frontier_model: fable` field (PB3941, set for the
owner's Fable design rounds of 2026-10-17) requires the owner's approval for each Fable dispatch. Asked whether to
approve Fable for the close-out or send it to Opus, the owner chose "Route to Opus". So the field is deleted. The note
carries the `NEEDS-OPUS` marker, the `routing.opus_when.body_patterns` entry that routes a group to Opus. Without it,
the note's `runtime/io` area would route it to the implementer's default model, Sonnet. A planner dry run now plans
PB3330 with "model: PB3330: an open design question ((?i)NEEDS-OPUS)". A note that declares its own model would be the
structural fix, which stays PB2294's.

