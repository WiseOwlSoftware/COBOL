## PB5445: COBOL-85 refuses a RECORD KEY or ALTERNATE RECORD KEY naming a group that holds an OCCURS DEPENDING ON table

Re-probe on a fresh Debug build: `cobol --std 85` translated both drafts (`RECORD KEY IS IG`, `ALTERNATE RECORD KEY IS IG`, where
`IG` is a group holding `IGT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON IGN`) with no diagnostic. §1985:IX.2.7.3 SR3 and
§1985:IX.2.5.3 SR3 (both `cite.py --check` OK) say "Data-name-1 must not reference a group item that contains a variable
occurrence data item"; the 2023 rows of `FileControlKeyRules` (SR3 of §2023:12.4.5.12.3 and §2023:12.4.5.6.3) test a
dynamic-length elementary item, a different rule.

Fix shape: a second catalog, `FileControlKeyRules.Rules1985`, two rows (prime and alternate key) that refuse a key whose item
is a group with `DataItem.HasOdoBeneath`, reported as COBOL0863 under an `ANSI §1985:` citation. `Screen` asks
`RulesFor(edition)`: the 2023 table everywhere, plus the 1985 rows below COBOL-2002. No new diagnostic code (COBOL7285 to
COBOL7287 returned). At 2002 and 2023 the same programs still translate, as the standards there do not state the rule.

Goldens: `1985-SR-IX_2_7_3-3` and `1985-SR-IX_2_5_3-3` (a refusal at 85 each, with their `wiseowl/diagnostics` records);
`FileControlKeyRuleDriftTests.Every1985Row_QuotesItsOwnNumberedRuleOfTheClauseItCites` re-derives each 1985 row from
`specs/ANSI_COBOL_1985.md`. Rows `1985-SR-IX.2.7.3-3` and `1985-SR-IX.2.5.3-3` recorded CONFORMS (`--edition 1985`, GAP 169 to 167).
