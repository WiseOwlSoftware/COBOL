# PB6010, 2026-11-21: the seven codes cite their 1985 rule; the construct rows that relax a 1985 rule carry it (wave 20262131 group c)

**Fixed, each cite-checked (`scripts/spec/cite.py --check`) and pinned by `EditionCitationTests.Pb6010Refusal_At85_CitesThe1985Rule`
and a `pb6010-*-85` conformance refusal:**
- COBOL2404, GLOBAL outside its sections: §1985:X.4.6.3 SR1 (the GLOBAL clause's own rule, level and sections), not X.4.3.3 SR4
  (`DataBinder.ClausePlacement.cs#ClausePlacementRules`, the GLOBAL residence row's `Rule1985`).
- COBOL2377, a misplaced USE: the USE rule of the module of the named file's organization, §1985:VII.4.6.3 / VIII.4.8.3 /
  IX.4.8.3 SR1 through `EditionCite`, every module's for an open-mode USE, §1985:XIII.4.9.3 SR1 for BEFORE REPORTING and
  §1985:XV.3.2.3 SR1 for USE FOR DEBUGGING (`StatementBinder.UsePlacementRule`).
- COBOL5989, SORT/MERGE (and PROGRAM) COLLATING SEQUENCE: §1985:VI.4.5.4 GR4 (`SortBinder.SortCollatingClauses`).
- COBOL0899, SUM addend: §1985:XIII.3.19.3 SR1 (`ReportSectionBinder`, the SUM addend screen).
- COBOL0900 / COBOL3734 / COBOL3735 (edition gates): a new construct-row field `rule1985`, the 1985 rule an 85 program writing
  a RELAXED construct breaks; `ConstructRegistry.Check` cites it at 85 through `EditionInfo.Cite` and the row's `citation` from
  2002 on (unchanged there). 22 rows carry it (areas, linkage GLOBAL, sentences before a paragraph-name, the Annex F.2
  relaxations, the word, literal and PICTURE length bounds, the SEARCH subscript, the PROGRAM-ID and PD-header formats).

**The mechanism, for the successor groups (PB5993, PB5978, PB5530).** There is one choice of a citation by edition,
`EditionInfo.Cite(y1985, later[, y2002][, y2014])` (WOCA0001 refuses any other); a code is MOVED by one of three data changes:
(1) the message site writes its 1985 rule as Cite's first arm (or an `EditionCite` per organization for an I-O rule); (2) the
descriptor records `No1985Rule` with the reason 1985 has none; (3) for an edition gate, the construct row's `rule1985`. The tests
that go red when one is missed: `EditionCitationNet` (every refusal the suite runs at 85, through `ConformanceSuiteTests`) for
(1)/(2), and `ConstructRegistryDriftTests.EveryRelaxationRow_CitesIts1985RuleAt85` for (3) (a row whose vcr or description says
relaxation/restriction and whose 85 message names no §1985 rule). PB5993's measured upper bound of codes still citing only a later
edition is 336; no in-tree test lists them yet (the net sees only the refusals the suite runs at 85).
