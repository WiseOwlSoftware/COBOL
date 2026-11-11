## The controller's decisions on the stage 4 design (2026-11-10, Mythos session, under the owner's delegation)

The design (`2026-11-10-design.md` beside this file) was written read-only by an Opus planner against main at e322280e15
and the stated outcomes of PB5230, PB5231, PB5232 and PB5235. It asked for three decisions. All three are CONFIRMED.

1. **`land` checks the merged tree** with that tree's own `build.py`, replacing "the landing gates nothing and reads the
   head's record" (kb/Work PB4331). Reasons: PB5207, which the owner approved, says so; with the store a merge that touched
   no target's inputs costs nothing (11 of 15 measured landings), and the other 4 cost one test run of 250 to 500 s; a
   red caused by the merge is found before the queue instead of as a bounce; and it is what stage 6 needs, because a pass
   CI may accept has to be a pass at the merged tree's key. CLAUDE.md's Testing section changes with landing 4b: the
   merge group stops being the first run of the merged tree on Windows.
2. **`land` owns the pull request until it is merged**, classifies a bounce by its failing job and cases, re-queues an
   infrastructure or runner red once, and ends on one verdict line. A bounce went unseen for 30 minutes on 2026-11-10
   because only lane-launched attempts were watched.
3. **Two landings, 4a (`check`, `why`, `await`; deletes `gate.py` and `build-local.*`) and 4b (`land`; deletes `enqueue.py`
   and the lane's watch), each landed by its own branch's code.** Each deletes what it replaces and leaves no alias. 4a is
   provable as behaviour-neutral by the 12-change replay; 4b changes landing policy and is proven by its arms and by
   landing itself.

**Held to the owner's rule of 2026-11-10 ("If it cannot be resolved, there is a problem/bug"):** the design's section 4
table is binding. Every place the present code guesses, falls back or widens becomes exact, the input's true meaning, or a
named `NOT RUN [cause]`. Two rows are tightened here:
- `land_lane.py:962-971` (a job whose status GitHub does not report): the stall ladder must say on every read that the
  status was unreadable and why; it never counts the read as "stalled" silently.
- "An unknown NOT RUN tag reads as UNCLASSIFIED (BRANCH)": an unknown tag is a defect of the tree's own code, so the
  verdict says so in words ("unknown cause tag X: a defect of this tree's build.py") and exits NOT RUN, never as a class
  that sends a finisher to guess.

**Corrections to the design's assumptions.**
- "PB5207's addendum on the bounce" exists only as the requirement in decision 2; there is no separate file.
- The LINE ENDINGS blob memo (design 1.6) replaces PB5225's change-scoped form: PB5225's fallbacks to the full sweep when
  a classification input changes, and its attribute check through `git check-attr`, must survive in the memo's key (the
  memo is valid only for the `.gitattributes` content and the checker's own version it was built under).
- Audits become traced script targets (design A3). The tracer now fails a run on a read it cannot key (`UNKEYED READ`,
  kb/Work PB5227), so an audit that reads the tree in a form the tracer does not model fails loudly when it first runs as
  a target; that is the intended behaviour, and each such audit is fixed or the tracer is taught the form.

**Order.** Built after PB5230, PB5231 and PB5232 are on main, by whichever lane is free. The first commit of 4a writes the
arm-by-arm map of gate.py's self-test into this note's evidence, as the design's section 5 says.
