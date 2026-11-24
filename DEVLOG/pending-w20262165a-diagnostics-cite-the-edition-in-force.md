## At --std 2002 and 2014 the CALL, INVOKE, SET, MOVE and repository diagnostics cite their edition's rule, and a net asks it

Wave 20262165 group A (kb/Work PB5922, PB6118, PB6149, PB5670, PB5659, PB5660, PB5676, PB5671, PB5654), continuing the branches of
PB5706 (worktree-wf_c37a519f-ba4-18) and PB2622 (worktree-wf_38508f28-551-1), merged here. Their two new dialect behaviours both took
number 71 and VCR row 7.127: PB5707's (stamped DEVLOG 4036) keeps them, PB5706's moves to `DialectBehavior` 72 and VCR row 7.128.

**The net.** `EditionCitationNet` asked only `--std 85` (PB4950). It now asks 2002 and 2014 too: a diagnostic citing a later
edition's rule and none of its own fails, unless it is an edition gate. Measured on the merged base the 2002 and 2014 directions were
3395 and 3146 offending lines under 525 and 536 codes, about four times PB4950's 1985 campaign, so the net is laid as a ratchet of
codes (`edition-citation-sweep.json`) that the PB6118 sweep empties; `WISEOWL_COBOL_RECORD_CITATION_SWEEP` measures it.

**The shapes.** `CallSyntaxRules` holds the CALL rules by their 2023 number (2014 numbers them alike, 2002 by its own table) and the
conformance clauses (14.7.1/14.7.2 at 2002, 14.8.1/14.8.2 at 2014). `StorageSectionRule.For` carries each edition's rule and its section
list: 2002's still names the communication section. `ObjectReferenceSet.Refusal` takes the SET rules clause (`OoConformance.SetRules`;
SR8 to SR14 are numbered alike at 2002, 2014 and 2023), the run time passing 2023's. `SymbolTable.ClassNames.ParameterizedGround` is the
one statement of the parameterized-name ground, now also INVOKE's (COBOL0823 said "neither a resolvable data item nor a class").
The procedure-header rules, the report-group rules, CURRENCY SR21, USE AFTER EXCEPTION OBJECT and the scope rule choose by edition.

**Re-probes and adjudications.** PB5659's refusal is right at 2002 (§2002:8.4.2.9.2 SR6 gives the property the ACTIVE-CLASS
description, §2002:14.8.35.2 SR14 refuses the sender); the message now cites 2002 and names `SELFREF OF A`, not `__PROP-TEMP-200002`.
PB5660's COBOL0809 at 2002 is right (MOVE of class object, §2002:14.8.24.2 SR1), and at 2014 the receiver's 0809 is not a cascade; the
sender is now named as the compiler temporary it is. PB5671 and PB5654 were comment and help-text fixes.

**Goldens.** `wiseowl/cases/14.9.4-CALL/pb6149-call-by-reference-pointer-2002`, `14.9.39-SET/pb5659-set-property-active-class-2002`,
`14.9.25-MOVE/pb5660-inline-invocation-move-object-2002`, `12.3.8-REPOSITORY/pb5676-invoke-parameterized-class` (2002, 2014, 2023).
Thirteen refusal expectations that quoted a 2023 clause at every edition keep its rule designator. 32 codes left the ratchet.

**Left open.** PB5922 (COBOL1560/1707 licence text, COBOL1627 intrinsic clauses, the run-time reason texts) and PB6118 (the ratchet).
