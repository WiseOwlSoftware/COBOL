## Entry 4095 — 2026-11-28 00:19 PST — SET Format 4 names the category of a wrong operand, and SET TO FALSE draws one refusal below COBOL-2002 (PB6355, PB6356)

Two misleading SET diagnostics, one root: the Format 4 refusals named a mistake or an edition the program did not make.

**PB6355.** A SET Format 4 operand that is a declared name of another category (a data item, a paragraph or section, a mnemonic-name,
an alphabet-name, a class-name, an index-name) drew COBOL1639 "the reference identifies no resource", which is false: the name
identifies one. Re-probe on the build before the change (`--std 85`, 2002, 2014, 2023) showed the text at all four editions, and at 2002 and
2014 it cited the 2023 rule. `StatementValidation.RejectSetConditionName` now asks what the word IS (a procedure-name through the
binder's quiet procedure lookup, `SymbolTable.Declarations.NearestDeclaration` over every name class, then the SPECIAL-NAMES name classes)
and refuses with the existing category-mismatch code COBOL1757 (`StatementOperandRule`), naming the category the operand has and the
category SR6 requires, in the edition's own rule: §1985:VI.6.23.3 SR6, §2002:14.8.35.2 SR6, §2014:14.9.35.2 SR6, §2023:14.9.39.3 SR6 (each
checked with `cite.py --check`). COBOL1639 stays for a name nothing declares, a type declaration's member and a special register.
`DeclarationFact.ClassPhrase` gained the alphabet-name and SCREEN SECTION arms and the 2002 and 2014 clauses of its index-name, file
control, report description, report group and sum counter citations, which it printed from 2023 at those editions. The switch-status
condition-name arm and SET Format 3's SR5 quotation cite the 2002 and 2014 text too.

**PB6356.** `SET condition-name TO FALSE` is a 2002 construct. At `--std 85` the binder's SR7 screen drew COBOL2049 citing a 2023 rule
beside the COBOL0900 edition gate. `SetConditionBinder.BindSetCondition` now asks `ConstructRegistry.GateErrorCodeAt` for the
`set-condition-false-2002` row, as the ENTER statement does (PB5538), and stands down where the gate refuses; from 2002 on COBOL2049 cites
that edition's SR7 through `CiteRuleFrom2002`. The gate is an error on both strict and `--permissive` axes, so the screen never speaks at 85.

**Goldens.** `pb6355-set-format4-operand-wrong-category` (refused at all four editions, COBOL1757, "'W' is a data item"),
`pb6356-set-condition-false-with-false-phrase` (accepted 2002, 2014, 2023, output derived from §2002:14.8.35.3 GR5 and GR6),
`pb6356-set-condition-false-without-phrase-gate-only` (refused at 85, COBOL0900), the existing `pb1314-set-false-operand-not-condition-name`
now expecting the new text, and `SetFormat4OperandRefusalTests` pinning the per-edition citations, the absence of COBOL2049 and COBOL2362 at 85
and COBOL1639 for a name that resolves to nothing.

No inventory row changes verdict: both notes close with `closes_rows: []`. The 2023 nothing-resolves arm still cites §2023:8.4.2.1 at
2002 and 2014 (`UniqueReferenceRule.Citation`, PB6219 item 2), which that note owns.

