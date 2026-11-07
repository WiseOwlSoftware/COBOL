## Entry 3227 — 2026-11-06 17:58 PST — kb/Work PB4856: each WHEN phrase of an EVALUATE is its own USE FOR DEBUGGING moment

PB4856 (a review lead of PB4810): consecutive WHEN phrases over one body were composed into one `BoundEvaluateWhen.Match` (an OR), so the
per-phrase "evaluated" flag of the debugging planner covered the whole OR. With `WHEN 1 WHEN D` and K = 1 the OR short-circuited at the
first phrase, yet D's ALL REFERENCES section ran. X3.23-1985 USE FOR DEBUGGING rule 5: an operand in a phrase that is not evaluated does not
run the section; ISO §14.9.13.4 GR4 processes each WHEN phrase left to right.

Fix: `BoundEvaluateWhen` holds `Matches`, one condition per WHEN phrase; `BoundEvaluate.PhraseOperands` flattens them across the statement;
`EvaluateEmitter.Emit` marks each phrase inside its own condition and ORs them in source order. `UsageCollectionPass` walks every match.
No diagnostic code used. Golden `pb4856-debug-evaluate-consecutive-when` (accept 85: first phrase selects, second evaluated, ALSO subjects,
first phrase names D) and the 2002 refusal `pb4856-debug-evaluate-phrase-removed`. No inventory row closed. Sibling sweep: SEARCH's WHENs
were already one phrase each (`BoundSearch.PhraseOperands`); no other node ORs phrases.

