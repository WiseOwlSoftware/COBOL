## PB4270 retired: the pipeline redesign removes the impact maps it would have kept fresh

The owner adopted Mythos's clean-sheet pipeline redesign (kb/Work PB4323) on 2026-10-31. Its step 5 (PB4328) deletes
the impact maps, the impact lane and the planner, and a code change's local gate runs the whole Windows population. A
gate that asks for a fresh map has nothing left to serve. PB4270 is retired, and its parked branch
`tooling-pb4270-map-freshness`, never landed, is abandoned and deleted. That branch's file set had held migration steps
2, 3, 4, 5, 6 and 11 out of every wave. This landing changes only the register.
