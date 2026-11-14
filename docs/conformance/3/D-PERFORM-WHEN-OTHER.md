- **D-PERFORM-WHEN-OTHER — Exception-checking PERFORM — where control goes after WHEN OTHER's imperative-statement-3 (§2023:14.9.28.4 GR18, a GENUINE
  STANDARD DEFECT: two consecutive sentences, two destinations)** (kb/Work PB438). GR18 prints, one after the other, "At the
  completion of the execution of imperative-statement-3, control is passed to the end of the PERFORM statement or, if WHEN COMMON
  is specified, to imperative-statement-4" and "At the completion of the execution of imperative-statement-3, control is passed
  as indicated in General rule 14.9.29 or, if WHEN COMMON is specified, to imperative-statement-4". Both are on the printed page
  (PDF page 719), so the duplication is the standard's, not the transcription's; the second sentence's "General rule 14.9.29"
  names a CLAUSE number (14.9.29 is the RAISE statement) where a general-rule number belongs. **Pinned reading: the second
  sentence, with its broken cross-reference read as General rule 20, exactly as GR17 and GR19 word the same hand-off.** After
  imperative-statement-3 (and imperative-statement-4 when WHEN COMMON is specified) GR20 decides: a NONFATAL condition resumes
  with the implicit CONTINUE immediately after the statement of imperative-statement-1 that raised it, so the rest of
  imperative-statement-1 still runs; a FATAL one proceeds as 14.6.13.1.3 (abnormal termination) unless a RESUME redirects it.
  *Why, from the text and not from the code:* (1) GR20 itself says "the processing in General rules 17, 18, and 19 is completed,
  return depends on whether the exception condition was fatal or nonfatal" - it names GR18, and that return would be vacuous for
  a WHEN OTHER whose first sentence had already sent control to the end of the PERFORM; (2) the first sentence cannot be the
  rule alone, because with WHEN COMMON both sentences hand control to imperative-statement-4 and GR19 then passes it "as
  indicated in General rule 20" - so a WHEN COMMON written beside a WHEN OTHER would turn "abandon imperative-statement-1" into
  "resume in place", and one WHEN OTHER whose destination depends on whether an unrelated phrase is present is no reading of GR18;
  (3) a named condition (GR17, returning by GR20) would otherwise resume in place where an unnamed one (WHEN OTHER) abandons imperative-statement-1, though GR14 enables both the same way and
  GR17 and GR18 select the same handler machinery. §2023:14.9.28.4's own cross-clause statement, §2023:14.6.13.1.4 item 2, describes only a
  PERFORM "that contains a WHEN phrase that specifies the exception-name" and says the WHEN phrase's statement "completes as
  specified in the rules for the PERFORM statement" - GR20; it is silent on WHEN OTHER, which GR18 supplies, and that silence is
  the one consideration the other way. This is not implementor latitude (CLAUDE.md rule 1's precedence never starts: the ISO
  text controls, through GR20); the project's earlier scout note that "the first sentence already gives the correct destination"
  (`PHASE-13-remaining-waves-scout.md`) is superseded by this entry. Realized in one place: `ControlFlowEmitter.EmitMatcherArms`
  sends the WHEN OTHER arm through the same `__RunF3` return path (`EcEmitter.EmitRunF3`) as a named WHEN, so GR17, GR18 and GR19
  cannot drift. Witnesses: `PerformFormat3BehaviorTests.WhenOther_NonfatalWithoutResume_ContinuesInPlace`,
  `WhenOther_NonfatalWithCommon_RunsCommonThenContinues`, `WhenOther_Fatal_ThenTerminates` and
  `conformance:cases/14.9.28-PERFORM/pb438_f3_when_other_nonfatal_resumes_in_place`; the test that stood for GR18 before wrote
  RESUME NEXT STATEMENT inside the WHEN OTHER, which prints the same output under either reading.
