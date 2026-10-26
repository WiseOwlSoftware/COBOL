## The OO design doc's GR7 c) list names both ACTIVE-CLASS arms and says which crossings proceed unchecked

PB3793 (filed from the branch review of PB2985). `docs/COBOLNET_OO_DESIGN.md`'s GR7 c) bullet listed what the universal
switch checks. It named the ACTIVE-CLASS returning arm but not the formal arm (`ActivationRelations.ActiveClassFormalViolation`,
asked by `CobolObject.CheckActiveClassFormal`, section 14.8.2.3.2 rule 4), and its closing sentence said any nonconforming
crossing with checking not enabled in both cannot proceed and takes `CobolImplementorFatalException`. That contradicts both
ACTIVE-CLASS arms, which proceed unchecked (the returning arm under the delivery's class narrowing, the formal arm under the
section 9.3.6 3 d) 4./5. match). Re-probe: the stale text was still on main. Fix: the bullet now describes both arms and the
closing sentence splits the arms. No code changed; the behavior is PB2985's. Sweep: the PB1112 paragraph was already current and
the interprogram design doc describes the typed lane. Citations checked with `cite.py --check`: 14.8.2.3.2, 14.8.3.3 2) b) 4.,
9.3.6 3) d) 4. Rows closed: none (documentation drift). Gate: see the landing record.
