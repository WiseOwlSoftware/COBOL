# PB6046 - the owner: the suite need not follow the NIST model (2026-11-22)

The owner, verbatim: "WISEOWL test suite need not follow the NIST model for test cases. It's 40+ years later. We can
potentially do better" (restating his 2026-10-14 direction, kb/Work PB3160: "we need not use the NIST structure as that
is a 40+ year old designs. We can modernize").

How revision 4 holds to it:
- **NIST-85 is a coverage floor, never a template.** Stage 4 measures that every behaviour a NIST unit checks is also
  checked by a WiseOwl test of the rows it maps to (decision 3: "a feature map, not a copy"); no NIST program, paragraph,
  report page, X-card or PASS/FAIL scaffolding enters the suite, and a crosswalk entry never shapes a test.
- **A WiseOwl test is derived from the 1985 text, row by row**, with its expected result from the rule (design §6.1), small
  and independent, judged per test by any compiler through an adapter, with machine-readable results (design §3-§5).
- **Where we do better than NIST, and measure it:** every 1985 row and every alternative of every general format
  (NIST tests a sample); the exception and abnormal paths of file, sort and merge statements (MANDATORY-PRACTICES I11);
  the two warning mechanisms 1985 requires, judged at the site; per-test verdicts that FAIL only for a non-conformance;
  and the report lists what NIST missed (the 1985 rows and points no NIST unit exercises).
- A design choice in a later stage that copies a NIST structure needs a reason recorded in this note's folder.

## The owner, later the same day: NIST becomes unnecessary

Verbatim: "When WISEOWL test suite is complete, we should not NEED to run the NIST test suite as all WiseOwl tests
should cover all of NIST. We can optionally run NIST to assurance, but is WiseOwl passes, NIST should never fail."

So Stage 4's exit is an INVARIANT, not a percentage: **a compiler that passes the WiseOwl 85 suite passes every NIST-85
unit not excluded** (decision 11's FIPS programs aside). Held by:
- the crosswalk: every NIST unit's checked behaviour (its expected value or report line) is checked by a named WiseOwl
  test of the same rows, with the same or a stricter expectation; a `differs` entry (a NIST check the 1985 text
  contradicts) is the only exception and carries its citation;
- the gate: once Stage 4 closes, `tests/nist/` leaves the gate and the nightly guard (design §6 step 5) and runs only as
  optional assurance; a NIST failure on a compiler that passes the WiseOwl suite is a SUITE defect (a coverage gap), filed
  as a kb/Work note and closed by the WiseOwl test that would have caught it, never by a NIST-side exception.
