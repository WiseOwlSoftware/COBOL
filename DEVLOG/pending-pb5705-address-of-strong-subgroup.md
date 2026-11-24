## PB5705: ADDRESS OF a group under a strong record, with no TYPE clause of its own, is refused below 2023

At --std 2002 (and 2014, SR5) a data-address-identifier that references a strongly-typed group item shall be defined with
its own TYPE clause referencing a STRONG declaration (§2002:8.4.2.11.2 SR4). 2023 drops the rule and types such a group by
position (GR2). `AddressOfOperandScreen.Admit` checked only SR2's elementary-subordinate case, so
`ADDRESS OF T-G OF S` (T-G a plain group under strong record S) compiled and ran at 2002 and 2014 ("ADDR-TAKEN", rc 0
on the re-probe at this tree).

Fix: one arm in `Admit`, below 2023, refuses a group entry with a strong root and no `StrongType` of its own, with
COBOL2785 citing the edition's rule (`EditionInfo.Cite`), asked through the new `DialectBehavior.AddressOfSubordinateStrongGroupNeedsNoOwnTypeClause` (ChangedIn 2023, VCR 7.125; the first gate refused an inline year comparison, InlineEditionComparisonDriftTests). The screen is the one door for SET, CALL, INVOKE and the
relation operand, so each surface is covered. Re-probe after: 2002 and 2014 refuse, 2023 still runs.

Goldens: `pb5705-address-of-strong-group-own-type` (accept 2002; ADDRESS OF S and of a subgroup with its own TYPE),
and the refusal `pb5705-address-of-strong-subgroup-no-type` (refuse at 2002 and 2014, COBOL2785). Row
`2002-SR-8.4.2.11.2-4` recorded CONFORMS (2002 GAP 1137 to 1136). No new diagnostic code used.

Two older cases had relied on the 2023 reading at 2002: `pb1408_universal_address_of_subordinate_group` now accepts from
2023 only, and the `pb1408-set-address-of-based-from-subordinate-strong-group` refusal lists 2023 alone (2002 and 2014
refuse the same program earlier, with COBOL2785). The first gate's other two reds were harness timeouts
(`call_by_reference_formal_passed_on` at 2002, `pb3350_open_reversed_varying_span_85`: NO VERDICT, the program did not finish).
