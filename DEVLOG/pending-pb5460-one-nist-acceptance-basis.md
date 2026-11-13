## PB5460: one NIST acceptance basis in two languages, held to one table; a form feed is a page eject everywhere a harness compares program output

The nightly `Guard (WiseOwl COBOL NIST, parallel)` job had been red on SQ101M since PB2509 (pull request #1091). The
re-probe at 9dbc2cac09 reproduced it alone: `GUARD_TESTS=SQ101M bash scripts/guard-fast.sh` printed `UNEXPECTED: SQ101M
expected MATCH, got DIFF` and `unexpected=1`, and the forensic `actual.norm` held eight `␠\f` where `expected.norm` held
none. Since PB2509 the runtime writes each print record whole, so a padded record's spaces precede the form feed of an
ADVANCING PAGE. The shell normalizer, `guard_normalize_into`, dropped the spaces before a CR and at a line's end, never
before a form feed. Its C# twin, `NistRunner.Normalize`, whose comment called it "exactly" the shell rule, went through
`String.ReplaceLineEndings`, which reads a form feed (and NEL, LS and PS) as a line break. So the in-process gate stayed
green, and it would have compared a runtime that wrote a line advance where a page eject is owed equal to a correct one.

The fix is one rule, written in two languages: every CR goes with the spaces before it, the spaces before a form feed go
and the form feed stays, each line loses its trailing spaces, COMPUTED= is masked, and trailing empty lines and the final
line end go (the shell arm now drops those too, so both arms produce the same bytes). `tests/nist/normalization-cases.json`
holds 26 cases. `NistWrittenFilesTests.TheAcceptanceBasis_MapsEveryParityCaseToItsStatedResult` holds the C# arm to them,
and a new parity witness in `scripts/guard-verify.sh --self-test` holds the shell arm (`GUARD_VERDICT_LIB` points it at
another copy). Before the fix the C# arm failed 8 cases and the old sed failed the 6 form-feed cases and every case with
a final line end; after it both pass all 26, and SQ101M prints `MATCH` with `unexpected=0`. The golden and its
`corpus.tsv` row did not change. `scripts/inert_lane.py` names the table as a TOOLING_READS entry of `guard-verify.sh`.

The sibling sweep (`git grep -n ReplaceLineEndings -- tests src`) found the same fold in the console-output basis of the
conformance suite and the differential goldens. `CutRunner.Normalize` and `run.py`'s `normalize()` now fold only the host
newline (CR LF or LF); a lone CR, a form feed, NEL, LS and PS are what the program printed. Their shared
`normalization-fixture.json` states it, and `run.py` reads an expected file's bytes, because text mode folds a lone CR.
`DifferentialGolden.Assert`, `VersionBehaviorMatrixTests` and `WiseOwlCobolTestBase.RunAssembly` were changed the same
way. The other sites compare diagnostics, source or repository text, or a fixed literal the test's own program DISPLAYs;
the note's landing addendum lists each with its decision. DESIGN-test-build-ci.md §1.2 states both bases.
