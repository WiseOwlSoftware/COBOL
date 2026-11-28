## EVALUATE bare condition-name object takes its per-evaluation window before the bind (PB6228)

A bare level-88 or SR6a boolean WHEN object is bound inside ClassifyPair, but the Condition arms of
WhenPhraseBinder.BindWhenItem took their window mark after that bind, so activations in the object's subscripts
(`WHEN T-OK (FUNCTION INTEGER (FUNCTION RANDOM * 3) + 1)`) stayed pending and were hoisted to the head of the EVALUATE
(14.9.13.4 GR4: an object is evaluated only when its WHEN phrase is analyzed). Re-probe on this tree before the fix
printed `ONE` then `HOISTED` (a hidden RANDOM draw); after, `ONE` then `UNTOUCHED`.

Fix: ObjectAsCondition takes the mark from before ClassifyPair and returns the condition with the window applied, so
neither Condition arm (Condition-subject x Condition, and the TRUE/FALSE-subject arm) can mark after the bind. The
explicit-condition object arm already marked first. Goldens: pb6228_bare_condition_object_subscript_not_hoisted
(2002+) and pb6228_bare_condition_object_subscript_refused_85 (COBOL0900). No inventory row changes.
