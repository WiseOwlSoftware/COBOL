**2026-11-17: landed (branch w20262102c).** `CommunicationBinder.BindSendDataOperand` binds identifier-2 (SEND WITH) and
identifier-3 (SEND ADVANCING): an operand whose own binding drew an error comes back refused, so SR2's and SR3's data-item rules
are not asked of it. Rerun of `triage85/g3/p/send.cob` at 85: one error, COBOL0900. SEND FROM keeps its two reports for a
non-alphanumeric function at 85, because §1985:XIV.3.6.3 SR6 is the statement's own rule for that operand ("If identifier-1 is
a function-identifier, it must reference an alphanumeric function") and the witness `1985-SR-XIV_3_6_3-6` pins it. Pinned by
`EditionCitationTests.SendWithFunctionIdentifier_At85_IsOneVerdict`.
