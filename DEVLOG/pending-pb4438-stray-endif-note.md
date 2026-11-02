## PB4438 filed: a stray END-IF directly after a recovered one is swallowed

The enqueue unit filed `kb/Work/PB4438` from wave 20261823 group A's report (`w20261823a-PB4299-report.md`, New
defects 1). The clerk batch of PR #1022 left this lead out because it read the lead as fixed by R1-1, but the report's
round-2 Self-review keeps it as a known limit. When a recovery consumes an IF's END-IF, a stray END-IF directly after it
is swallowed by `CobolErrorStrategy.Resynchronize` and draws no COBOL0306, on origin/main as on the PB4299 branch. The
note gives both witnesses and the fix direction. No code changes.
