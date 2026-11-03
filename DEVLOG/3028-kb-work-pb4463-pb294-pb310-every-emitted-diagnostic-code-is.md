## Entry 3028 — 2026-11-03 03:34 PST — kb/Work PB4463, PB294, PB310: every emitted diagnostic code is a DiagnosticCatalog descriptor, and COBOL1512, COBOL0898, COBOL1542, COBOL1532, COBOL0844, COBOL0864, COBOL0897 and COBOL1530 split one code per rule

**What the note said and what the tree held.** PB4463 named 19 codes with no catalog row (0850-0857, 0898, 1522, 1523, 1525, 1526, 1532,
1539, 1542, 1586-1588) and COBOL1512 shared by three rules. Re-probing on the merged PB2296/PB2352/PB2355 head showed the population is
larger: 93 codes at 255 `Error`/`Warning`/`Report`/`ReportError`/`Emit` sites passed a `"COBOLnnnn"` literal, 85 of them with no
descriptor. PB294 and PB310 (both open, both MAJOR) had measured the same hole from the test side: `EveryEmittedCode_IsACatalogDescriptor`
carried a `(?<![\w.])` look-behind that skipped every qualified call, which is how nearly every site is written, so the fact read 2% of its
subject. A drift test that "every emit site names a descriptor" is true only of the whole population, so the whole population was done
(CLAUDE.md rule 5: a stated scope is an estimate).

**Fix shape.**
* One descriptor per rule where a code had been reused for unrelated rules, which took 23 new codes (COBOL5977-COBOL5999, all
  allocated and used): COBOL1512 (LOCK MODE MULTIPLE, the lock phrase under AUTOMATIC locking, OPEN SHARING needing LOCK MODE),
  COBOL0898 (ten rules: VALUE national, VALUE boolean, THROUGH on a boolean condition-name, numeric-edited VALUE class, undeclared
  alphabet-name, ALPHABET branch, SPECIAL-NAMES literal class and emptiness, collating-sequence alphabet kind, ORDER TABLE repeated),
  COBOL1542 (ANY LENGTH: placement, PICTURE, other clauses, header reference, REDEFINES target, CALL argument), COBOL1532 (STRONG
  level, redefined, renamed), COBOL0844 (arithmetic operand, boolean relation, FUNCTION CHAR), COBOL0864 (READ KEY, LINAGE-COUNTER),
  COBOL0897 (USE rules, PERFORM WHEN file) and COBOL1530 (TYPE undefined, TYPE self reference). Every test and refusal golden that
  asserted a moved rule's old code was re-pointed (the refusal files under `conformance-suite/wiseowl/diagnostics` and the test classes that named them).
* One descriptor per code where the sites are one family of the same kind of rule (intrinsic-function argument bands 1504, 1514, 1516,
  1546; the PICTURE string band 0808; the USE band 0897; the currency band 0890-0892; the boolean-expression band 1511). Each title names
  every clause of the band and quotes one rule, checked with `cite.py --check` (105 quotations checked; five failed and were corrected).
* The 7 front-end sites (`TurnDirectiveProcessor`, `CopyProcessor`) pass `DiagnosticCatalog.X.Code`, the front end's convention; COBOL0902
  routes to the existing `EditionRemovedConstruct`.
* `SpecialNamesBinder.Literals.cs` carried its SR10/SR11 refusals through one `Report` closure and a `FigurativeSlot.Refuse` delegate; they
  now carry a descriptor per rule (`ClassCode`, `SymbolicCode`) so the two rules cannot share a code again.
* `DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor` and `SplitCodes_HaveNoBareEmitLiteral_InCompiler` (a two-code
  allowlist) are replaced by one fact, `NoEmitCall_TakesACodeLiteral`, over every emit call shape. It was made to fail before it was
  trusted: it caught two unqualified `Error("COBOL0801"/"COBOL0802", ...)` calls in `EditionContext.CheckDigitCapacity` that the
  semgrep rule and the old fact both missed. The semgrep rule `wiseowl-cobol-raw-diagnostic-code-literal` is at 0 findings and left
  `scripts/semgrep/baseline.json` (230 -> 0); PB4462 records that semgrep is blind over part of `DataBinder.cs`, so the unit fact is the net.

**Goldens.** Three new refusals for rules that had no witness naming their code: `pb4463-odo-integer-1-not-less-than-integer-2` (COBOL0850,
SR16), `pb4463-symbolic-characters-in-undeclared-alphabet` (COBOL5985), `pb4463-using-parameter-not-linkage-root` (COBOL0888). The other
new rules already had a refusal or a unit test; their expectations moved to the new codes.

**Registered.** PB4463, PB294 and PB310 landed (`closes_rows: []`: no inventory row was open on the mechanism). Leads in the report: the
dead `FUNCTION CHAR` arm (COBOL5996 is reached only after COBOL1627 refuses the same argument) and the unreachable `RemovedIn` arm
(COBOL1503: no catalog function carries a `RemovedIn`); COBOL0806 and COBOL1564 are declined facilities (Annex A.3) that should flow
through `EditionContext.Refuses` with `Annex: DeclinedAnnex.A3`; a band descriptor splits into its rules when a rule needs its own
code.

**Review round 1 (R1-1, R1-2).** The branch review found that "every emitted code is a descriptor" was not yet true: the
`BoundRejected.Report(ctx.Edition, "COBOLnnnn", ...)` overload (50 sites), the code tuples of `Sr8PicturelessVerdict` and
`ClassConditionModel.Wording`, a `ReceivingBar` constructor, `OoNameResolution.Resolve`'s code argument and five `EditionDiagnostic`
constructors still wrote a literal, and both the unit fact and the semgrep rule needed the literal to be an emit call's first argument.
The string overload is deleted, every site names a descriptor, 17 descriptors are new (COBOL0712-0714, 0812, 0813, 0818, 0823-0826, 0848,
0865, 0866, 0870, 1521, 1568), and the unit fact is now `NoSourceLine_WritesACodeLiteral`, which reads every line of `src/` outside the
catalogue, so the next shape is caught whatever it is. COBOL0827, COBOL0844 and COBOL1523 each named one rule while others emitted them;
the minority rules took COBOL6000 (class condition SR4), COBOL6003 (SR8), COBOL6001 (CAPACITY register as a receiving operand) and COBOL6002
(SELF/SUPER placement), and the refusal goldens and tests that asserted the old codes moved with them.

