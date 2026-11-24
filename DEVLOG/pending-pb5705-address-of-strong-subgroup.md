## PB5705: ADDRESS OF a group under a strong record, with no TYPE clause of its own, is refused below 2023

At --std 2002 (and 2014, SR5) a data-address-identifier that references a strongly-typed group item shall be defined with
its own TYPE clause referencing a STRONG declaration (§2002:8.4.2.11.2 SR4). 2023 drops the rule and types such a group by
position (GR2). `AddressOfOperandScreen.Admit` checked only SR2's elementary-subordinate case, so
`ADDRESS OF T-G OF S` (T-G a plain group under strong record S) compiled and ran at 2002 and 2014 ("ADDR-TAKEN", rc 0
on the re-probe at this tree).

Fix: one arm in `Admit`, below 2023, refuses a group entry with a strong root and no `StrongType` of its own, with
COBOL2785 citing the edition's rule (`EditionInfo.Cite`). The screen is the one door for SET, CALL, INVOKE and the
relation operand, so each surface is covered. Re-probe after: 2002 and 2014 refuse, 2023 still runs.

Goldens: `pb5705-address-of-strong-group-own-type` (accept 2002; ADDRESS OF S and of a subgroup with its own TYPE),
and the refusal `pb5705-address-of-strong-subgroup-no-type` (refuse at 2002 and 2014, COBOL2785). Row
`2002-SR-8.4.2.11.2-4` recorded CONFORMS (2002 GAP 1137 to 1136). No new diagnostic code used.
