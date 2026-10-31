## PB4213 filed: an UNPARTITIONED group is planned beside running work, and PB1654 gets its site block

At 09:36 PDT on 2026-10-31 the wave unit planned wave 20261784 for campaign PB2911 and got three groups. Group B
(PB1654) was flagged UNPARTITIONED because its `**Sites:**` line read `none`. Its audit covers the PICTURE tests under
`Binding/`, and that population includes `DataBinder.cs`, which the running Mythos branch c21-12 edits. The owner's rule
says no plan with an UNPARTITIONED group is dispatched. The planner cannot leave a group out, so the unit released B from
the dispatch ledger and launched groups A (PB1253) and C (PB107) from a trimmed copy of the args.

kb/Work PB4213 (cluster PB1957) records the gap. The fix's shape is to have the planner list such a group as waiting
(`needs a **Sites:** block`) instead of planning it, to make the wave unit's text agree, and to sweep the other open
`Sites: none` notes. This change also gives PB1654 a glob site block over `Binding/**/*.cs` (read as a sweep, PB3721),
so the next plan holds it until c21-12 lands. This landing changes only the register.
