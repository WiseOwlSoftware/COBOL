## PB4958: a declared free word gives its keyword back where its user-word reading fails

kb/Work PB4958 (wave 20261930 H). At an edition where §2023:8.9 leaves a reservation-gated word free, a program that declares
it had every occurrence retyped to a user-defined word, the keyword of a later edition's clause included, so the clause's
named edition refusal was lost: `01 TYPEDEF PIC X.` then `01 T PIC X TYPEDEF.` at COBOL-85 drew COBOL1941 "'TYPEDEF' is not
a clause" where the undeclared program draws COBOL0900, and a FACTORY paragraph header with an item named FACTORY drew
COBOL5506 where the plain class draws the class definition's COBOL0900. Both re-probed on bf46220cde.

**The note's fix was unsound, and measured so.** It proposed a `refusalWhereFree` row (PB4933's determination) per clause
word. With a TYPEDEF row, the legal COBOL-85 table `05 ENT OCCURS 3 ASCENDING KEY IS K TYPEDEF.`, keyed on an item named
TYPEDEF, drew COBOL0900: the clause reading of TYPEDEF parses at the end of the KEY list as well, and the row kept every
keyword reading of the word. Every clause of a data description entry competes that way, so no clause word could carry the
determination.

**The fix decides per occurrence.** `ReservationGateRewriter.ParseToFixpoint` runs a third round. Before a word's retype,
`KeywordsReadIn` records the tokens the pass read in a keyword position (outside `reservedGatedWord`, in a rule that spells
the token, never a syntax error's offender). A later pass whose user-word reading of such a token fails, a syntax error at it
or a closed format's error production holding it (`NameReadingsRefused`, through `ClosedFormats.IsErrorProduction`), gives
it its keyword type back (`TokenRetypes.KeptKeywords`; `RetypedToken` remembers the type across rounds of one stream) and the
source is parsed again. §2023:8.3.2.1 leaves the word two readings, and the parse picks between them where they differ. A
refused reading is settled before the pass's newly freed words: in the inner loop `OPTIONS.` read as an unknown
paragraph made DEFAULT of `DEFAULT ROUNDED MODE IS TRUNCATION` an offender, which freed DEFAULT and cost the restored
OPTIONS paragraph its clause (the pb4933 OPTIONS golden caught it).

**One mechanism, not two.** The per-occurrence rule covers PB4933's header words (SCREEN, LOCAL-STORAGE, REPOSITORY,
OPTIONS, COMMUNICATION, CD, the five comment paragraphs) and the source-unit words PB4933 had to exempt (FACTORY, CLASS-ID,
...), so the `refusalWhereFree` determination is deleted with its eleven row fields, its `gen-cobol-words.ps1` step 5d and
`IsRefusalWhereFree` emission, `CobolParserCoreBase.DeclaredGatedWords`, and the four drift tests that held the rows. Every
PB4933 golden keeps its diagnosis.

**Witnesses.** `DeclaredFreeWordKeywordTests` computes from the generated parser's ATN every gated word that opens an
alternative of a closed format's list and that §2023:8.9 leaves free at some edition (42 words: the USAGE words, BASED,
ALIGNED, GROUP-USAGE, PROPERTY, CONSTANT, TYPEDEF, the validation words, FORMAT, LABEL, SHARING, PADDING, RERUN, CRT, CURSOR
and the paragraph headers), requires a sample of each, and compiles each with and without an item of that name: the
diagnostics must agree by code. With the put-back disabled, 36 of the 42 fail. Goldens:
`cases/13.18.58-TYPEDEF/pb4958-typedef-clause-with-an-item-named-typedef` (refused at 85, COBOL0900),
`cases/11.4-FACTORY/pb4958-factory-paragraph-with-an-item-named-factory` (refused at 85, the class's COBOL0900) and
`cases/8.3.2.1-General/pb4958_key_named_typedef_is_a_user_word_85` (accepted at 85: SEARCH ALL on the TYPEDEF key prints 2).
No inventory row is re-verdicted (a diagnosis fix).

Self-review (architecture, full code, performance, duplication): one mechanism replaces two; the restore is per occurrence and acts only where a name reading failed; the tree walks run only for a program that frees a gated word; no rule is written twice. Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,672/22,672 · Unit 40,556/40,556 · Characterization 37/37 cases ran (skipped 0)` (run 000002-20261108T213342Z); the first run was RED on a harness non-observation only (TrackedTreeScopeDriftTests: drift_rules.py --scopes timed out at 100 % host load, 6 s standalone).
