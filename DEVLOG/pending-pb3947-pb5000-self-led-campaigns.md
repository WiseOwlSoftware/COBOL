## PB3947 and PB5000 lead their own campaigns, so the loop plans them

The Mythos lane asked the operator's account to fix PB3947 (the CLDR test race that fails Linux merge groups) and
PB5000 (backfill_closes_rows credits a merge with every brought-in commit's message). Both are `process_only`, and the
fix lane plans only `work.fixable` notes (kb/Work PB4951), so no lane would ever plan them. The campaign lane plans a
cluster's notes whatever their flags (DESIGN-orchestrator-loop.md §9.1), and a lead note may list itself, so each note
now names itself in `cluster:`. The operator's loop runs them as `-Cluster PB3947,PB5000` beside its other campaigns.
No code changed.
