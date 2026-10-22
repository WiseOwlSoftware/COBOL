## PB3379 recurs: the loop and the Mythos lane both sent a finisher to one blocked branch

The Mythos lane's PB3511 branch went red at its landing (Linux, one golden), and both the Mythos session and the
loop's enqueue unit dispatched a finisher for it. The two fixes stacked without loss, but both accounts paid for one fix
and two agents wrote one worktree. It is the class PB3379 already records: a branch has no owning orchestrator, so two
lanes work it at once. The note gains this recurrence and one case for its fix: a blocked report is finished only by
the lane that owns the branch, and a finisher dispatch records a claim that every other dispatcher reads first.
