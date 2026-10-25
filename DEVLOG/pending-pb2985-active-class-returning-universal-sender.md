## kb/Work PB2985: an ACTIVE-CLASS returning item through a universal INVOKE is checked as a universal sender

ISO §14.8.3.3 rule 2 b) 4. gives an ACTIVE-CLASS returning item, invoked "with any other object reference", the same
description as that reference as the sending operand of its SET. A universal INVOKE is invoked with a universal
reference, so the sender is universal, and §14.9.39.3 SR10, SR12 and SR14 admit a universal sender only into a universal
receiving item (SR8). A typed receiving item does not conform, and §14.9.23.4 GR7 c) sets EC-OO-UNIVERSAL when checking
is enabled in both the activated method and the activating element. The compile-time lane
(`OoConformance.ReturningSender`) already asked this; the run-time lane did not.

**Re-probe.** On the wave-1055 probe `pa.cob`, `INVOKE U "RA" RETURNING OP` (U universal holding a subclass object, OP typed
as the superclass, RA's item ACTIVE-CLASS) ran without an exception and delivered the object. The cause was one line:
`ActivationRelations.ReturningConformance` returned null for every ACTIVE-CLASS sender, leaving the delivery's class
narrowing (`CobolObject.NarrowUniversal`) as the only check, and that checks the object, not the description.

**Fix.** `ActivationRelations.ActiveClassReturningViolation` asks the one SET table (`ObjectReferenceSet.Refusal`) with a
universal sender. `CobolObject.CheckActiveClassReturning` raises EC-OO-UNIVERSAL before the method runs when checking is
enabled in both halves and does nothing otherwise, so an unchecked program that delivered correctly still does (the
narrowing stays as the floor). The emitted `__CobolInvoke` case asks it for each ACTIVE-CLASS returning item
(`RuntimeApi.ObjCheckActiveClassReturning`), and so do BASE's New and FactoryObject, whose half counts as enabled because
the standard class has no source. It is not a term of `ReturningViolation`: that stop is unconditional (an
implementor-fatal when unchecked), which would turn a working unchecked program into a crash.

**Sweep.** Arms of the dispatch: emitted universal case (fixed), BASE New and FactoryObject (fixed), typed INVOKE
(already right), CALL RETURNING (unreachable: ACTIVE-CLASS is legal only in a factory, instance or method definition,
§13.18.60.3 SR16). `pb2463_universal_returning_set_match` c14 changed from a match to EC-OO-UNIVERSAL, as that note had
recorded rule 2 b) 4. would refuse it; its golden is updated. A sweep of the 28 conformance programs that turn
EC-OO checking on and INVOKE found no other output change.

**Goldens.** `pb2985_active_class_returning_universal_invocation` (ten cases: universal and typed receivers, factory and
instance methods, BASE New and FactoryObject, the activated method's checking off, the activating element's off), its
below-2002 negative, and `ActiveClassReturningUniversalSenderTests` (the relation's matrix, the two-halves rule, a drift
guard that every BASE method storing a RETURNING box asks the check). `docs/COBOLNET_OO_DESIGN.md` states the arm; the
work note PB2985 is landed with `closes_rows: []` (no inventory row claims this corner).
