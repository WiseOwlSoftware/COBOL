## Entry 3896 — 2026-11-21 03:36 PST — The 1985 prose requirements probed, and the first of their defects fixed: obsolete elements flagged, a misplaced DECLARATIVES named, the PICTURE precedence cited

The 1985 rule catalog takes only numbered rules, so normative prose was never checked against the compiler (kb/Work PB5924). A
mechanical scan measured 198 such sentences in 134 paragraphs (Appendix C excluded: it restates the body). At the owner's word
each one got a program compiled at `--std 85` and a status: 14 slices, a reader and a refuter each, on main at 8244f1aa4b.

45 sentences restate a row the catalog already has (the refuter sent 4 back as needing their own test; all 4 conform), 30 are
not normative, 103 were tested (95 conform, 8 do not) and 20 cannot be observed by a program (19 conform, 1 does not). The refuter
upheld 176 calls, overturned 6 and doubted 15. 271 golden files are drafted; they land once the catalog gives this text rows of
its own.

The defects, by mechanism: PB5986, the 1985 obsolete elements are neither flagged nor documented; PB5987, a misplaced
DECLARATIVES draws a reserved-word message; PB5988, the PICTURE precedence refusal cites the wrong rule; PB5989, the 1985 spacing
rules for unary, relation and logical operators are not enforced (`IF (A = 1)AND(B = 2)` compiles); PB5990, debugging lines are
not checked without the debugging mode; PB5991, a section header in area B compiles (reproduced: `S1 SECTION.` at column 11,
exit 0). PB5992 keeps two unruled leads. Seven sentences wait on the 1985 conformance statement (PB4997).

Beside it, a measured class: compiling every suite case refused at 85 and reading the messages found 20 diagnostic codes, 42
places, that cite only a later edition's rule where 1985 has its own; seven more turned up by hand. PB5993 holds the list and
the fix, one citation table keyed by edition with a drift test over the 85 refusal corpus.

### The 1985 obsolete elements are flagged and documented (PB5986), and two leads ruled (PB5992)

§1985:I.1.5.2.3 requires an implementation to support the obsolete elements of §1985:C.1, to identify them in its documentation and
to provide a warning for their use. At `--std 85` none of the 18 rows that stand for them carried `ObsoleteIn`, so COBOL0903 never
fired and no user document listed them (re-probe at 8244f1aa4b: AUTHOR, MEMORY SIZE and ALTER compiled silent).

The fix is structural. Setting `ObsoleteIn` to 85 alone would have broken the sites that ask whether a 1985 element exists,
because `ConstructRegistry.IsAvailable` read `StatusAt == Available` and an obsolete element is not that. `ConstructDialectStatus.IsInForceAt`
(Available or Obsolete) is now the one spelling of "in force", used by `IsAvailable` and by the two directive-catalogue readers that
had spelled its complement by hand. The 18 rows carry `obsoleteIn: 85` and cite their item; the two debugging rows move from 2002 to 85,
as item 18 states (the owner decision R61 had followed GnuCOBOL's edge). A USE FOR DEBUGGING section that a unit without WITH DEBUGGING
MODE makes comment never reached a parse, so `DebuggingLineRewriter.Rewrite` now returns the hidden sections' headers and the frontend
reports the first through the registry's verdict. ENTER stays refused at every edition (COBOL7240) and has no warning row.

`docs/manual/user-guide/editions.md` gains the table of the 18 items; the doc audit `scripts/docchecks/manual.py` holds it to the registry in both
directions, and the version matrix's obsolete cells now cover each row at 85 (the full Conformance population is the gate's run).

PB5992: the contained program named like its container conforms (§1985:X.1.3.8.1 reads the same as §2023:8.4.6.3); a SEGMENT-LIMIT
in a program with no sections is accepted by the text's silence on what makes segmentation "used", recorded in docs/conformance/3.8.
Neither is a defect.

No conformance-suite golden: the 1985 catalog has no rule id for I.1.5.2.3 yet (PB5924).

### 1985 fixes: a DECLARATIVES portion after a nondeclarative unit is refused by the ordering rule (PB5987)

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

Review round 1 (R1-1): a late portion followed by a paragraph or sentence still failed to parse (COBOL5506). The trailing loops now read `declarativePart sentence* paragraphDefinition*` as one item, so COBOL2797 names the ordering rule wherever the portion stands; goldens pb5987-declaratives-then-paragraph and pb5987-declaratives-section-then-paragraph refuse at all four editions.

### The 1985 "no digit position" PICTURE refusal cites the paragraph that states it

Note PB5988 (1985 prose-requirement campaign). The composition validator's refusal of a character-string with no digit
position and no character position (COBOL1934, `PIC $`) cited §1985:VI.5.9.4 GR3a at `--std 85`. That rule defines a numeric
item's symbols and its 1 to 18 digit positions (cite.py: the quoted text is not in it). The rule the refusal enforces is the
prose of §1985:VI.5.9.6 ("At least one of the symbols 'A', 'X', 'Z', '9', or '*' or at least two occurrences of one of the
symbols '+', '-', or 'cs' must be present in a PICTURE character-string"; cite.py OK).

Re-probe on this build (`--std 85`, `PIC $`): the refusal reproduced and now reads "(ISO §1985:VI.5.9.6)". The fix is the
1985 argument of `Rule("SR12", "§1985:VI.5.9.6")` in `PictureComposition.cs`. The sibling sweep found the other `GR3a` citations
(`EditionContext.cs`, 18 digit positions) correct: GR3a really says "must range from 1 to 18 inclusive".

Golden: `13.18.40-PICTURE/pb5988-picture-no-digit-position-cites-1985` with `wiseowl/diagnostics/...json` pinning
"(ISO §1985:VI.5.9.6)" at 85. Rows closed: none (the catalog holds no row for the VI.5.9.6 prose). The message's symbol list at 85 is the 1985 VI.5.9.6 set (the review's round-1 finding, fixed on the branch).

Review round 1 (R1-1): the refusal message at --std 85 still listed the 2023 symbol set (N, 1, character-1). It now names the §1985:VI.5.9.6 set (A, X, Z, 9, *; +, -, cs) through Edition.Cite, the 2002+ text unchanged; the golden expect pins the 1985 list. The 2023 arm SR12 a became SR12 (cite.py rejects a sub-item a). PB5988 Sites no longer names PictureAnalyzer.cs (R1-2).

### PB5991 retired: my reproduction was wrong

PB5991 claimed that a section header at column 11 is in area B and compiles at `--std 85`. Area A is columns 8 through 11, so
column 11 is area A and the program is legal: the Mythos session's "reproduction" (`bsec.cob`, exit 0) ran a legal program and
read its acceptance as a defect. The implementer of wave 20262117 group D found it: a section header that does begin in area B
(column 12) is refused COBOL3734 by AreaPlacementPass (PB3147, PB5313), with negatives in the suite. Retired, no change.

