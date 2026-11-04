## PB1654: COBOL1942 names the ALIGNED subject by the one group classifier and the Table 2 category

The note asked for a sweep of the has-a-PICTURE tests against the rules that name group kinds. The earlier audit
(wave 20261861 b, 108 sites in 57 files) had already found no under-rejection, so this landing carries the one defect
it left: `CheckAlignedClausesPass.DescribeAlignedSubject` classified the subject by hand. The re-probe on this tree
reproduced it. `01 G ALIGNED.` over a `DYNAMIC LENGTH` item was told it was "an alphanumeric group item" (§13.18.29.4
GR3 excludes a variable-length group), `PIC ZZ9 ALIGNED` read "category numericedited" (the enum name, not the §8.5.2.1
Table 2 name), and `USAGE OBJECT REFERENCE ALIGNED` read "objectreference".

The pass now asks `ItemCategory.Face`, which switches on `GroupKindsOf` and names the national, strongly-typed and
variable-length exclusions, and prints an elementary item's Table 2 category through `CategoryName`. Three hints stay
because only §13.18.1.3 SR1 needs them: the GROUP-USAGE BIT hint (§13.16.4 GR1) for a plain alphanumeric group, the
boolean DISPLAY hint (§13.18.60.4 GR5), and two shapes the classifier cannot see because the pass judges the TEMPLATE
once. A group of a `TYPEDEF STRONG` declaration is no strongly-typed item yet (§8.5.3.1: the items described with the
type are), so `GroupKindsOf` answers alphanumeric there by design; the pass asks `DeclaresStrongType` and
`DataBinder.UnderStrongTypeDeclaration` and says no GROUP-USAGE BIT clause can make it a bit group (§13.18.29.3 SR1)
instead of telling the programmer to write one. A `TYPE T ALIGNED` entry is held unexpanded, so it reads "an item
described with the TYPE clause" rather than "not an elementary or group data item".

Goldens (negative, `refuse-at` 2002, 2014, 2023, pinning the words of the message): `pb1654-aligned-strong-typedef`,
`pb1654-aligned-variable-length-group`, `pb1654-aligned-numeric-edited`, `pb1654-aligned-object-reference`,
`pb1654-aligned-type-entry`. The accepted legs are the existing `pb487_aligned_*` cases; the rule's behaviour did not
change, only the words. No inventory row moved. Sibling enum-name faces in other binders (ExpressionBinder, SetBinder,
RefModBinder, SignClause, ReportSectionBinder) stay with PB3229. PB1654 is landed.
