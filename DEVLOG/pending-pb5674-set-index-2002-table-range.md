## At --std 2002 with EC-BOUND-SUBSCRIPT checking on, SET of an index outside its table raises and leaves the index unchanged

**PB5674.** Re-probed on the branch build before the change: with `>>TURN EC-BOUND-SUBSCRIPT CHECKING ON`, a table of 10 and
`SET IX TO 3`, `TO 11`, `UP BY 20`, `DOWN BY 5`, the run printed `S1=03 S2=11 S3=31 S4=26` and ran no declarative. §2002:14.8.35.3
GR2a.1 (with §2002:8.4.1.2.3 GR2) and GR3b make a value outside 1 through the highest occurrence the EC-BOUND-SUBSCRIPT condition,
the SET unsuccessful and the index unchanged.

**The fix is 2002 alone.** The 2014 text (§2014:14.9.35.3 GR2a.1, GR4) words the range as EC-RANGE-INDEX against the
implementor index range and says the index may refer to an occurrence "not a valid occurrence within this table"; 2023 agrees. The
note's `editions` listed 2014; the probe at `--std 2014` stores 11, as that text requires, and the note now says `[2002]`. A register member,
`DialectBehavior.SetIndexOutsideTableStored` (VCR row 7.126, edge 2014), carries the difference (the inline-edition-comparison drift test refused a predicate in `EditionInfo`). The binder now records each index-name
receiver's table maximum (`SetIndexTarget.Highest`, the OCCURS count) and whether Format 1's sender is arithmetic-expression-1
(`BoundSetTo.ValueIsArithmeticExpression`: index-name-2 and identifier-2 are placed unchanged, GR2a.2 and GR2a.3). `SetEmitter`
emits `CobolIndex.WithinTable` per receiver for Format 1 and `CobolIndex.AugmentWithinTable` for Format 2 where the predicate holds;
PERFORM VARYING and SEARCH keep `CobolIndex.Augment`, as the 2002 text gives them no such rule. With checking not enabled the outcome
is implementor-defined (D-SET-INDEX-UNCHECKED) and the value is still stored; the determination now names the checked arm.

**Goldens.** `set-index-outside-table-checked` (2002; ten legs, two tables), `set-index-outside-table-below-2002` (refused at 85,
COBOL0900), `set-index-outside-table-unchecked` (WiseOwl determination witness). **Rows.** `2002-GR-14.8.35.3-2` and `-3` closed
CONFORMS (GAP 1157 to 1155). Design: `docs/DATA_MODEL_DESIGN.md` D3 now states the 2002 exception.
