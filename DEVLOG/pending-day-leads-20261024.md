## The open leads of the day's hand-dispatched fixes filed (PB3695 to PB3700) and a PB3367 recurrence

The operator filed the leads that the branch reviews of PB3670 and PB3666 left as low findings. PB3695 and PB3696 are in
PB3670's partition: its measurement counts note ids and branch names as files, and in-wave chaining compares only the
primary file. PB3697 and PB3698 are PB3666's boolean store: the bypass list in three documents is short by one, and one
predicate exists in two copies. Two process defects came from driving the owner's order to land all pending work.
PB3699: the drain planner gives no finisher to a group a fleet stop left SPLIT, so wave 20261031's groups A, C and D were
dispatched by hand. PB3700: no allocator hands out a wave number to a hand dispatch, and the operator's report number
20261104 collided with the loop's next wave. PB3645's landing failed halfway through `devlog.py stamp`, which is open
PB3367; that note gains the case. The operator restored the pending entry and landed the branch by hand.
