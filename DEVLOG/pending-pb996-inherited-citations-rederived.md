## PB996: inherited clause numbers re-derived (STOP RUN, composite, boolean relation, SAME AS/TYPE, SORT/MERGE collating, COMP-5)

Fix-lane wave 1115 group G took kb/Work PB996, the register's catch-all for code comments and diagnostic messages
that cite the wrong rule. The behaviour at every site was already right; the text sent the reader to the wrong
clause. Each citation was re-derived with `cite.py --check` against the sentence the code implements.

**Re-probe.** Five of the note's sites were already fixed on main and are recorded as such: SortEmitter's GIVING
space-fill cite (w1003b), `SortBinder.TableSortKeyUnsupported` (w1004c), the `sortCollatingPhrase` grammar comment
(PB1139), the retired CBL3617/CBL3618 descriptors (PB756) and StatementBinder's orphaned STOP status-phrase comment
(PB1178). The rest still read wrong on base `ea3d015b9`.

**What changed.** STOP RUN cited §14.9.43, which is the STRING statement, at five sites (the note counted four:
`ProgramEmitter.EmitEntryWrapper` was the fifth) and in the pipeline design doc; all now cite §14.9.42.4 GR1.
COBOLNET0805's "§14.7 rule 2", which `cite.py` rejects, is §14.7.7 rule 2 a). COBOLNET0844's "§8.8.4.2.1 F1
SR2/SR3" names a General subclause that has no syntax rules; the rules are §8.8.4.2.3 SR2/SR3, and the
object-reference comment's "§8.8.4.2.1 Format 3 / SR5" becomes §8.8.4.2.2 Format 3 / §8.8.4.2.3 SR5. COBOLNET1557's
SAME AS cycle through a TYPE clause breaks §13.18.49.3 SR4 and §13.18.57.3 SR1, the same cycle seen from each
clause, so the message now names both. The SORT/MERGE COLLATING SEQUENCE phrase printed both verbs' rules in one
COBOLNET0898 message, and the MERGE FOR-phrase repeat cited SORT's general format. `SortBindCollating` now takes the
verb and reads SORT §14.9.40.2/SR1/SR2 or MERGE §14.9.24.2/SR5/SR6 from one table, `SortCollatingClauses`; six
negatives pin the clause in their `expect`. The `ExceptionEngine.DataConversionError` doc said the raise "never
throws", but it runs declarative selection and a RESUME unwinds through `RaiseResumeSignal`. The
`NonfatalDispatcher` rationale said a CALL does not save the checking flags, which PB841 made false. Both docs now
state the current contract (§14.9.49.4 GR3, GR4 a)).

**COMP-5.** About thirty sites cited §13.18.60.4 GR12 for COMP-5's full-container range. GR12 governs only
BINARY-CHAR to BINARY-DOUBLE; COMP-5 is an extension the standard does not define, and its range is the owner's
carrier decision recorded as CONFORMANCE.md DOC-A.1-208 (kb/Work R10). COMP-5-only sites (16-byte containers,
`PIC S9(4) COMP-5` examples) now cite DOC-A.1-208; sites about both usages cite `§13.18.60.4 GR12 / DOC-A.1-208`.
This covers src, tests, golden comments, CONFORMANCE.md and two design docs.

**Why the audit missed them.** `audit_code_citations.py` reads no designator in "§14.7 rule 2". Its RULE and
DIAG-UNQUALIFIED arms skip a clause with no rule block of that kind, so "§8.8.4.2.1 F1 SR2" got through. Its SUBJECT
arm needs the context to name the wrong construct, and "STOP RUN" never spells "STRING statement". A NO-BLOCK arm was
measured on the tree at 545 findings, most of them back-references to a sibling subclause and function-argument
rules. That is a noisy heuristic with a backlog, so it goes to the report as a lead rather than into this slice.

**Rows.** None: the note is process_only and every inventory row it touches was already OK, so `closes_rows: []` with
a reason. **Self-review:** one table for the collating clauses, no second copy of a message; no behaviour change, no
hot path touched. **Gate** (run 20261018T061112Z-1d9678): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN ===`, with every changed COBOLNET0898 / COBOLNET0844 negative passing in leg 1 at each edition.
