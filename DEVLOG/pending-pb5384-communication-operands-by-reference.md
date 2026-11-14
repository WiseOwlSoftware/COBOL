## PB5384, PB5342, PB5343: the communication statements judge each operand as bound, gate ALL ZERO at 2002, and quote the targeted edition's rule

Three defects of one file, `CommunicationBinder.cs`, each re-probed on this branch's own build before the fix (`cobol --std 85|2002`, probes under the wave's
scratch directory).

**PB5384, the operand read through its reference modification.** `IsIntegerItem` took the PICTURE of the item behind a `RefModPlace`, so SEND's
identifier-2 `I9 (1:1)` and identifier-3 `N2 (2:1)` ran (`RAN 00`) although a numeric item's slice is "class and category alphanumeric" (§1985:IV.4.3.8.4.4
GR5, §2023:8.4.3.3.4 GR6). It also answered true for every computed operand, so the LINAGE-COUNTER of a `LINAGE IS 60` file passed as a one-character
identifier-2 although its implicit description is "an unsigned integer whose size is equal to the size of integer-1" (§1985:VII.1.3.7). The KEY of ENABLE
and DISABLE and RECEIVE's identifier-1 had the same root in the other direction: `IsCommunicationAlphanumericArea` read `Place.Item`, so `KEY K-NUM (1:2)`
at 85 and `RECEIVE ... INTO K-NUM (1:2)` at 2002 were refused COBOL6171 as legal source. The sibling sweep found the same read in the one integer
classifier, `IntrinsicResultType.IsIntegerOperand`: `PERFORM N2 (1:1) TIMES` compiled clean and ran twice (§2023:14.9.28.3 SR2). The fix is at the
classifier: a reference-modified field operand is never an integer (the rule `RefModPlace.CategoryOf` already writes, so no ref-mod result is numeric),
and `IsIntegerItem` now asks that classifier for a data item, applies its one-character test to the PICTURE, and asks a counter register through its
implicit description (`AlgebraicRanges.CounterRegisterDescription`, a reader over the one `DescribeCounter`); every other computed operand (a function,
an index-name) is no integer data item. The alphanumeric screen is asked of the reference: a slice reads through `Table16Operand.Of(Place)`, whose
alphabetic rider keeps a PIC A slice alphabetic. A one-digit COMP item stays a one-character identifier-2 (`FUNCTION BYTE-LENGTH` is 1 here, and the
1985 read accepted it).

**PB5342, SEND ... ADVANCING ALL ZERO at 2002.** The ADVANCING literal admitted ALL ZERO at every edition. Integer-1 is a literal restricted to a numeric
literal, where from 2002 "the only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase" (§2002:8.3.1.2.5.2 SR1a, §2023:8.3.3.6.3
SR1a). `ScreenSendAdvancingLiteral` now asks the one gate of that row, `FigurativeAssociation.GateAllZeroAsNumeric` (VCR Table 7 row 7.28): COBOL0902
at 2002, accepted at 85 as before (`pb5289_send_integer_operands_85`). The message also spells the literal through `ConcatFolder.Spelling` (it printed
`ALLZERO`).

**PB5343, the 2002 rule quoted at --std 85.** Every COBOL6170 and COBOL6171 message quoted ISO/IEC 1989:2002 A.2 at 85. The two editions number and word
the same rules differently (2002 inserted SEND's SR2, so SEND's 85 SR2 to SR6 are its SR3 to SR7; 85 writes "must"), so each rule is now a `SyntaxRule`
carrying both, `Cite` quotes the targeted edition's, and `Prints2002` (formerly `ClassRules2002`) is the binder's one edition read. All 39 quotations
were checked with `cite.py --check` in both editions.

**Witnesses.** Refusals (85 and 2002, COBOL6171): `pb5384-send-with-refmod`, `pb5384-send-advancing-refmod`, `pb5384-send-with-linage-counter`; at every
edition (COBOL1646): `pb5384-perform-times-refmod`; at 2002 (COBOL0902): `pb5342-send-advancing-all-zero`. Acceptances: `pb5384_send_linage_counter_85`
(the counter of LINAGE IS 9 sends ESI, end key 1, and the counter of LINAGE IS 60 is an identifier-3), `pb5384_key_refmod_85`,
`pb5384_receive_into_refmod_2002` (the message lands in the slice: 5634). `CommunicationRuleCitationTests` pins the wording per edition. The inventory rows
1985-SR-XIV.3.6.3-2, -3, 1985-SR-XIV.3.3.3-4 and 1985-SR-XIV.3.2.3-4 are re-verdicted PARTIAL to CONFORMS (1985 GAP 249 to 245); 1985-SR-XIV.3.6.3-4's notes
drop the open PB5343.

**Found, not fixed here.** RECEIVE INTO a numeric display item stores the message through the item's numeric carrier, so a non-digit character is lost:
at 85 `RECEIVE ... INTO K-NUM` and `INTO K-NUM (1:2)` of the message `AB` leave `0034` where §1985:XIV.3.5.4 GR3 puts `AB34` in the character positions
(`CommunicationEmitter.EmitReceive` writes `CobolNum.StoreImage` of the spliced image). It is a code-generation defect of its own, recorded for the
registrar in the implementer's report.
