- **D-R3 — which function a REPOSITORY `FUNCTION F` specifier names when its externalized name and its word name two
  different functions** (kb/Work PB989, PB2102; owner decision 2026-10-05; `docs/rearchitecture/DESIGN-external-repository.md`
  §2023:8.3). §2023:12.3.8.4 GR11 takes the details by EXTERNALIZED name: a) "if the externalized name of the function prototype
  is the externalized name of a function definition specified previously in the same compilation group" (11) a)), b) a
  function prototype definition of that externalized name, c) the external repository (GR11 NOTE 2: the externalized
  name is literal-5, or the word itself without AS). §2023:12.3.8.3 SR10 states LEGALITY with alternatives one of which is the
  WORD — "the name of a function definition specified previously in this compilation group" and "the name of a function
  prototype specified in this compilation group" (10)) — and §2023:8.4.6.7 lets the user-function-name be referenced "in the
  REPOSITORY paragraph of any source element that follows that function definition". With `FUNCTION-ID. FX AS "F"` and
  `FUNCTION-ID. F AS "G"` both ahead of `FUNCTION F`, the two clauses name different functions and the standard does
  not rank them; CLAUDE.md rule 1's order settles it by GnuCOBOL, whose 3.2.0 resolves by externalized name only
  (measured 2026-10-05). **WiseOwl COBOL:** (1) GR11 a), b), then c) — the external repository's entry of that
  externalized name (A.1 item 66) — by externalized name; whichever hits names the function. (2) When all miss, the
  function the WORD names: the definition specified before the referencing element
  whose user-function-name is F (its externalized name, "G", is what is activated), else the function prototype of that
  name, else the repository entry whose recorded word is F (a definition or a prototype; kb/Work PB2103). (3) Whenever
  step 1 selects a function and a DIFFERENT function carries the word F — in the compilation group, visible to the
  element, or in the repository — the externalized name is used, as GnuCOBOL does, and the compiler warns **COBOL2969**
  naming both. Legal source is never rejected: a specifier legal only by SR10's word alternative or §2023:8.4.6.7 takes step
  2, and one none of SR10's alternatives satisfies is REPO-1 (COBOL5962). A FUNCTION specifier therefore reads the
  repository's word index for step 3 even when the group answers step 1. Witnesses:
  `conformance:cases/11.5-FUNCTION-ID/pb989_function_externalized_name_wins`,
  `conformance:cases/11.5-FUNCTION-ID/pb989_function_prototype_externalized_name`,
  `conformance:cases/8.3.2.2-User-defined-words/pb303_as_externalized_name`,
  `conformance-test:RepositoryDetailsOrderTests.ExternalizedNameWins_AndTheFunctionThatOnlyTheWordNamesIsReported`,
  `conformance-test:RepositoryDetailsOrderTests.AnAsLiteralThatNamesTheOtherFunction_SelectsItWithoutAWarning`,
  `conformance-test:RepositoryDetailsOrderTests.APrototypeAndItsDefinition_AreOneFunction_AndDrawNoWarning`,
  `unit:GroupRepositoryOrderDriftTests`.
