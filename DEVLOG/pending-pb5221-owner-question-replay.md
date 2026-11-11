## PB5221: the loop replays an answered owner question on every restart

The operator restarted the loop at 18:33, once PB5220 had put the new toolchain on main. The loop stopped 25 s later and
wrote the 17:43 pwsh question to OWNER-QUESTIONS.md a second time, though the owner had answered it at 17:40.
`next_unit.choose` sends the loop to `owner-question` whenever `handoff.last.json` carries one. Nothing consumes that
question once it is written or answered. kb/Work PB5221 records this and the shape of the fix: rule 1 decides from
OWNER-QUESTIONS.md, which is the question's one home and records the answer. The restart went through with an operator
steer, which the planner checks before rule 1.
