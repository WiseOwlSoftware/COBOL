**Addendum (2026-11-12): the exact lines on origin/main (5237495315) for the five sites this note names, from the PB5125 finisher's lead 3 (w20262006a-PB5125-report.md).**

All five still read `Place.DenotedItem` for an attribute of the operand, so the lead holds and nothing since has fixed it:

- `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/CallEmitter.cs:528` (`CallEmitter.ElementarySending`): `BoundFieldOperand f => f.Place.DenotedItem?.Pic?.Category is PicCategory.Numeric or PicCategory.NumericEdited`.
- `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/CallEmitter.cs:1510` (`CallEmitter.IsImageCarriedNumeric`): `p.DenotedItem is { IsGroup: false, Pic.Category: PicCategory.Numeric } item`.
- `src/WiseOwl.Cobol.Compiler/CodeGen/ProgramEmitter.cs:781` (the RETURNING profile read; the member to be located by the probe): `ret.DenotedItem is { Pic: { Category: PicCategory.Numeric, IsFloat: false, Usage: not Usage.Index } } item`.
- `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/OoEmitter.cs:2151` (the COMPUTE sender test): `sp.DenotedItem is { Pic.IsClassNumeric: true }`.
- `src/WiseOwl.Cobol.Compiler/Oo/ActivationDescriptions.cs:196` (the place-to-description step): `p is RefModPlace r ? OfReferenceModification(r) : p.DenotedItem is { } item ? Of(item, classes) : null`. The path in the note, `Oo/ActivationDescriptions.cs`, is `src/WiseOwl.Cobol.Compiler/Oo/ActivationDescriptions.cs`, not under `CodeGen/`.

Other `DenotedItem` reads in the same files (CallEmitter.cs:500, 647, 720, 740, 794, 837; ProgramEmitter.cs:700, 775; OoEmitter.cs:926, 951, 1969) are the same family and belong to this note's probe. The probe the note asks for is still the next step: an alias of a group passed BY REFERENCE, as RETURNING, as an OO argument and as a COMPUTE sender, each at the editions that admit it.
