**2026-11-14 (wave 20262032, group A): no further row is recordable by an implementer; the shipped statement no longer calls every derivation owner-signed.**

Re-read on `fe96afd75f`: the 21 rows stand at 11 recorded, 10 open, and each open row is blocked on something outside an
implementer's reach.

- **The three ENTER rows** (`1985-GR-VI.6.12.4-1`, `-2`, `-3`) wait for kb/Work PB5308, still `open`.
- **The five class 2 rows** (`1985-GR-VII.4.7.4-15`, `1985-GR-XIV.3.6.4-14`, `-16`, `-18`, `1985-GR-VI.3.4.4-1`) need an
  UPHELD `cobol-refuter` record each, because a `claude:` signature on an argued arm stands only on one. An implementer
  has no way to run that pass, and a record it wrote itself would not be an independent read. The proposed derivations
  are in the wave 20262028 A report (`E:/COBOL-coord/scratch/reports/w20262028a-PB5373-report.md`, lead 2).
  On `-18`: the runtime transmits a message to a device only when it is complete
  (`MessageControlSession` releases a terminal's message on its last portion, `McsStore.Transmit` writes it as one
  line), so no SEND ever transmits characters to a line that already holds characters. The rule's antecedent (a device
  that does not superimpose) is true here, but its consequent's subject (characters previously transmitted to the same
  line) is empty. That is an argument for the refuter to test, not a test: a program of two SENDs would show two lines
  either way and could not fail.
- **`1985-GR-IV.4.4.2-L2.1` and `-L2.2`.** The design question is settled against
  `docs/rearchitecture/DESIGN-spec-conformance-review.md` (the "Lists" item of the unit segmentation): "a unit after the
  list that continues it belongs to the colon unit, not to the last item". The 1985 catalog breaks that rule at
  §1985:IV.4.4.2, filing the list's closing paragraph ("When there is no next executable statement ... the program flow
  of control is undefined unless ...") in `1985-GR-IV.4.4.2-L2.5`'s text. Neither catalog carries the `lead` field the
  same item describes. So the root is the catalog extraction (`scripts/spec/rule_catalog_1985.py`), not this register:
  once the closing paragraph belongs to the colon unit, the `undefined-in-rule` check can read a list item together with
  the lead and closing paragraph of the unit that owns it. `1985-GR-IV.4.4.2-L2.5`'s CONFORMS row quotes the closing
  paragraph and must be re-read when that lands. Until then the two rows stay open; this is not a decision to give them
  another arm.

**Also fixed (the wave 20262028 A report's lead 3).** The shipped conformance statement's §8 heading read "owner-signed
derivations", which three `claude: 2026-11-12` rows have made untrue. The heading now reads "signed derivations"
(`derivation.register-heading` matches its prefix, so no schema change), and the descriptions of the current mechanism
say "signed" in `docs/conformance/README.md`, `docs/DOC_INDEX.md`, the design doc, the docstrings and messages of the
derivation tooling, and the C# readers. Comments that record what the owner decided in 2026-09 (kb/Work PB386) and
the rows the owner did sign keep "owner-signed".

**Sites:** `docs/conformance/8/index.md` (`heading`), `docs/conformance/README.md`, `docs/DOC_INDEX.md`,
`docs/rearchitecture/DESIGN-spec-conformance-review.md` (section 1 (c), the `derivation` field row),
`scripts/spec/inventory_schema.py` (`Derivation`, `Schema.__init__`), `scripts/spec/record_verdicts.py` (module
docstring, `main`'s derived-closure report), `scripts/spec/audit_derivations.py` (module docstring),
`scripts/spec/audit_annex_a1.py` (the §7-row finding), `scripts/docchecks/register.py` (`build_extract`),
`conformance-suite/harness/generate.py` (module docstring), `tests/_shared/ConformanceRegister.cs` (class summary,
`Derivations`), `tests/WiseOwl.Cobol.Tests.RepoDrift/SpecTraceabilityInventoryDriftTests.cs` (`DerivationRule`,
`DerivationStands`, `EveryDerivation_StandsUnderItsOwnArm`, `TheseChecks_ActuallyFail_OnAFabricatedInventory`).
