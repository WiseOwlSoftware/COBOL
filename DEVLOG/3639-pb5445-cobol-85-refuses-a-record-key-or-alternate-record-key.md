## Entry 3639 — 2026-11-14 19:08 PST — PB5445: COBOL-85 refuses a RECORD KEY or ALTERNATE RECORD KEY naming a group that holds an OCCURS DEPENDING ON table

Re-probe on a fresh Debug build: `cobol --std 85` translated both drafts (`RECORD KEY IS IG`, `ALTERNATE RECORD KEY IS IG`, where
`IG` is a group holding `IGT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON IGN`) with no diagnostic. §1985:IX.2.7.3 SR3 and
§1985:IX.2.5.3 SR3 (both `cite.py --check` OK) say "Data-name-1 must not reference a group item that contains a variable
occurrence data item"; the 2023 rows of `FileControlKeyRules` (SR3 of §2023:12.4.5.12.3 and §2023:12.4.5.6.3) test a
dynamic-length elementary item, a different rule.

Fix shape: a second catalog, `FileControlKeyRules.Rules1985`, two rows (prime and alternate key) that refuse a key whose item
is a group with `DataItem.HasOdoBeneath`, reported as COBOL0863 under an `ANSI §1985:` citation. `Screen` asks
`RulesFor(edition)`: the 2023 table everywhere, plus the 1985 rows below COBOL-2002 and the 2002 rows at 2002. No new diagnostic code (COBOL7285 to
COBOL7287 returned). Review round 1 (R1-1) found that COBOL-2002 states the same refusal (§2002:12.3.4.12.2 SR3 and §2002:12.3.4.5.2 SR3, with §2002:13.16.36.3 GR8 making a group holding a variable-table OCCURS a variable-length data item), so the fix also adds `Rules2002`, two rows in force at 2002 alone; the same programs translate at 2014 and 2023, which narrow the term (§2014:8.5.1.8). The goldens now refuse at 85 and 2002 and cite the 2002 rule ids, and `FileControlKeyRuleDriftTests.Every2002Row_QuotesItsOwnNumberedRuleOfTheClauseItCites` re-derives the rows from `specs/ISO_COBOL_2002.md`.

Goldens: `1985-SR-IX_2_7_3-3` and `1985-SR-IX_2_5_3-3` (a refusal at 85 each, with their `wiseowl/diagnostics` records);
`FileControlKeyRuleDriftTests.Every1985Row_QuotesItsOwnNumberedRuleOfTheClauseItCites` re-derives each 1985 row from
`specs/ANSI_COBOL_1985.md`. Rows `1985-SR-IX.2.7.3-3` and `1985-SR-IX.2.5.3-3` recorded CONFORMS (`--edition 1985`, GAP 169 to 167).

