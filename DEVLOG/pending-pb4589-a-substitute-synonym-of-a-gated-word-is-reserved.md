## PB4589 - a SUBSTITUTE synonym of a reservation-gated word is reserved, not a user word (the parser's reservation reads asked the overlay about the original)

**Mechanism.** The lexer's `>>COBOL-WORDS` retype turns a synonym (EQUATE literal-2, SUBSTITUTE literal-5) into its original's keyword token
and spells it as the original. `CobolParserCoreBase.reservedHere`, `userWordHere` and `IsKeywordReadingHere` then resolved that already-resolved
word through the group's overlay (`Canonical`), where SUBSTITUTE had withdrawn the original (literal-4, a user word by GR4): "no keyword,
no reservation". The synonym was therefore neither a reserved word nor the facility it stands for (ISO 1989:2023 section 7.3.10.4 GR4;
section 8.3.2.1 rule 1).

**What the re-probe showed (own build).** `SUBSTITUTE "RECEIVE" BY "RCV"` then `01 RCV PIC X.` compiled clean (the reservation gate retyped RCV a user word);
`p3t.cob` (CONSTANT/CNST) drew a COBOL1639 clash between the synonym and the freed original. Two sibling arms of the same read: `DISPLAY "A" CMT.` after
`SUBSTITUTE "COMMIT" BY "CMT"` printed `AC` (the token's text COMMIT named the item COMMIT as DISPLAY's operand, CMT was no statement), and
`CRTX STATUS IS ST` after `SUBSTITUTE "CRT" BY "CRTX"` drew COBOL1970 (legal source rejected; `reservedHere("CRT")` was false).

**Fix.** A keyword token is asked about its own word: `reservedHere`, `userWordHere` and `IsKeywordReadingHere` read `ReservedWords` / `ReservedWordSet.Default`
for the token's spelling and no longer resolve it a second time (the once-only rule of `CobolWordsRewriter.CanonicalWordOf`). The funnel had the same
bug one stage later: `VersionConformancePass.FlagReservedUserWord` looked the token's text up in the group's overlay, so a declaration of the synonym was
never reported (only a use, through the parser's syntax-error arm); it now asks a non-`IDENTIFIER` token of `ReservedWordSet.Default`, and the
overlay only for an `IDENTIFIER` (RESERVE, and synonyms of words the lexer has no token for). `docs/rearchitecture/DESIGN-cobol-words-directive.md` states the rule.

**Goldens.** `pb4589_cobol_words_substitute_synonym_of_commit_is_the_commit_statement` (accept 2023, output `A`, `C`; it printed `AC` before) and the refusal
`pb4589-cobol-words-substituted-receive-synonym-is-no-user-word` (refuse-at 2023, COBOL0901 naming RCV and RECEIVE). Unit: two tests in `CobolWordsDirectiveTests`
(declaration-only synonym refused and the original a name; the CRT clause and the COMMIT statement reached through the synonym).

**Rows.** None claimed, none closed (`closes_rows: []` with its reason): GR-7.3.10.4-4 was already CONFORMS, the landing adds witnesses.

**Leads (not fixed).** The synonym's declared item is named RECEIVE (the token's text), so `01 RCV` beside `01 RECEIVE` also draws a COBOL1639 clash after the COBOL0901;
the primary diagnostic is right and the second is the same retype-overwrites-text shadow.

**Self-review.** Architecture: one reading ("a keyword token is already resolved") in four sites, none with a second copy of the overlay lookup; `Canonical` remains only for text-tested words.
Full code: sibling sweep over every `Canonical(` / `Resolve(` / `RejectsAt(` caller (pasted in the report). Performance: the predicates run inside ANTLR prediction and now do less work.
Duplication: none added. Gate: see the report.
