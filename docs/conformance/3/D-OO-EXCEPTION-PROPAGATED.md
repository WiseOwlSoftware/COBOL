- **D-OO-EXCEPTION-PROPAGATED — An exception object converted to EC-OO-EXCEPTION is "propagated" (CALL / INVOKE NOT ON EXCEPTION) only when it is
  RAISED in the activator (§2023:14.9.4.4 GR3 i), §2023:14.6.13.1.5 items 1 and 4, spec silent on the pairing)**: GR3 i) runs the
  NOT ON EXCEPTION phrase unless "an exception condition is propagated from the called program". An object that a USE
  AFTER EXCEPTION OBJECT declarative of the activator takes IS raised there. An object no declarative takes becomes,
  by items 1 and 4, "as if EXCEPTION EC-OO-EXCEPTION were specified in the RAISING phrase", and a RAISING condition is
  raised in the activator only "if checking for that exception condition is enabled in the activating runtime element"
  (§2023:14.9.18.4 GR1 b)). **Pinned choice: with EC-OO-EXCEPTION checking OFF in the activator nothing is raised, so the
  call completes normally and its NOT ON EXCEPTION phrase RUNS; with checking ON the condition is raised and the NOT
  ON EXCEPTION phrase does not run** — the reading the NAMED GOBACK … RAISING arm already follows (kb/Work PB606).
  The last exception status after an unraised conversion is left holding the exception object. Witnesses
  `conformance:cases/14.6.13.1.5-Exception-objects/pb1121_object_propagation_boundary`;
  `conformance-test:OoSpineTests.GobackRaisingObject_NoF4_ActivatorUnchecked_RaisesNothing`;
  `conformance-test:FatalRaiseSelectionTests.ObjectPropagation_ActivatorChecked_NoHandler_TerminatesAbnormally`.
  (kb/Work PB1121.)
