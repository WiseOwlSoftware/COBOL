**Review lead R1-2 of the w20262006b finisher (2026-11-12, wave 20262008 group B): the citation of the 1985 refusal.**

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/ValueSubject.cs` (`CharacterSizeRule`),
`src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs` (`ValidateValueCategory`, the alphanumeric and numeric-edited size arms).

At `--std 85`, `88 XXC VALUE "ABC"` under `PIC XX` was refused (right) with a COBOL1740 message citing ISO §2023:13.18.63.3
SR4, which bounds only "an elementary item" and "an alphanumeric group item" and does not reach a condition-name
(`ValueSubject.ForConditionName`'s own remarks). The rule that reaches it is §1985:VI.5.15.3 SR3 (cite.py --check OK:
"Nonnumeric literals in a VALUE clause of an item must not exceed the size indicated by the PICTURE clause").
`ValueSubject.CharacterSizeRule` now names the rule per subject kind and both character-size arms cite it; probe
`E:/COBOL-coord/scratch/review-worktree-wf_ea985d42-ace-7/s85.cob` now reads `(§1985:VI.5.15.3 SR3, the conditional
variable's PICTURE)` for the 88 and keeps `(ISO §2023:13.18.63.3 SR4)` for the data item. Pinned by
`ConditionNameValueSizeTests.At85_AnOversizeAlphanumericConditionNameLiteral_IsRefusedBySr3` and the
`pb5190-condition-name-value-oversize-1985` diagnostic expectation.
