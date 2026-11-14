- **D-PROP — what `>>PROPAGATE ON` propagates, and from where** (kb/Work PB1119). §2023:7.3.21.4 GR2 says "any exception
  condition raised and not handled … shall be propagated as though a GOBACK RAISING LAST statement were executed",
  while the two processing orders it feeds name the directive in exactly two places: §2023:14.6.13.1.3 6) for a FATAL
  condition with checking enabled (not EC-FLOW-GLOBAL-EXIT / -GOBACK), after 5)'s declarative, and §2023:14.6.13.1.5's
  EXIT/GOBACK item 3 for an exception OBJECT in the activating element. §2023:14.6.13.1.4, the order for a NONFATAL
  condition, has no propagation step — its 4) continues the statement. **(a) A nonfatal level-3 condition is not
  propagated**: the specific processing order governs, so an unhandled EC-OVERFLOW-STRING under PROPAGATE ON
  continues exactly as under OFF (`conformance:cases/7.3.21-PROPAGATE/w73d2_pb1119_propagate_on`, "B CONTINUES"). **(b) A MAIN program
  propagates nothing**: §2023:7.3.21.1 propagates "to the activating runtime element" and a run unit's main program has
  none — the literal GOBACK there is a STOP that ignores RAISING (§2023:14.9.18.4 GR3), which would end a failing run
  unit NORMALLY — so 7)'s abnormal termination stands (`conformance:AutomaticPropagationTests.MainProgram_HasNoActivator_Terminates`).
  **(c) An activator's re-propagated OBJECT is typed at run time**: "an applicable class or interface … specified in
  the RAISING phrase of the procedure division header in the activating element" is an `is` test over that header's
  classes (FACTORY OF: the factory type) and interfaces; no match stages EXCEPTION EC-OO-EXCEPTION
  (`conformance:AutomaticPropagationTests.ExceptionObject_ItemThree_ObjectOrEcOoException`). Implemented at the one
  fatal default, `EcEmitter.EmitSelection` (and the EC-I-O arm of `__IoCheckEc`, which §2023:14.6.13.1.3 3) sends on to
  6)), with the element's ON/OFF folded by `PropagateState`.
