## PB4349: CiBoundsDriftTests's summary points at the design section that pins it

**Re-probe.** `CiBoundsDriftTests`'s summary, which `scripts/spec/drift_rules.py` prints to every implementer who touches a
workflow or the toolchain action, cited `DESIGN-test-build-ci.md` §3.17 (THE LANDING PIPELINE). The paragraph that states the CI
bounds and pins the class ("CI is bounded, and every cache is saved from main only") is in §1.4. The number was inherited from
PB4324's brief. Reproduced on this build by reading both sections.

**Fix.** The summary cites §1.4. A sweep of the repo for the same citation (`grep` over tests, scripts, `.github`, docs, skills,
`kb/Work`) found no other CI-bounds pointer at §3.17; the remaining §3.17 citations name the landing pipeline and are right.

**Why it cannot drift again.** New test `TheSummaryCitesTheDesignSectionThatPinsThisClass` takes the `DESIGN-*.md §N.M`
citation out of the class's own source file, finds that numbered heading in the design doc, and requires the section to name
`CiBoundsDriftTests` and to hold the "CI is bounded" paragraph. Run against the old §3.17 text it fails with "the summary cites
DESIGN-test-build-ci.md §3.17, which does not mention CiBoundsDriftTests". Fixture arms pin the section reader (nested sub-sections
belong to their parent, a sibling's text does not, a missing heading throws).

**Sweep result, not fixed here.** The same probe over all 59 `DESIGN-*.md §` citations in the unit tests found 18 whose cited
section does not name the citing class; most are legitimate (the doc describes the rule, not the test), a few look misplaced. They
go to the report as a lead for a registrar's note, since a generic check needs a rule for what "pins" means.

Gate: see the landing record.
