## PB5484, PB5480: a COBOL-85 arithmetic statement with no ON SIZE ERROR phrase ends only itself; a zero divisor or a zero base no longer ends the run unit; a relative or indexed READ outside the reader record size answers 04

**The defect.** At `--std 85`, `COMPUTE R = Z ** 0` (Z = 0) and `COMPUTE R = 5 / Z` ended the run unit with
`abnormal run-unit termination: EC-SIZE-EXPONENTIATION (fatal)` / `EC-SIZE-ZERO-DIVIDE (fatal)`, exit 1, re-probed on this
tree before the fix. That is the 2002+ fatal exception condition, which 1985 does not have. §1985:VI.6.4.2 GR3 says that with no
phrase "the values of the affected resultant identifiers are undefined", the other resultants are stored as if no size error had
occurred, and "control is transferred to the end of the arithmetic statement". §1985:B.2.2 item 45 adds that "the program will
continue". GR1 and GR2 make the exponentiation violation and the zero divisor size error conditions like any other. The kernels
raise `CobolSizeError` whether or not checking is on (PB1605), and at 85 nothing caught the raise.

**The fix.** I added one dialect behaviour, `DialectBehavior.UnphrasedSizeErrorIsFatal` (changed in 2002; VCR row 7.71; the branch review moved it from an `EditionInfo` property into the registry). When
`ArithmeticEmitter.EmitArith` emits a COBOL-85 arithmetic statement with no phrase, it uses the two-phase shape that a statement
with the phrase uses, with two settings. No condition is checked (`SizeChecks.None`), so every render and landing stays the
unchecked one and an overflowing resultant still keeps its low-order digits. Every raise is caught. A resultant whose value
cannot be formed keeps its content (GnuCOBOL 3.2's disposition, recorded in DOC-A.1-70), the per-receiver try still stores the
other resultants, and the next statement runs. A debugging section for an item that kept its content does not run (§1985:XV.3.2.4
GR6; probed). A reference-modifier position expression rides `EmitArith` but is not an arithmetic statement, so it keeps the
termination, and so does a 1985 condition (`IF 5 / Z > 1`, probed). The condition case is reported as a lead.

**Witnesses.** The golden `cases/VI.6.4@1985-COMMON-OPTIONS-AND-RULES-FOR-STATEMENTS/pb5484_no_phrase_size_error_continues` runs
the change record's own STATEMENT-1 and STATEMENT-2 (DIGIT2 = 10, and the program continues). It also runs a zero base, a power
with no real result, a nested quotient and DIVIDE GIVING REMAINDER by zero. It displays only values the standard defines. I added
`SizeErrorDispositionTests.Cobol85_NoPhraseSizeError_KeepsTheResultantsAndContinues` (7 rows, pinning the kept value) and its
control `Cobol85_NoSizeError_StoresTheResultants`. Row 1985-GR-VI.6.4.2-3 was CONFORMS, but its notes claimed the EC-SIZE
termination was unreachable at 85. Batch `w20262025a-pb5484-1985-no-phrase-size-error` restates those notes and adds the code
site and the witnesses (1985 GAP 232 -> 232). I updated DOC-A.1-70, NUMERIC_DESIGN.md and the `CobolSizeError` docs.

**Branch review finding R1-1 (wave 20262027 group B).** The reviewer found the 85-versus-2002 fact had been written as a new
`EditionInfo` property, outside the `DialectBehavior` registry that VERSION_CHANGE_REFERENCE Table 7 and its drift test hold in
both directions. It is now `DialectBehavior.UnphrasedSizeErrorIsFatal` (VCR row 7.71, doc-facts row, public API entry); the property
is deleted. R1-2 (a dead flag and an unreachable outer catch in simple 85 statements) was answered no change: the shape is the
two-phase shape every phrase statement already has, and eliding the outer try needs per-verb knowledge of whether an initial
evaluation exists outside the receiver stores. Unmeasured; registered as a lead.

## PB5480: a relative or indexed READ outside the reader's record size answers 04, not 00

**The defect.** A connector opened INPUT on a zero-byte relative or indexed file validates nothing at OPEN (`StoreFormat.Empty`
records no attributes), so records another connector wrote later under wider bounds read back with status `00`, the DEPENDING ON item
above the reader's maximum. The sequential READ already answered `04` (§1985:VIII.4.5.4 GR16, §1985:IX.4.5.4 GR18, §1985:IX.1.3.4 GRL2.1).
Probes `c4c` (relative) and `ixzero` (indexed) at `--std 85`: `R READ1 00`, `R READ2 00` / `R READ4 00`, `R READ20 00`.

**The fix.** One test, `FileConnector.RecordLengthConflicts`, replaces the sequential READ's inline expression and is asked by the
relative READs and, through `IndexedConnector.IndexedReadStatus`, the indexed READs. After: random and sequential READs of records of
3/20/7 (relative) and 4/20/7 (indexed) characters under RECORD VARYING 5 TO 10 answer `04/04/00`; the in-range controls stay `00`.
Determinations in docs/CONFORMANCE.md: the DEPENDING ON item holds the length as delivered (never above the maximum); with a length
conflict and a duplicate key both true the status is `04`.

**Goldens and rows.** `pb5480_relative_read_record_length_conflict` (1985, VIII.4.5), `pb5480_indexed_read_record_length_conflict`
(1985, IX.4.5) and the WiseOwl determination case `pb5480_depending_on_after_truncated_read_85`. Rows 1985-GR-VIII.4.5.4-16 and
1985-GR-IX.4.5.4-18 PARTIAL to CONFORMS (1985 GAP 229 to 227).

PB5480 review round 1 (R1-1): the READ's cap of `LastReadLength` at the RECORD VARYING maximum (the delivered length the DEPENDING ON item reports) was also
the length `SequentialConnector.RewriteRecordSequential` compared under §2023:14.9.35.4 GR16, so a REWRITE after a truncated READ answered `00` and half-replaced
the 20-byte record. The REWRITE now compares `CurrentRecord.Length`, the physical record's; golden `pb5480_rewrite_after_truncated_read_85` (READ 04, REWRITE 44,
record unchanged). R1-2 removed the unused `using WiseOwl.Cobol.Editions` from `ArithmeticEmitter.cs`.
