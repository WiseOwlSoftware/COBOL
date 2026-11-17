**2026-11-17: landed (branch w20262102c).** Row 7.16 now says no ENTER compiles at 85 (COBOL7240), `ENTER COBOL.`
included, and keeps the SYSTEM-names rationale for the reason that still holds: the operands parse as SYSTEM-names so that the
refusal names the statement, where a cobolWord slot would answer `ENTER COBOL.` with a false reserved-word error (0901).
