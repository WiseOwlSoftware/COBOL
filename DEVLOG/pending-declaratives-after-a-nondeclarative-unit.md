## 1985 fixes: a DECLARATIVES portion after a nondeclarative unit is refused by the ordering rule (PB5987)

PB5987 (PB5924 campaign): a DECLARATIVES portion written after a section was refused COBOL0901 ("DECLARATIVES is a reserved
word ... cannot be used as a user-defined word"), never naming the rule. Re-probe on this build (decl-after.cob, --std 85)
reproduced it. Cause: the note named `CobolErrorStrategy`, but the strategy only worded a parse failure: `procedureDivision`
was `declarativePart* sentence* paragraphDefinition* sectionDefinition*`, so the late portion could not parse and its first
word was read as a paragraph name. Fix at the root, the shape PB1146 already uses for the other format faults: the trailing
loop is `(sectionDefinition | declarativePart)*` (DECLARATIVES is reserved at every edition and follows a section's last
paragraph by its first word, so no decision looks further than a header), and `ProcedureFormatPass` refuses a portion that
follows a sentence, paragraph or section with COBOL2797 citing §1985:IV.6.4.1.1 / §2023:14.3 (both cite.py --check OK; the note's
§1985:II.6.1 did not carry the quoted text and is dropped). A portion among the leading ones is still "a second DECLARATIVES
portion". Output at 85 and 2023: `error COBOL2797: ... a DECLARATIVES portion that follows a nondeclarative section, paragraph
or sentence`. Goldens: pb5987-declaratives-first-legal (accept 85), pb5987-declaratives-after-section and
-after-paragraph (refused at every edition, COBOL2797). No inventory row closed (the note claimed none). No new code used
(COBOL7681-COBOL7683 returned). Design doc DESIGN-frontend-grammar.md updated for the grammar shape.
