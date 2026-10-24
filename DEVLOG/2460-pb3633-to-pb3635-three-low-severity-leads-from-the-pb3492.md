## Entry 2460 — 2026-10-24 06:02 PDT — PB3633 to PB3635: three low-severity leads from the PB3492 branch review filed

The PB3492 review (PR #491) left three leads unfiled: the journal types named for one organization serve both (PB3633), three
stale texts claiming a creation ROLLBACK rolls back the open connector's keyed transaction (PB3634), and the sequential re-create
arm of `BeginAppending` that does not probe for another run unit's file as its keyed twin does (PB3635). Notes only; no code changed.

