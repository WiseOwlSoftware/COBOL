## PB5357: the owner withdrew every quota borrow; the instructions of both lanes now say so

On 2026-11-11 the owner told the Mythos session: "I also revoked the account 2 quota borrow, so not it must wait until
the days catch up. I am doing the same here. Usage on both accounts should evenly scale to exhaust the quota the late
evening before the quota reset", that Mythos on either account needs his explicit approval, and then: "Reconcile all
instructions across you and operator with the new quota spend guidelines".

A sweep for every statement of a borrow or a weekly pace in the instruction-bearing files found seven that still granted
or implied one: plan section 0 twice (`-BorrowDays 6` under PRIORITY, `-BorrowDays 3` in the 2026-10-06 paragraph), the
workstream skill, MANDATORY-PRACTICES O3, the orchestrator-loop and architecture-review designs, and `r2_cost.py`'s
docstring. Each now states the rule or points at kb/Work PB5357, the decision note, and `CLAUDE.md` carries the plan in
force in four lines under Ownership. `model_rules.json` needed no change: `daily_pct` 14.3 and the 97 % dispatch cap are
the rule, every tool's borrow defaults to 0, and P1 already requires approval for each Fable or Mythos dispatch.

The sweep also showed why a stale sentence was dangerous: a borrow is a bare command-line argument with no source and no
end, so any session reading the old plan could have granted it again. kb/Work PB5358 files that: the borrow belongs in
a dated quota override that carries the owner's words and lapses by itself, as the weekly cap already does.

Outside the repository the same rule went into both lanes' memory, the week's quota plan and a mail to the operator.
Account 1 stood at 87 % on day 4 and waits until Saturday 02:00 PST; this landing is the owner's direct request.
