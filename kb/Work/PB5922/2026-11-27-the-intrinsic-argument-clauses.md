## 2026-11-27 - the intrinsic function argument clauses cite their own edition (wave 20262174 group B)

**What the re-probe showed.** On the merged base COBOL1627 was the largest family left in the ratchet (145 lines at 2002, 134 at 2014, measured with
`WISEOWL_COBOL_RECORD_CITATION_SWEEP`): an argument-class refusal of FUNCTION `f` at `--std 2002` printed `§2023:15.n.3 ARk` and
`§2023:15.3`. The 2002 and 2014 standards print the same rules under the function's own `15.m.2` clause (`§2002:15.68.2 AR1` for SQRT) and the
argument types under `§2002:15.2` / `§2014:15.3`.

**The fix.** `IntrinsicCitations.Clause` (src/WiseOwl.Cobol.Compiler/Binding/IntrinsicCitations.cs) reads every `§2023:15.n.2` / `§2023:15.n.3`
citation in a clause text (`AR1`, `AR1/AR3`, `rule 2`, `r4a`, a chain through two functions) and renders it in the edition in force: the function
is named by `IntrinsicCatalog.FunctionAt(2023, n)`, the edition's clause number by `IntrinsicCatalog.Paragraph(year, name)` (the catalog's functions in
ordinal name order from 15.6 at 2002 and 15.7 at 2014 and 2023: derived, never listed), the subclause is one earlier (`.3` is `.2`, `.2` is `.1`),
and `IntrinsicCitations.Edition2002` / `Edition2014` hold the 42 rules an edition numbers differently or lacks (NUMVAL, NUMVAL-C, NUMVAL-F, the
ALGEBRAIC pair, ORD-MAX/MIN, STANDARD-COMPARE, TEST-NUMVAL-C, TRIM, EXCEPTION-FILE, the zero-length-literal rules), found by matching each 2023
rule's text against the edition's rules and read. A rule the edition lacks cites the whole arguments subclause; a function the edition lacks
keeps the 2023 clause with the gate's phrase "(targeting COBOL-2002)"; a text citing no such clause throws at 2002 and 2014, so a new form fails at
once. The argument types (`ArgumentTypes`, `IntegerType`), the floating-point item (`FloatingPointItem`: §2002:13.16.58.3 GR12 and §2014:13.18.60.3
GR12) and the integer-operand bar (`NumericFunctionAsInteger`: §2002:8.4.2.2.2 SR9, §2014:8.4.2.2.2 SR11) carry their own editions' text.
Sibling sites of the same function-rule mechanism moved onto it: the zero-length-literal and NUMVAL-C currency-string messages, DISPLAY-OF /
NATIONAL-OF's argument rules, the locale-name rule of LOCALE-COMPARE/DATE/TIME/TIME-FROM-SECONDS, the arity (COBOL1504) and ALL-subscript
(COBOL1645) messages.

**Measured.** COBOL1627 and COBOL1645 left the ratchet at both editions (508 codes at 2002, 517 at 2014). `IntrinsicEditionClausesDriftTests` reads
the function headings of the 2002, 2014 and 2023 transcriptions against `Paragraph`/`FunctionAt` and holds every renumbering to a rule the edition
prints (rule counts read from the transcriptions).

**What is left (not closed here).** COBOL1560 and COBOL1707 (the screen-module licence text: its 2002 ground needs PB5581's wording), COBOL1688's
run-time reasons, the EC-REPORT-INACTIVE run-time text, and the intrinsic-function messages that spell a 2023 rule as a literal rather than through
`IntrinsicCitations` (COBOL1546 other arms, COBOL1516, COBOL1501's summary clause, the CONVERT/CONCAT/BASECONVERT/FORMATTED-* rules in
IntrinsicBinder.cs, and the run-time texts of CobolIntrinsics.*): each is a call to `IntrinsicCitations.ClauseSince2002`, PB6118's sweep. The
inventory rows stay in `inventory_rows`: their closing waits for the 2002 direction to land whole.
