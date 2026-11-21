# PB6011, 2026-11-21: the computer-name half landed with PB5985; the missing-period half stays open (wave 20262131 group c)

**Landed.** `SOURCE-COMPUTER. MERGE.` (and `OBJECT-COMPUTER. GLOBAL.`, PB5985) now draws COBOL7654, the computer-name sentence, at
every edition: the parse layer's residue refusal (`CobolErrorStrategy.ResidueRefusal`) asks `ClosedFormats.ComputerNameSlotAt`
of its half-built context, and `ClosedFormats.ComputerNameRefusal` is the one sentence both reporters write, citing the
reserved-word rule beside the computer-name rule (§1985:III.2 "Reserved Word", §2023:8.3.2.1 1)). Conformance refusals
`pb6011-source-computer-reserved-word-name`, `pb5985-object-computer-reserved-word-name`.

**Open: the missing separator period after a data entry.** Re-probed at --std 85 (`01 A PIC X VALUE "A"` then `01 B PIC X.`):
COBOL3735 "'01' begins in area A", then COBOL1639 twice, COBOL1585 and COBOL2423; at 2023 the four cascades alone. The parse
takes `01` and `B` as further VALUE operands, so the entry never ends. The root is in the parse: at 85 an integer literal that
begins its fixed-form line in area A can only be a level-number (§1985:IV.7.3.3, §1985:IV.7.4), so the entry before it lacks its
separator period ("The last clause is always terminated by a separator period", §1985:IV.7.4), and the parser should end the
entry there and report the missing period, which removes the cascades. The parser has no area placement today (the area check
is the post-parse `AreaPlacementPass`, through the source map), so the fix needs the placement of a token at parse time: a
redesign of its own. Repro `E:/COBOL-coord/scratch/w20262131c/p/noperiod.cob`; sites `src/WiseOwl.Cobol.Compiler/Validation/AreaPlacementPass.cs`
(the COBOL3735 report), the data description entry's clause list in `src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4` and
`src/WiseOwl.Cobol.Frontend/Parsing/CobolErrorStrategy.cs` (the missing-period reading).
