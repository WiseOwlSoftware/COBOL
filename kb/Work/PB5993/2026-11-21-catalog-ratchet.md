# PB5993, 2026-11-21: the catalog asks it, 494 of 669 descriptors moved (wave 20262131 group h, branch train85b-cite)

**The structure the 336-code count lacked.** The earlier measurement compiled suite programs at `--std 85`, so it saw only what the
suite exercises. The question is now asked of the CATALOG: `DiagnosticDescriptor` carries `Rule1985` (the 1985 rule its site cites
at 85, in the edition form) beside `No1985Rule` (why the 1985 text has none), and
`tests/WiseOwl.Cobol.Tests.Unit/EditionCitationCatalogDriftTests.cs` requires every descriptor whose `IsoSection` cites only a later
edition to carry one of them, or to be on `Unmoved`, a list of descriptor ids that ratchets DOWN only (a new descriptor that carries
neither fails; a listed one that gains one fails until it leaves the list). Measured on 3a30f8b2f7: 669 descriptors (655 codes).

**Rule1985 is held to the message.** `EditionCitationNet.MisRecorded` (Conformance) asks every refusal the suite compiles at 85: a
code whose descriptors all record `Rule1985` and whose message cites a 1985 rule must cite one of the recorded clauses. Its first run
found 28 family descriptors whose sites cite more rules than the source harvest saw (298 cases); each `Rule1985` now lists the rules
the messages cite (`misrec.py` over the run's trx), asked at the clause.

**Moved (669 -> 175):**
- 319 `No1985Rule`, by family, each family's reason one constant in `DiagnosticCatalog` (`No1985Oo`, `No1985Exception`,
  `No1985Directive`, ..., 42 families): constructs ANSI X3.23-1985 does not have (object orientation, exception checking, `>>`
  directives, floating indicators, REPOSITORY, function definitions and prototypes, pointers, national, boolean, CONSTANT, types and
  SAME AS, dynamic-length items and dynamic-capacity tables (the 2023 variable-length group, §2023:8.5.1.12.1), Format 2 VALUE, locales,
  floating-point, VALIDATE, locking, COMMIT, concatenation, externalized names, fixed-width binary, PICTURE EDITING, BY VALUE /
  OMITTED / RETURNING, line sequential, file COLLATING SEQUENCE, WRITE FILE, later intrinsics, FORMAT / SELECT WHEN, ORDER TABLE,
  GROUP-USAGE, hexadecimal literals, OPTIONS, RECURSIVE, ALIGNED, IN alphabet, INITIALIZE VALUE, ASSIGN USING, table SORT, SUM
  ROUNDED, the screen facility, SET FARTHEST-FROM-ZERO, the source encoding).
- 175 `Rule1985`: descriptors whose sites already cite the 1985 rule at 85 (a harvest of the 1985 arm of each `Cite`/`CiteRule` in the
  statement that names the descriptor, then the trx correction above), COBOL2894's §1985:I.1.5.2.5.2 and COBOL1637's §1985:VI.5.8.3 SR13. Every value passes
  `python scripts/spec/audit_code_citations.py --check` (0 findings).

**Left (175, the ratchet list):** descriptors whose sites cite no 1985 rule the harvest could read (some cite one through a helper, as COBOL1637 did), mostly
the genuine members of this note's class (a 1985 program reaches a message citing only a later edition), plus a few whose 1985 status needs a reading
(`messaging-operand-class`, `messaging-message-area`, `termination-status-operand`, `duplicate-externalized-definition`,
`record-less-fd-*`). Each needs its 1985 rule derived from the 1985 text with `cite.py --check`, the site's 1985 arm, and the
descriptor's `Rule1985`; or `No1985Rule`. The list is `EditionCitationCatalogDriftTests.Unmoved`.
