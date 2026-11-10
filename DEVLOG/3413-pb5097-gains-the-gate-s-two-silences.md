## Entry 3413 — 2026-11-09 23:51 PST — PB5097 gains the gate's two silences

All ten STALL mails of wave 20261973 (2026-11-09 22:05-23:45) were false. PB5097 now records the two mechanisms beyond the
queued group: a gate's test phase writes nothing to its redirected log, and a gate waiting for a slot writes only when its
place in line moves (C waited 1601 s with no line). The fix it asks for is a gate heartbeat that `group_activity` reads, with
self-test cases for both. Notes only.

