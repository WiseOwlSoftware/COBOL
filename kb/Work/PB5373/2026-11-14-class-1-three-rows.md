**2026-11-14: class 1 is recorded on three of its five rows (wave 20262028, group A); the other two do not fit the arm as the catalog stands, and class 2 waits for its refuter pass.**

kb/Work PB5374's tooling had landed (the `undefined-in-rule` arm, the `claude: 2026-11-12` signature), so the class 1 rows
were read against their own 1985 catalog text and recorded through `record_verdicts.py --edition 1985` (batch
`E:/COBOL-coord/scratch/w20262028a/batch-class1.json`: 3 records, 3 rows changed, 1985 GAP 210 to 207).
`audit_derivations.py --check`: 48 rows claim a derivation, 48 close on one, no findings.

| Row | Names (quoted from the row's own text) | Citation check |
|---|---|---|
| `1985-GR-XIV.2.2.4-7` | "lacking an INITIAL clause, the results are undefined" | `cite.py --check "§1985:XIV.2.2.4 GR7"` OK |
| `1985-GR-XIV.2.2.4-29` | the same words (GR29 is GR7's level 1 twin) | `cite.py --check "§1985:XIV.2.2.4 GR29"` OK |
| `1985-GR-XIV.3.6.4-5` | "referenced by identifier-1 is undefined" | `cite.py --check "§1985:XIV.3.6.4 GR5"` OK |

**Moved out of class 1: `1985-GR-IV.4.4.2-L2.1` and `-L2.2`.** Neither item's text says "undefined". The sentence that
does, "When there is no next executable statement and control is not transferred outside the COBOL program, the program
flow of control is undefined unless ...", closes the list in §1985:IV.4.4.2, and the 1985 rule catalog (and so `cite.py`)
files it as part of the last item, `GRL2.5` ("The end program header."). So `undefined-in-rule` cannot close either row
without quoting another row's text, which its checker rightly refuses. The two rows stay open until the catalog gives the
list's closing paragraph its own place (a defect of the 1985 extraction, reported by this wave as a lead) or a decision
names another arm for them; the 2023 standard settles the same case in Annex A.2 item 11.

**Class 2 is not recorded.** Every argued arm signed `claude:` stands only on an UPHELD `cobol-refuter` record, and an
implementer cannot run that pass. The documentation each row needs already exists: `docs/CONFORMANCE.md` section 2 rows
28 to 30 and section 7's A.1 item 24 (no reel/unit device) for `1985-GR-VII.4.7.4-15`; section 3, "Communication devices"
(vertical positioning is not applicable; a device line is one whole message) for `1985-GR-XIV.3.6.4-14`, `-16` and `-18`;
and section 7's A.1 item 117 (no source listing) for `1985-GR-VI.3.4.4-1`. The wave's report carries a proposed
derivation for each, for the refuter to try to break. `-18` is the weakest: its antecedent (a device that does not
superimpose) is TRUE here, so it may want a test of class 4 rather than a derivation.

**Sites:** `docs/CONFORMANCE.md` (section 8: `DRV-1985-GR-XIV.2.2.4-7`, `-29`, `DRV-1985-GR-XIV.3.6.4-5`),
`tests/version-matrix/inventory-1985/` (the three rows), `tests/version-matrix/conformance-register.json` (generated),
`scripts/spec/record_verdicts.py` (the derivation-closure message no longer says owner-signed),
`scripts/spec/inventory_schema.py` (`Schema.state_for`, `Schema.derivation_stands` docstrings).
