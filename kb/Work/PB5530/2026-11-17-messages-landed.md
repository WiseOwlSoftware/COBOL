**2026-11-17: the message half landed (branch w20262102c, kb/Work PB4950).** Every COBOL1964 and COBOL1965 message of
`SearchAllFormat2Rules` cites, and quotes, the rule of the edition in force through one map, `SearchAllFormat2Rules.Rule`,
which asks `EditionInfo.Cite`: §1985:VI.6.22.3 SR1, SR3 and SR4 at 85; SR4 to SR9 of §2002:14.8.34.2 and §2014:14.9.34.2;
§2023:14.9.37.3 SR6 to SR13. The 2023 SR12 and SR13 begin in 2014 (§2014:14.9.34.2 SR10, SR11), so at 85 and 2002 those two
cite 2014. Each locator passed `cite.py --check`. Rerun at 85 (`triage85/g2/p5530.cob`): `COBOL1965: ... each data-name-1,
data-name-2 "must be referenced in the KEY IS phrase in the OCCURS clause referenced by identifier-1" (ISO §1985:VI.6.22.3 SR4)`.

**Remaining:** the second item, a `docs/VERSION_CHANGE_REFERENCE.md` row (85 to 2002) for the two sentences kb/Work PB5193
relaxed at 85, with its `constructs/` anchor. That is an edition behaviour (a construct row and its gate), not a message, so
this note is `half`.

**Lead (not filed, for the owner of the SEARCH ALL rules).** SR12 ("shall not specify a variable-length group") is refused at
85 and 2002, where neither text has the rule; whether 85 should refuse it is a question for the 1985 read, not this wording.
