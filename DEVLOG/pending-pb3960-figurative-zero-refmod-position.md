## PB3960: a figurative ZERO in a reference-modification position meets the compile-time range screen

**Re-probe.** On the wave's build `DISPLAY T(ZERO:2)` and `DISPLAY T(2:ZERO)` over `PIC X(4)` compiled clean (they abort at run
time with EC-BOUND-REF-MOD), while `T(0:2)` and `T(2:0)` were refused COBOL2670. The spelling variants ZEROS and ZEROES
behaved like ZERO; `ALL ZERO` (accepted at 85, COBOL0902 from 2002) did too. Reproduced as the note says.

**Root cause.** The ZERO beside the ref-mod colon is rewritten to the `ZERO_ARITH` token (`ZeroTokenRewriter`), which the
grammar parses as a `primaryExpression`. `ReferenceResolver.BindPosition`'s direct walk had arms for integer literals,
data-names and parentheses and none for that primary, so the whole position routed to the D18 materializer and became a
read of a computed temporary. `LiteralPosition` (the screen's "is this a compile-time integer") then returned null and
`LiteralRefModRangeViolation` never judged it. The fault is in the position reader, not in the screen: every other
literal-only reader of a `Position` (`Position.ConstantValue`, `Int32Literal`, `StorageExtent`, the bit-start reader of
`ParameterConformance`) was blind to the same figurative.

**Fix.** The walk reads a `ZERO_ARITH` primary as `PositionConstant(0)` (ISO 8.8.1.1 admits the figurative ZERO in an
arithmetic expression, 8.3.3.6.4 GR4 gives the zero format the numeric value 0 in a numeric context). `ALL` before it is
8.3.3.6.2 Format 1's optional word, and its 8.3.3.6.3 SR1 a) edition edge is asked of the one gate, now worded in one
place (`FigurativeAssociation.GateAllZeroAsArithmeticOperand`, which `ExpressionBinder.ZeroOperand` calls too). The walk
queues it as a third kind of `PendingScreen`, because the walk can be abandoned for D18 after reading a ZERO and D18
would gate the same operand again (measured on `T(2:ALL ZERO + 1)`: one COBOL0902, not two). `T(ALL ZERO:2)` at 85 is
COBOL2670; at 2002 and later it carries COBOL0902 for the ALL and COBOL2670 for the range, two different rules.

**Sibling sweep.** Other readers of a rendered position: `ScreenRefModLiterals` (the fix's consumer),
`DataBinder.Reports` CONTROL operands (read lexically by `ReadOperandSuffixes`; the branch review (R1-1) found the first
version of this note wrong here: `ReadLiteralPositions` read the rewritten token's text "0" as the literal 0, so
`CONTROL IS T(1:ZERO)` compiled clean and `CONTROL IS T(ZERO:2)` was refused by the range screen, quoting text the program
never wrote, instead of §13.18.16.3 SR4's COBOL1699. `ReadLiteralPositions` now refuses a position whose tokens include
`ZERO_ARITH` as non-literal, for every clause that asks it: CONTROL, SUM/RESET, TYPE CONTROL, the CODE clause; goldens
`13.18.16-CONTROL/pb3960-control-refmod-zero-length` and `-zero-start`), the subscript path (a constant subscript is not screened at compile time by design, DOC-A.1-69;
the ZERO there is now a direct constant, still range-checked at run time). Folded constants such as `T(1 + 0:2)` or
`T(-0:2)` are not literals to the screen and are still left to run time; filed as a lead.

**Goldens.** `8.4.3.3-Reference-modification/pb3960_figurative_zero_refmod_position` (accept 85..2023: `T(ZERO + 2:2)` BC,
`T(2:ZERO + 2)` BC, `T(ZERO + N:ZERO + 1)` B, `T(4 - ZERO:1)` D, `T(1:4 * ZERO + 3)` ABC) and the refusals
`pb3960-refmod-zero-start`, `pb3960-refmod-zero-length` (all editions) and `pb3960-refmod-all-zero-start` (85). Inventory:
GR-8.4.3.3.4-5 gained two code and four test witnesses (batch `pb3960-refmod-figurative-zero`, GAP 31 to 31). DOC-A.1-69
now says "integer literals or the figurative ZERO".

**Self-review.** Architecture: one gate wording, one position reader; no C# text in a bound node. Full code: the walk
reads the cited rule, not a paraphrase; `Position.cs`'s node-set doc no longer lists the figurative ZERO as a D18 shape.
Performance: no new allocation on the hot path (a record struct field and a constant). Duplication: the
"as an arithmetic operand" diagnostic text moved to one place.

**Docs.** VCR Table 7 row 7.28 and DESIGN-frontend-grammar.md now name the resolver's position walk as the third asker of the
one ALL ZERO gate (review lead R1-2).
