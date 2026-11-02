## PB4421 filed: the wave planner gives every group three codes, whatever its notes need

Wave 20261819 sent group A (lead PB2103) with three diagnostic codes for the second wave in a row. PB2103 part 2 and
PB2104 need about eleven REPO codes (DESIGN-external-repository.md §14), so the implementer finished the DataBinder
notes it could, gated GREEN at 296e68ed3, returned the three codes unused and came back SPLIT. `plan_wave.py` takes
one rules value, `wave.codes_per_group`, for every group and never reads what a group's notes need. kb/Work PB4421
records the defect, the witness and two ways to fix it. This change files the note only, with no code change.
