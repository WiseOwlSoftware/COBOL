## Review finisher (2026-11-16): the independent hunt's four findings, each answered on the branch

Source: `HUNT-gate-differential.md`, a read-only review of head 44ea07eee5. The loop's branch review recorded CLEAN.

- **F1, the compile's ambient inputs. CONFIRMED and fixed.** Before the fix, the capture gave
  `suite/2002/g2df04_parameter_environment` a hex program digest (`baf28350…`), so the case could be carried. After the
  fix it is `ambient`. `ArchOracle.Ambient` reads the one record of a compile's inputs, `Frontend.CompilationInputs`. A
  compile that read the environment, the repository, or a file outside its scratch directory, or whose probe found anything
  there, is never carried. The test that holds it is `ArchOracleDriftTests.TheProgramDigest_NamesTheProgram_AndRefusesAnAmbientCompile`.
- **F2, the working directory. PLAUSIBLE, closed without a reproduction.** I did not plant a stray library to reproduce it.
  The capture now compiles in the Conformance test host's working directory, its assembly's output directory, which is
  where VSTest starts the test host. Each row carries its absent probes outside the scratch directory, and a case whose
  absent probe now exists runs.
- **F3, the run's environment. PLAUSIBLE, closed by construction.** Every host and the capture start from
  `product_clear_env`, so no `COBOL*` or `WISEOWL_COBOL_*` variable of the shell reaches them. This applies to the
  whole-target store too. The installed runtimes and the locale are in the harness key. The calendar is in no key, as
  stated in the design. Left open: the whole-target key does not yet name the installed .NET runtime. That is a residue
  for the toolchain facet, which CI's leg computes from toolchain.env.
- **F4, untested protections. CONFIRMED and closed.** There is now one arm per protection: the manifest entry, the
  manifest's non-case content, the differential's own inputs, the facets, the run's facts, the ambient compile and the
  absent probe. I ran seven in-place mutations, one per protection, and each turns exactly its own arm red.
- **Design.** Section 3.14.13 now states that on Windows a carried case runs nowhere at the landed tree.
