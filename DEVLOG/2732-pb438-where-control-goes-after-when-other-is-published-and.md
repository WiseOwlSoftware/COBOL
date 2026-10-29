## Entry 2732 — 2026-10-29 12:41 PDT — PB438: where control goes after WHEN OTHER is published, and the test that could not tell is replaced

ISO/IEC 1989:2023 §14.9.28.4 GR18 prints two consecutive sentences naming different destinations after imperative-statement-3:
"control is passed to the end of the PERFORM statement" and "control is passed as indicated in General rule 14.9.29" (a clause
number where a general-rule number belongs; 14.9.29 is RAISE). The compiler already implemented the second reading, nothing
recorded the choice, `PHASE-13-remaining-waves-scout.md` said the first sentence was right, and the one covering test wrote
RESUME NEXT STATEMENT in the WHEN OTHER, which prints the same output under both readings.

- **Re-probe.** On a fresh Debug build a nonfatal EC-USER-DEMO with a WHEN OTHER that has no RESUME printed
  `OTHER / AFTER / DONE`, and a fatal EC-BOUND-SUBSCRIPT printed `OTHER` and then terminated abnormally. Behaviour unchanged.
- **Determination.** The second sentence, read as General rule 20. GR20 itself says "the processing in General rules 17, 18,
  and 19 is completed, return depends on whether the exception condition was fatal or nonfatal", so it names GR18. The first
  sentence cannot be the whole rule: with WHEN COMMON both sentences go to imperative-statement-4 and GR19 then goes "as
  indicated in General rule 20", which would make one WHEN OTHER abandon or resume imperative-statement-1 depending on an
  unrelated phrase. §14.6.13.1.4 item 2 (the note's support for the first sentence) never mentions WHEN OTHER and sends the
  WHEN statement's completion to "the rules for the PERFORM statement", which is GR20. No implementor latitude is used.
  Published in `docs/CONFORMANCE.md` §3 beside the FINALLY-on-fatal and RESUME-skips-COMMON determinations.
- **Emitter.** No change: the WHEN OTHER arm already shares the named WHEN's `__RunF3` return path, so GR17, GR18 and GR19 cannot
  drift. The comments at `ControlFlowEmitter.EmitMatcherArms` and `EcEmitter.EmitRunF3` now cite the determination.
- **Tests.** `WhenOther_HandlesUnnamedCondition` is replaced in both `PerformFormat3BehaviorTests` and
  `PerformFormat3MethodBehaviorTests` by RESUME-free tests: nonfatal resumes in place, nonfatal with WHEN COMMON runs
  `OTHER / COMMON / AFTER`, fatal terminates, and RESUME NEXT STATEMENT skips WHEN COMMON. New golden
  `14.9.28-PERFORM/pb438_f3_when_other_nonfatal_resumes_in_place` (2023) with its below-2023 refusal
  `pb438-f3-when-other-below-2023` (COBOL0900). The scout note's GR18 line is corrected.
- **Register.** `GR-14.9.28.4-18` PARTIAL to CONFORMS (GAP 31 to 30); PB438 landed with `closes_rows`; PB435 now names the
  replaced test.
- **Sibling sweep.** The pattern "a test whose body forces the same output under both readings of the rule it names" was
  swept across the WHEN OTHER / WHEN COMMON / RESUME tests of both Format-3 test files; the other RESUME-bearing tests
  (`Fatal_ResumeNext_SuppressesTermination`, `ResumeNext_SkipsWhenCommon`, the tier tests) name rules whose answer the RESUME
  itself decides, so they discriminate.

