**2026-11-17: landed (branch w20262102c).** Decided by the 1985 text: §1985:IV.4.2.2.2.2 makes 1 through 18 digits the
numeric literals an implementor must allow, and this compiler's limit at 85; a literal of 19 digits is refused AS A LITERAL by the
literal screen (COBOL0802), the fault of the literal, and the VALUE clause's PICTURE range (COBOL1625) is no longer asked of it.
`DataBinder.ScreenValueLiteral` asks the screen's own decider (`NumericLiteral.WrittenDigits`, `EditionInfo.DigitCapacityExcess`).
Rerun of `triage85/g2/p5636.cob` at 85: COBOL0802 alone. Pinned by `EditionCitationTests.OverlongValueLiteral_At85_IsOneVerdict`.
