## Entry 2565 — 2026-10-25 13:32 PDT — kb/Work PB3765: no fast lane for a fix to CI itself

The 2026-10-25 .NET download outage (PB3308) held main for about 1.5 hours. Its fix waited in the merge queue behind
entries the outage was failing, then ran the whole matrix under the runner limit. The owner approved an immediate
merge, and the session disabled the main ruleset, merged PR #594 and restored the ruleset; that also dropped two other
entries from the queue. PB3765 records the missing fast lane for CI repairs, the superseded runs that keep runners, and
the need for a bounded bypass that ejects nothing.

