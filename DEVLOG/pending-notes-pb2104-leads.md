## kb/Work PB4456-PB4460: the PB2104 branch review's leads, filed

The PB2104 branch review (w20261838a, rounds 1 and 2) left six leads. Each was checked against the branch head before
filing:

- PB4456 (rejects legal source): the placement-(a) free-form writer continues a literal before its first content unit,
  so a literal whose opening delimiter lands at column 197 or 198 leaves an empty continued line.
- PB4457: placement-(a) sub-compilation errors point at a deleted temp file and drop their warnings.
- PB4458: three ExpansionPlacementPass arms (REPO-12 overwrite, COBOL2240, REPO-2) have no test.
- PB4459: OoMethodSymbol.Ctx encodes its origin as nullness.
- PB4460: a conformance test builds its own stale runtime major in place of RuntimeVersionSkew.OtherMajor.

The FragmentParse.ParseUnit duplication went onto the open PB2735. PB4461 is unused: the citation the reviewer
flagged was already §11.3.4 GR1 at the branch head.
