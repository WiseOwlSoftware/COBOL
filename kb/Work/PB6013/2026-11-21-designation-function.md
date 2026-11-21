# PB6013 landed (wave 20262131 group h, branch train85b-cite): the designation is one function

**Re-probe** (`--std 85`, this branch's build, the note's repro `E:/COBOL-coord/scratch/w20262131/pad85/src/PADLIT2.cob`):
`error COBOL6848: PADDING CHARACTER IS "**": literal-1 is not a one-character nonnumeric literal (§1985:VII.2.7.3 SR1: "Literal-1
must be a one-character nonnumeric literal.")`; the note recorded `(ISO §1985:VII.2.7.3 SR1: ...)`.

**Mechanism.** `EditionInfo.Designated(citation)` (`src/WiseOwl.Cobol.Editions/EditionInfo.cs`) is the one place a message's citation
takes its standard's name: `ISO ` before a rule of ISO/IEC 1989 (its 2002, 2014 and 2023 editions), nothing before a 1985 or 1989 rule
in the edition form. `EditionInfo.CiteRule(y1985, later, y2002, y2014)` is `Designated(Cite(...))`. A 1989 rule (the intrinsic
functions' amendment) is bare too: `FUNCTION MEDIAN argument-2 is of class alphanumeric; §1989:A.2.2 requires class numeric`.

**Moved** (mechanically, then by hand): 248 literal `ISO §1985` strings; 191 `ISO {..Cite(` and `"ISO " + ..Cite(` lines; 135
multi-line joins (`... (ISO "` then `+ X.Cite(` on the next line) that the ratchet's per-line regex never counted; 216 `ISO {helper(...)}`
interpolations whose helper returns a 1985 rule at 85; 19 hand-shaped sites (`StatementValidation.IoRule` now designates,
`SpecialNamesBinder.LiteralPhraseRules.CiteRule`, `CobolErrorStrategy.CiteRuleAt`, `ConditionalPhrases`, `ClosedFormats`, `ReservedWords`).

**Drift test.** `StandardDesignationDriftTests` is no longer a ratchet: it reads whole files (multi-line joins included) and refuses
any literal `ISO §1985` in `src/` and any `ISO {` or `ISO " +` before a computed citation outside `EditionInfo.cs` (the runtime has no
edition and is outside the computed half). 0 hits; a theory plants each form.

**Not moved, by decision:** the doc comment of `src/WiseOwl.Cobol.Listing/ListingDataMap.cs` line 86 keeps `ISO §1985:XIV.2.2`: the
published listing JSON schema carries that comment as a description, and changing it means a schema version bump
(`ListingJsonSchemaDriftTests`, DESIGN-listing.md section 7.3) for a comment. Thirty source lines print `ANSI ` before a 1985 citation (for example
`FileControlKeyRules.cs` rows `1985-FMT-...`): correct, and a second spelling of the same designation; left for a later sweep.
