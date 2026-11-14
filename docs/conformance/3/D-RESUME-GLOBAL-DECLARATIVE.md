- **D-RESUME-GLOBAL-DECLARATIVE — RESUME within the scope of execution of a global declarative is CONTINUE — "scope" is per program instance
  (§2023:14.9.33.4 GR1, spec silent on what bounds the scope)**: GR1 makes a RESUME executed "within the scope of execution
  of a global declarative" a CONTINUE. A global declarative may PERFORM a local declarative (§2023:14.9.49.3 SR4), so the
  question is dynamic, and the standard says only "scope of execution". **Pinned choice: the scope is bounded by the
  PROGRAM INSTANCE that declares the GLOBAL declarative**, the same test as §2023:14.9.18.4 GR6's "specified in the same
  program" (the per-instance `__useActive[global ids]`). A RESUME in a program CALLed from a global declarative runs in
  a fresh activation whose resume point is well defined, so it resumes normally there; GR1 exists because a resume point
  inside a global declarative's on-behalf-of invocation is not. Witnesses `conformance:cases/14.9.33-RESUME/pb1160_resume_in_global_scope`
  (a RESUME in a PERFORMed local declarative, `NEXT STATEMENT` and `procedure-name` arms, and the contained-program arm
  through `__RunGlobalUse`); `conformance:cases/14.9.33-RESUME/pb1160-resume-in-when-in-global-declarative` (SR2 asked before the
  WHEN-phrase question). (kb/Work PB1160.)
