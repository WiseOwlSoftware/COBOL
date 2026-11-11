## COBOL-85 edition arms: SORT in a sort procedure, the currency exclusion list, five Report Writer rules and the index data item contexts

Wave 20262001 group D landed eight of its nine kb/Work notes. Each is a rule the compiler applied in its 2002 or 2023
form at every edition. Each fix is the edition arm in the one place the rule is computed. Every note was re-probed on
this build before its fix, either with its own repro or, where no 85 arm existed, by running the same code path at 2023.

- **PB5274** (rejected legal source). A file SORT written in an input or output procedure was refused with COBOL2700 at
  every edition. The text makes that ban a 2023 rule only: §2002:14.8.36.2 SR1 and §2014:14.9.36.2 SR1 keep the 1985
  placement rule, and §1985:XI.4.4.4 GR7/GR10 forbid only executing such a SORT. `GateSortMergeProcedures` now puts its
  SORT arm under `Year >= 2023`, beside the MERGE arm. The four repros, which COBOL2700 used to refuse, now run. The
  PB1139 refusals are now `refuse-at: ["2023"]`. New 85 golden: `pb5274_sort_written_in_sort_merge_procedures`.
- **PB5173** (rejected legal source and accepted illegal source). The forbidden currency-symbol set is now one record per
  edition, `CurrencySymbolExclusion`. The 1985 row comes from §1985:VI.4.5.4 GR11 and admits E and N but refuses every
  lowercase letter; the 2002 row comes from §2023:12.3.7.3 SR22/SR27. `CurrencySymbolExclusionDriftTests` pins both rows
  to the quoted rules. A 1985 E or N must also be read as the currency symbol, so `PictureAnalyzer` and
  `PictureComposition` now ask for the currency symbol before the picture letters. Result: `[E05][ E12.50][012.50E]`
  at 85. The 1985 row of DOC-A.1-44 is documented.
- **PB5247** (wrong answer). An alphanumeric SUM counter is capped at 18 digits below 2002 (§1985:XIII.3.19.4 GR1 c.).
  An X(20) total now prints `000000000000000002` followed by two spaces, where it used to print twenty digits.
- **PB5244**. The 1985 report footing starts after the page footing's final LINE-COUNTER setting, which includes its
  NEXT GROUP (`Cobol85FinalLineCounter`). From 2002 it starts after the page footing's last line (§2002:13.16.55.3
  GR7 g)).
- **PB5251**. At 85 GROUP INDICATE does not stand as a PRESENT WHEN clause, so §1985:XIII.3.11.3 SR2 refuses
  out-of-order and overlapping GROUP INDICATE items (COBOL2745).
- **PB5252**. A single test, `ExceedsThreeSignificantDigits85`, now bounds the PAGE, LINE NUMBER and NEXT GROUP integers
  to 999 below 2002.
- **PB5253**. At 85 a SUM clause may not name a sum counter of another report (§1985:XIII.3.19.3 SR1). The
  cross-report admission is §2002:13.16.52.2 SR3 g).
- **PB5188**. `OperandContextRules.IndexDataItemAdmittedAt` is the edition reading of the SR10 column: an index data
  item as an intrinsic function argument is refused at 85 with the new COBOL6979. A SORT or MERGE key and a report
  SOURCE are named by no edition's list, so both are now refused at every edition (COBOL1757, COBOL2770; the SOURCE
  screen now applies MOVE SR1's sending half).

**Not landed.** PB5242 (the 2002 and 2023 Report Writer clause additions accepted at 85) is about twelve constructs, each
needing its own row in the construct catalogue and its own edition determination. It goes to a fresh implementer.

**Inventory.** Eight 1985 rows were recorded CONFORMS through `record_verdicts --edition 1985`. They are the ones for
GR-VI.4.5.4-11, GR-XIII.3.19.4-1, GR-XIII.3.10.10-1, SR-XIII.3.11.3-2, SR-XIII.3.15.3-1, SR-XIII.3.16.3-2,
SR-XIII.3.19.3-1 and SR-VI.5.14.3-5. The 1985 GAP fell from 451 to 443. Four 2023 rows gained witnesses.

**Gate.** The first gate run was RED on LINE ENDINGS: Python writers had left CRLF in the working tree, and
`line_endings.py --fix` repaired it. The second was RED on INERT LANE: the new drift test opened a Markdown
transcription, so its rule texts are now quoted constants. No test ran in either. See the branch report for the final
verdict.
