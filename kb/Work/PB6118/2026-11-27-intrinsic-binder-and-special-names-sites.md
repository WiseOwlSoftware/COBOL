## 2026-11-27 - the intrinsic binder, the SPECIAL-NAMES binder and the one concordance of §2023:15 citations; what the sweep still holds (wave 20262174 group C)

**Landed.** The 17 two-arm sites of `IntrinsicBinder.cs` and `SpecialNamesBinder.cs`; the ORDER TABLE messages (a 2002 clause); every
§2023:15 rule of `IntrinsicBinder.cs` through `IntrinsicCitations.Rule`/`BareRule` and `IntrinsicCitationConcordance` (180 citations, the rule
found by wording, the subclause where the wording differs). The ratchet `edition-citation-sweep.json` is now 487 codes at 2002 and 499 at
2014; the measure (`WISEOWL_COBOL_RECORD_CITATION_SWEEP`) is 3008 and 2774 lines.

**Left, by mechanism (each a site of this sweep; none is a new defect).**
- `SpecialNamesBinder.Alphabets.cs`, `.Classes.cs`, `.Currency.cs`, `.Literals.cs`, `.Locales.cs`: about 40 hard-coded `ISO §2023:12.3.7.3 SRn`
  and `§2023:12.3.6.3 SR3` literals (`Locales.cs:64`, `Classes.cs:85,161`, `Alphabets.cs:250`). The shape landed here (`CiteRule` with
  `y2002` and `y2014`; the 2002 clauses 12.2.6.x and 12.2.5.x, the 2014 clauses 12.3.6.x and 12.3.5.x).
- `SpecialNamesBinder.cs:105-107` (floating-point literal): the message words 36 digits and four exponent digits at every edition, where the
  2002 text (§2002:8.3.1.2.2.2 rules 2 and 3) says 31 and three. A citation question and an edition-behaviour question: check what the binder
  enforces at 2002 before re-citing.
- `IntrinsicArgumentRules.cs` (COBOL1627, 5 lines at each edition): the zero-length-literal rule (`§2023:15.59.3 AR3`, ORD-MAX and ORD-MIN
  `AR2`) fires at 2002 and 2014 and prints bare; neither edition has the rule, so the question is whether it applies there at all.
- `IntrinsicArgumentRules.cs:842,843,1266,1270`: prose descriptions of argument types cite `§2023:15.2` and `§2023:15.3 argument type n`
  (2002: 15.1 and 15.2, with other type numbers).
- Data-side COBOL1630 (a reference-modified item cannot itself be reference-modified, §2023:8.4.3.3.3 SR3, 2 lines) and COBOL5989 at
  SORT/MERGE (`SortMergePhraseBinder.cs`, `FileControlBinder.cs`, 6 lines): the same two-arm shape in other files.
- The families the 2026-11-24 addendum lists (COBOL1560, COBOL1639, COBOL1757, COBOL5506, COBOL2269) are outside the three files.
