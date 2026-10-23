## PB3528 follow-up: a one-sided overlap between two finishers also chains them

The branch review of the PB3528 fix found a gap. If the first finisher overlaps the second one's branch but not the other
way round, the first went ahead and the second never linked back, so the two ran side by side on the shared file. The
planner now records, when a group goes ahead past a finisher not yet placed, that the finisher must follow it. A new
planner check covers the one-sided case: it failed before this change and all 338 checks pass after it.
