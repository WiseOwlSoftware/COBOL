**2026-11-17: landed (branch w20262102c, kb/Work PB4950).** `DiagnosticDescriptor.Title1985` holds a decline's message at
85, and `EditionContext.Declined`, the one mechanism that composes a decline, renders it through `EditionInfo.Cite`. COBOL1778 at
85 (rerun of `triage85/g1/p5875.cob`): "the STANDARD-1 phrase of the RECORD DELIMITER clause (ISO §1985:VII.2.8) is a hardware
dependent language element (§1985:I.1.5.2.5.1; §1985:C.3 item 6, "dependent upon a reel type of device") whose §1985:VII.2.8.3
SR2 medium is a magnetic tape file ...". Every 1985 citation passed `cite.py --check`. COBOL1778 left the PB4950 allowlist.
