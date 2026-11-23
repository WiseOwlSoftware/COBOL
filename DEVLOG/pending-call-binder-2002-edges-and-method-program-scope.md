## CALL at 2002: the restricted-pointer signature rule and the ANY LENGTH screen take their 2014 edges; a method's CALL literal reaches the group's outermost program

Wave 20262158 group B, one root in the CALL binder: kb/Work PB5714, PB5611 and PB5963.

**PB5714.** At `--std 2002`, `CALL PA AS PRB` with PA restricted to a prototype whose signature differs from PRB's was refused
COBOL2936 under §2023:14.9.4.3 SR14 (re-probed rc=65). That rule first appears as §2014:14.9.4.2 SR14; COBOL-2002 says only which
prototype determines the called program's characteristics (§2002:14.8.4.3 GR7). The edge is a new register member,
`DialectBehavior.CallRestrictedPointerSignatureSame` (2014, VERSION_CHANGE_REFERENCE row 7.121), asked by
`CallBinder.Identifier1Screen`. The probe now prints `N=0008` at 2002, and COBOL2936 cites the 2014 text at 2014
(§2014:14.9.4.2 SR14; §2014:13.18.60.3 GR24) instead of 2023's.

**PB5611.** At `--std 2002`, an ANY LENGTH item passed as a `CALL ... AS NESTED` USING operand or RETURNING item compiled and ran.
COBOL-2002 states the rule once, under ALL FORMATS (§2002:14.8.4.2 SR10); 2014 split it by format (SR11, SR18). The second new member,
`DialectBehavior.CallAnyLengthOperandRuleByFormat` (2014, row 7.122), gates the split in `CallBinder.ScreenCallOperand`; at 2002 every
operand is screened and COBOL5981 cites the 2002 rule. Two existing cases ran an illegal 2002 leg (pb240_call_operand_screens leg 2,
pb1167_any_length_returning's program path); both are now accepted at 2014, where each matched its `.out`.

**PB5963.** A CALL or CANCEL literal in a method or factory method was listed in the cross-reference and the CALL graph as an EXTERNAL
program even when it named an outermost program of the group: `OoDriver.BindClassBody` built both of its statement binders without
`ProgramsInScope`. One function, `BinderDriver.ProgramsInScopeOf`, now makes the scope for every statement binder; a method's caller is
null, so `ProgramVisibleFrom` asks arm 4 (§2023:8.4.6.3 SR3) alone. The reviewer's probe now lists one `PROGRAM MAINQ` row with refs
5 13 26.

**Goldens.** `14.8.4@2002-CALL-STATEMENT/g2002-call-restricted-pointer-as-other-prototype` (accepted at 2002, `N=0008`),
`g2002-call-as-nested-any-length-using-refused` and `g2002-call-as-nested-any-length-returning-refused` (refused COBOL5981 at 2002);
`pb609-call-restricted-program-pointer-signature`'s refusal narrowed to 2014 and 2023; `ListingCallGraphTests.AMethodsLiteral_ReachesAnOutermostProgramOfTheGroup`.
Rows re-verdicted CONFORMS through one `record_verdicts` batch: 2002-GR-14.8.4.3-7 and 2002-SR-14.8.4.2-10 (2002 GAP 1147 to 1145).
The lineage screen places both members in §2002:14.8.4 (`lineage-screen-triggers-2002.json`), and a second batch answers the new
behaviour-switch flag on the seven stored 2002 CALL change records it reaches (SR14, SR15, SR17, SR18, SR20, SR21, GR6: none reads
either switch).

**Sibling sweep.** The SET program-pointer screen's SR22 is in 2002 as §2002:14.8.35.2 SR21, so it has no edition gap. The ANY LENGTH
rules of the three ISO editions differ only at CALL.

**Self-review.** No inline edition comparison: both edges are register members with VCR rows and doc facts. Each message chooses its
citation through `EditionInfo.Cite`/`CiteRule`. The program-name scope still has one producer.
