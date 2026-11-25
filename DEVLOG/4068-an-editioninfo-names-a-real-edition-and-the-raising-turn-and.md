## Entry 4068 — 2026-11-25 04:32 PST — An EditionInfo names a real edition, and the RAISING, TURN and word-character messages cite the edition compiled at (PB6082, PB6175)

Wave 20262171 group I, one root: the edition value and the citations it chooses.

**PB6082.** `EditionInfo` was a public positional `record struct`, so `new EditionInfo(1999)` and `default(EditionInfo)` (Year 0)
constructed and `CompilerDriver.Compile` validated nothing; the PB2619 claim that a year outside `EditionInfo.All` is
unrepresentable did not hold, and `EditionContext` built its value with the non-validating constructor on purpose. Now the one
constructor is private behind `EditionInfo.Of`, `Year` has no setter (a `with` can flip `Permissive` only), and `Year` throws on a
default value, so every predicate asked of one fails loudly; `ToString` still prints it. `CompilerDriver.Options.Edition`
re-validates in its initializer and its `init` accessor. Seven `new EditionInfo(dialectLevel)` sites and `EditionContext` ask `Of`.
Witnesses: `EditionInfoTests.Default_IsRefusedAtTheFirstRead`, `TheYearIsSetOnlyThroughOf`,
`CompilerDriverTests.CompilerDriver_Options_RefuseADefaultEdition`.

**PB6175.** The re-probe on this branch reproduced all three cases: COBOL2102 cited §2023:14.9.18.3 SR1 for GOBACK at 2002 and
2014, §2023:14.9.14.3 SR2 for EXIT PROGRAM at 2014; COBOL2773 cited the 2023 rule and Annex B at 2002; the >>TURN repetition
message printed `()` at `--std 85` beside COBOL0900. The sibling sweep found the same pin in every `EcRaiseSite` citation
(COBOL0710, COBOL2103, the EC-USER and identifier-1 messages cited the 2023 clause at every edition, and COBOL2103's text named the
2023-only PERFORM WHEN phrase at 2002 and 2014) and one more empty `y1985` (`NameResolution.ReportUnreferenceableLinkage`, unreachable
at 85). Fix: `EcRaiseSite` carries each edition's clause and cites by an `EcRaiseRule` with the edition in hand; the 2002 and 2014
clauses number the rules as 2023's does (re-derived from those editions' texts by `EcRaiseSiteDriftTests`), except GOBACK at 2002,
whose clause states only SR1 and whose RAISING phrase answers to EXIT PROGRAM's rules through §2002:14.8.17.3 GR1. A construct 85
lacks cites the 2002 rule through the new `EditionInfo.CiteRuleFrom2002` / `CiteFrom2002`. The >>TURN stage reports neither
repetition rule below 2002 (the construct gate alone speaks there, as the name window already did). `CobolCharacterRepertoire.Violation`
cites the judging edition's own rule in every arm. `CiteRuleArgumentDriftTests` (RepoDrift) refuses a `CiteRule` call with an empty
edition text. The merge of main brought PB5753's parameterized-definition header rules (`OoExpansion.HeaderRules`), whose empty
`y1985` and `y2002` texts that test refused in the merge queue (#2094, twice): they now cite through `CiteRuleFrom2002`, and the
repeated-parameter-name rule, which COBOL-2014 introduced, through the new `EditionInfo.CiteRuleFrom2014`. Goldens: `pb6175-goback-global-declarative-cites-2014` (refused at 2014 with §2014:14.9.17.2 SR1) and
`pb6175-word-outside-annex-c-cites-2002` (refused at 2002 with §2002:8.3.1.1 and Annex C); the two COBOL0710 refusals
`l1-exit-raising-level2-name` and `pb388-goback-raising-level2-name` now expect the message up to the edition's citation, which
differs by edition. No inventory row changes verdict (`closes_rows: []` on both notes).

Self-review: one decider for each citation (`EditionInfo.Cite` and its two helpers), no inline edition comparison added, the
analyzer's WOCA0001 honoured (the repertoire's hole names the edition number), no allocation on a hot path (the citations are built
only when a diagnostic is reported). Left as leads: the 2014 word-character table derives "not first" characters from 2023's Annex E
where 2014's GR4b makes its special and combining characters medial; the identifier-1 message's trailing "GR1 b) 2." and COBOL0717's
"Table 13" are 2023 numbering; GOBACK RAISING at 2002 rests on reading GR1 as importing EXIT PROGRAM's syntax rules; some
`CiteRule` calls (about a dozen, listed in the report) pass another edition's rule as an edition argument.

