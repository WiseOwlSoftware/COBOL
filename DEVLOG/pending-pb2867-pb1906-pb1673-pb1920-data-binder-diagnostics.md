## PB2867, PB1906, PB1673, PB1920: a TYPE subject composes in its bounds phase, and four refusals name what the source really says

Wave 20261769 group B took four DataBinder notes. Each was re-probed on the base 802352531 before any code changed.

**PB2867 rejected legal source; the note had the wrong expected outcome.** `05 V TYPE T OCCURS K.` with `01 K CONSTANT
AS LENGTH OF M OF V (1).` drew COBOL1547 "the data-name is not defined". The note asked for the being-described
refusal instead, but §13.18.57.4 GR1 makes a TYPE clause "as though the data description identified by type-name-1 had
been coded in place of the TYPE clause", so the source is `05 V OCCURS K. 10 M PIC X(3).`, which PB1941 landed as
legal: §13.10.3 SR4 forbids only a length that depends on K, and an element's subordinate does not. The explicit twin
already printed `3 9`. The root was not `IsBeingDescribed`, as the note guessed. `ComposePendingSubjectsAhead` skipped
every subject whose description was open, and that includes the entry's own item while its OCCURS bounds bind, the one
moment PB1941 gave an entry with subordinates a constant to demand. So V's TYPE clause was never expanded and `M OF V`
found nothing. The fix composes the subject unless it is a group whose subordinate ENTRIES are still being described
(`HasSubordinatesBeingDescribed`, the ancestor half of `IsDescriptionOpen`). Measuring V itself or G stays SR4. The
2002 golden `13.10-Constant/pb2867_constant_length_composed_open_entry` prints `3 2 5`, `50` and `ABC` (TYPE,
BYTE-LENGTH, SAME AS); the negative `pb2867-constant-length-composed-cycle` measures G (SR4).

**PB1906: "not defined" about a name that is right there.** A type-name in `MOVE "A" TO T` drew "no declaration in
this source element gives the name 'T' … Check the spelling". The sweep found the same false verdict at four more
sites: a type declaration's member (`M`, `M OF T`); every non-data kind as a file clause operand (`FILE STATUS IS T |
AL | KC | C-ON`, `RELATIVE KEY IS IX`); a type-name as an OCCURS DEPENDING ON object (COBOL0851 "is not defined");
and a type declaration's level-88, both in `IF M-ON` and in SET Format 4's "no level-88 entry … declares it". The note
asked for one query, and it now exists: `DataBinder.NonDataDeclarationOf` answers what a name is declared as when no
data item answers it, and `NonDataDeclaration.DeclaredAs` words that answer once. `ReportUnidentified` keeps its
kind-specific arms (screen COBOL2364, index COBOL1637, PB567's condition-name text) and words every other kind through
the query. `ResolveClauseOperand`, `OdoResolve` and `RejectSetConditionName` ask the same query. No diagnostic code
changed. Found on the way: `DISPLAY K` after a refused constant entry drew "K is declared as a constant-name, not as a
data item". It is now the refused-declaration consequence (COBOL2364). A first attempt to stay silent instead drew
COBOL2362, because a refused statement must answer with an error of its own. Witnesses: `NonDataNameReferenceTests`
(23 cases) and the negative `13.18.58-TYPEDEF/pb1906-typedef-member-without-group`.

**PB1673: an undeclared NULL at COBOL-85.** `VALUE NULL`, `CONSTANT AS NULL`, `DISPLAY` / `MOVE` / `STRING NULL` at
`--std 85` drew COBOL2576, the 2002 §8.4.3.10.3 SR1 rule. The SR1 contexts that admit NULL (a relation, `INITIALIZE …
BY NULL`) went further and treated it as the predefined NULL. §8.9 reserves NULL only from 2002, and the reservation
gate retypes a DECLARED NULL to a user word at 85 (PB655). An undeclared NULL token there therefore names nothing,
which is the §8.4.2.1 undefined reference PB655 already gave SELF, SUPER, NULL and ADDRESS in a receiving position.
`UnreservedKeywordReference` states that verdict once. `PredefinedNullRule.Report` asks it first;
`ExpressionBinder.NullAdmittingOperand` refuses through it where the edition has no predefined NULL; and
`RefuseNonReceivable` lost its private copy of the text. The grammar kept its superset parse. Negative
`8.3.2-COBOL-words/pb1673-null-undeclared-at-85`. The first gate's one Conformance red was rooted here:
`pb791-currency-null-literal-7` pinned COBOL2576 at 85 too, and its `refuse-at` is now 2002 and later.

**PB1920: a refused CURRENCY SIGN clause cascaded.** `CURRENCY SIGN IS SPACE WITH PICTURE SYMBOL "#"` followed by `PIC
#9.99` drew SR18 and then COBOL0808 for '#'. The same happened after SR23 a), SR23 b) and NULL. `SwitchBindCurrency`
now judges literal-7 (the string) and literal-8 (the symbol) independently, as SR23 names them. `RegisterCurrency`
binds a valid literal-8 even when the string was refused, as `CurrencyDefinition.OfRefusedClause`: SR21 does not
compare against the placeholder (so a later valid clause takes the symbol over), it joins no §15.68.3 r3 count, and
`PictureAnalyzer` asks no SR28 class of it. Witnesses: `CurrencySignRefusalTests` (exactly one error per refusal) and
the negative `12.3.7-SPECIAL-NAMES/pb1920-currency-string-refused-symbol-stands`.

**Self-review.** The four lenses checked these points. Architecture: one query and one wording per verdict, with no
second copy left behind (`RefuseNonReceivable`'s text moved and was not copied). Code: each message cites a
`cite.py`-checked rule. Performance: every new lookup runs only on an error path; the composition filter changed but
its call count did not. Duplication: `semgrep/verify.py` holds every count. No diagnostic code was allocated
(COBOL5503-5505 returned) and no inventory row changed (each note's `closes_rows: []` carries its reason). The gate's
verdict is recorded in the wave report.

**Branch review round 1: two blocking findings.** R1-1: PB1906 made `SetBinder.BindSetCondition` pass
`dref.cobolWord()` to the declaration query. In the LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER alternatives of
`dataReference` that word is the qualifier or absent, so `SET LINAGE-COUNTER TO TRUE` crashed with a
NullReferenceException where main reports COBOL1639. `ReferenceResolver.WrittenWordOf` now returns the word a
reference names (null for a register), and both the SET query and `SetAlterBinder.SwitchCondOf` ask it. The message
names a TO FALSE phrase as written; this branch and main's PB1314 (entry 2786) made that fix the same way, and the
merge of main keeps one `toTrue` parameter in `StatementValidation.RejectSetConditionName`, documented once. R1-2: SET reads NULL itself in its
object-reference arm, not through `NullAdmittingOperand`, so `SET IX TO NULL` at 85 still drew the 2002 §14.9.39.3 8)
refusal (COBOL0867). That arm now asks `UnreservedKeywordReference` before it selects a format. Witnesses: four
`NonDataNameReferenceTests` register cases and the negative `8.3.2-COBOL-words/pb1673-set-null-undeclared-at-85`.
SELF and SUPER at 85 also draw the 2002 OO rules. That is a separate mechanism with its own sites, so the report
records it as a lead.
