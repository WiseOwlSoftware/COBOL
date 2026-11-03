## Three defects filed: one from the first Claude-written fuzz batch, two from PB4503's review

- **PB4507** (rejects legal source): `FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD", FUNCTION UPPER-CASE(WS-D))` is
  refused COBOL1627, although §8.4.3.2.4 1) makes a function-identifier reference a temporary data item and §15.48.3 3)
  asks only for "a data item of the same type as argument-1". Found by the PB4506 smoke batch (5 Sonnet-written
  programs, billed to the API key), adjudicated by slice C1.
- **PB4508** (misleading diagnostic): below 2002, PB4503's up-front CALL judgement runs before the NULL screen, so an
  undeclared NULL draws an extra literal-2 refusal.
- **PB4509** (under-rejects, pre-existing): at `--std 85`, `CALL P7 USING ...` naming the program's own name by a bare
  word compiles clean as a prototype CALL.
