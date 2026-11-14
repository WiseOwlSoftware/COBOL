- **D-PROPAGATED-NOT-ON-EXCEPTION — A condition PROPAGATED from a called program versus the CALL's NOT ON EXCEPTION phrase (§2023:14.9.4.4 GR3 i) —
  "If an exception condition is propagated from the called program, execution continues as specified in 14.6.13.1,
  Exception conditions; otherwise, control is transferred to the end of the CALL statement or, if the NOT ON
  EXCEPTION phrase is specified, to imperative-statement-2")**: **the two are alternatives — once a propagated
  condition is RAISED in the activator (§2023:14.9.18.4 GR1 b): checking for it enabled THERE), imperative-statement-2
  is never executed**, whatever §2023:14.6.13.1 then does: a fatal condition terminates or runs its declarative; a
  nonfatal one whose declarative completes normally continues after the CALL (§2023:14.6.13.1.4 3) says so outright:
  "the imperative-statement in that phrase is not executed"); a RESUME AT NEXT STATEMENT leaves the CALL; and a
  nonfatal one with NO applicable declarative, where §2023:14.6.13.1.4 4) hands back to "the rules for that statement",
  takes GR3 i)'s own first branch again rather than its "otherwise" — the reading that keeps the rule's two outcomes
  exclusive (the rejected reading, which would run imperative-statement-2 for exactly that last case, makes the
  word "otherwise" do nothing). A condition staged by the callee but NOT enabled in the activator is never raised
  (§2023:14.6.13.1.1), so it propagates nothing and NOT ON EXCEPTION runs. An exception OBJECT takes the same
  disposition. The pickup precedes both phrase bodies, so an activation written inside a phrase body can never
  consume the CALL's own staging. (kb/Work PB606; golden `2002/pb606_call_not_on_propagated`.)
