## PB4505: the quota estimate reads the live meter the status line already receives, for each account

Before a dispatch the orchestrator anchored its quota estimate on a month-old reading, or opened the claude.ai usage
page in Chrome. On 2026-11-03 the estimate said 19 % of the week and the page 29 %. Yet every Claude Code session hands
its status line command the live `rate_limits` at each refresh, and the installed `ccstatusline` shows them.

`scripts/statusline_tap.py` now sits in front of `ccstatusline`. It writes the record's `rate_limits` to the session's
own config dir (`rate-limits.json`, one per account) and passes stdin through unchanged; a failure in the tap is
skipped, so the status line always renders. `budget.py` takes that account's file, when it is under 30 minutes old, as
the newest meter reading. Both accounts are on Ultra, which has no five-hour window, so a missing `five_hour` row is
session 0. Both accounts' `settings.json` point their status line at the tap.
