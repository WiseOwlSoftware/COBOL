## PB3773 and PB3774: CI's budget is 60 jobs on GitHub Team, and bounces belong to the lane that dispatched them

The owner upgraded the org to GitHub Team after CI was measured saturated at the Free plan's 20 concurrent jobs;
PB3773 updates the docs that state 20 and re-measures the merge queue's build width against 60. PB3774 files why
three actors re-armed the same branches today: the landing lane hands every bounce to the loop's enqueue unit,
whichever lane dispatched the branch; the dispatch ledger already knows the owning lane.
