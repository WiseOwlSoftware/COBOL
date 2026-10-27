## PB3941: the frontier-model hold reads a field, not prose, and the owner's approved fix PB3869 can be planned again

The owner approved the fix of PB3869 at about 10:10 PDT, in the Mythos session. PB3869 is a wrong answer: an arithmetic
statement with only a NOT ON SIZE ERROR phrase takes the SIZE ERROR phrase's disposition. The operator's handoff told
the next wave to plan it first. Wave unit 20261723 planned no groups. The planner listed PB3869 as waiting for "the
owner's approval of a frontier model".

The cause was the planner's frontier-model hold. It was one regex over the note's body, `mythos` or `fable` within 160
characters of `approv`. PB3869 records the owner's decision as given "in chat (Mythos session ef566665): \"Approve
fixing PB3869\"", and the regex matched it. The owner works in a session and a lane named after the Mythos model, so
any note that records an approval given there looks like a note that needs Mythos. Over the open register the regex
matched eight notes. Four were false (PB3869, PB3765, PB3836, R69) and one was historical (PB3160, a Mythos review that
ran on 2026-10-14). Three were true: PB3330, whose design rounds the owner gave to Fable, and PB3726 and PB1754, whose
work the owner gave to a Mythos model and the Mythos lane.

Whether a note's work runs on a frontier model is a fact about the note, so the note now states it in its frontmatter:
`frontier_model: fable | mythos`. `model_rules.json` names the families in `routing.frontier`, which replaces the
regex (deleted, no compatibility layer). `coord.frontier_models()` reads that list and `clusters.Note` carries the
field. `plan_wave.py` holds a lead whose field is set and names the model in its waiting line, and `work.py check`
refuses a value that names no frontier family. The three true notes carry the field, each with a paragraph giving the
reason, so their holds are unchanged.

`test_plan_wave.py` now states the field in both frontier fixtures and adds the PB3869 case: a body that names Mythos
beside an approval, with no field, plans exactly as a note without that body. With the old regex put back, that check
alone fails. The wave unit fixed this on its own branch and launched no fleet, as its unit rules require when a planner
defect makes the plan unusable. The next wave can plan PB3869.
