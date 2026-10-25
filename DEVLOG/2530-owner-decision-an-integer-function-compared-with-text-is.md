## Entry 2530 — 2026-10-25 00:39 PDT — Owner decision: an integer function compared with text is refused per §8.8.4.2.5 (PB3691)

§8.8.4.2.5 compares a number with text by its digits, so it admits only an integer literal or an integer data item
of usage display or national. WiseOwl already refuses a binary item there (COBOLNET2532) but compiled
`IF FUNCTION INTEGER(N) = "0007"` and answered arbitrarily, as GnuCOBOL does. The owner asked why a function
differs from an integer field, heard the digit-count reason, and decided: "Refuse it per the spec". PB3691 is open
with the bar: one shared admission predicate with D-FNUSE, the sibling sweep, goldens, and the CONFORMANCE.md
paragraph in the implementing change.

