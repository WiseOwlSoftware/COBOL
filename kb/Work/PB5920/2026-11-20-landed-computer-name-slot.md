# PB5920 - LANDED (wave 20262114 group A): a literal in the computer-name slot is refused as a computer-name

**Re-probe (Debug build of the worktree, `--std 85`, `1985-SR-VI_4_3_3-1.cob`).** Before: `error COBOL1971: '"XYZ"' is not a
paragraph of the CONFIGURATION SECTION - the ISO §1985:VI.4.2 general format lists the paragraphs ...`. The OBJECT-COMPUTER twin
(`OBJECT-COMPUTER. "XYZ".`) drew `COBOL1970: '"XYZ"' is not a clause of the OBJECT-COMPUTER paragraph` at every edition, the same
fault in the other paragraph: the user wrote a computer-name.

**Rule.** §1985:VI.4.3.3 SR1 and §1985:VI.4.4.3 SR1: "Computer-name is a system-name." (`cite.py --check` OK for both). §2002:8.3.1.1.2,
§2014:8.3.1.1.2 and §2023:8.3.2.3.1 (the quotation "add restrictions to the rules for formation of a COBOL word", OK in all three) give a
system-name's formation as a COBOL word's, so no edition lets a literal or a number stand there. The 2023 text has no SR that says
"computer-name is a system-name"; its 12.3.5.3 SR1 is only the second-period rule.

**Fix.** COBOL7654 `ComputerNameNotASystemName` (`DiagnosticCatalog`), reported by `ClosedFormatPass.Refuse` when
`ClosedFormats.ComputerNameSlotOf` reads the run as the slot's occupant: the first clause of an OBJECT-COMPUTER paragraph that wrote no
name, or the configuration paragraph right after a SOURCE-COMPUTER paragraph that wrote no name, no WITH DEBUGGING MODE and nothing else.
The message cites the edition in force (`EditionInfo.Cite`). At every edition: the wording was wrong at every edition, and the 2002
text is not changed in verdict (the program was refused before and after; only the code and the sentence differ).

**Not covered (lead).** A RESERVED word in the slot (`OBJECT-COMPUTER. GLOBAL.`, `SOURCE-COMPUTER. LENGTH.`) is met by the parse
layer (`CobolErrorStrategy`'s residue hint, `ResidueRefusal`) before any tree exists: it keeps COBOL1970 / COBOL1971 with "cannot parse
construct near" wording. Its context is the half-built stack, so the slot reading would need the same predicate over a half-built
tree.

**Sites:** `src/WiseOwl.Cobol.Compiler/Validation/ClosedFormatPass.cs` (`ClosedFormatPass.Refuse`, `ClosedFormatPass.ComputerNameRefusal`),
`src/WiseOwl.Cobol.Frontend/Cst/ClosedFormats.cs` (`ClosedFormats.ComputerNameSlotOf`),
`src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs` (`ComputerNameNotASystemName`),
`conformance-suite/wiseowl/diagnostics/1985-SR-VI_4_3_3-1.json`, `conformance-suite/wiseowl/diagnostics/1985-SR-VI_4_4_3-1.json`,
`conformance-suite/wiseowl/diagnostics/pb5920-source-computer-literal-name.json`,
`conformance-suite/wiseowl/diagnostics/pb5920-object-computer-literal-name.json`,
`tests/WiseOwl.Cobol.Tests.Unit/ClosedFormatReservedWordTests.cs`, `tests/WiseOwl.Cobol.Tests.Conformance/EditionCitationTests.cs`.
Not `AreaPlacementPass.cs`: the note named it for its PB5905 comment, and the message is not made there.
