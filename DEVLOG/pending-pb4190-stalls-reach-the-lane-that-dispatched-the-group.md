## PB4190: a stall message reaches the lane that dispatched the group, not always the operator

**Re-probe.** The live ledger (`E:\COBOL-coord\inflight-groups.json`) held 18 groups and none carried a `lane`;
`progress.post_stalls` sent every stall to `to="operator"`. That is the 2026-10-30 report: six `STALL w20261780<x>`
messages about the Mythos lane's wave woke the operator, who can neither act on those agents nor speed them.

**Fix.** The ledger entry now records who dispatched it. `mailbox.account_lane` is the one rule of which lane a
process belongs to (the config dir `operator-session.json` names is `operator`, any other `mythos`); `mailbox.lane`
(whose inbox a session answers) is that rule minus the loop unit, which owns no inbox but does run under an account.
`dispatch_ledger.record_dispatch` takes a `lane`, and a group recorded again keeps its first lane, as it keeps its
`at`, so one lane's re-render never takes the other's group over. `make_dispatch_specs.py#record`, the ledger's one
caller, passes `mailbox.account_lane`. `progress.py` stamps each stall with `stall_lane` (the entry's lane; an entry
recorded with none, as every one in flight at landing time, goes to the operator) and `post_stalls` sends to that
inbox. `stall_line` is split out of `stall_lines` so the poster reads the stall, not a re-parsed string.

**Arms.** `progress.py --self-test` runs a ledger with an operator group, a Mythos group and groups with no lane and
reads both inboxes; `test_plan_wave.py` covers the lane recorded, kept on a re-render, and absent; `mailbox.py
--self-test` covers `account_lane` of a loop unit. `DESIGN-orchestrator-loop.md` (progress line, ledger row) is current.
No inventory row is touched.
