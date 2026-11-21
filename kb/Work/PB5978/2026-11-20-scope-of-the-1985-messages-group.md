# PB5978 addendum, 2026-11-20: what the 1985 messages group (cluster PB5979) does here

The `DeclarationFact.ClassPhrase` 1985 half landed with PB5975 (PR #1910). This group does the remaining 1985 work only:
COBOL2326 at `--std 85` cites §1985:VI.6.23.4 GR3c, the rule a `SET identifier-1 TO integer-1 | identifier-2` with a
non-index identifier-1 breaks (see the addendum `2026-11-20-cobol2326-cites-a-satisfied-rule.md`), with a golden at 85.
The 2002 and 2014 citations (COBOL2692's `Cite(1985, 2023)`, and COBOL2326 and ClassPhrase at those editions) are NOT
changed here: 2002 work is frozen until 1985 is complete (owner 2026-11-15/16), and this note stays open for them. Shape the
1985 change so those editions are a table entry later, not a second switch.
