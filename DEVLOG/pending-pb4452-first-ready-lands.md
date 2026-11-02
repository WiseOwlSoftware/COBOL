## PB4452 follow-up: the landing check lets the first ready branch land

`landing_check.py` check 2 stopped the LATER-dispatched of two branches whose edits met on a member, even when the
earlier one was idle and unfinished. The reviewed CLEAN ReferenceResolver chain (w20261827a, PB2299 and PB2371-PB2375)
was refused this way because of operator branch `worktree-wf_a45e0bcf-285-1`, which was SPLIT, unreviewed and idle for
3 hours.

Now a meeting stops nothing. The landing proceeds, and the warning names the other branch, which merges main after this
one lands. A real conflict goes to that branch's finisher (MANDATORY-PRACTICES L12).

The self-test's eleven "the later branch STOPS" expectations are rewritten as proceeds-with-a-warning, and the binding-hint
check K3b' is deleted. The self-test passes 45/45.
