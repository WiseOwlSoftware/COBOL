## Entry 2791 — 2026-10-30 11:34 PDT — PB4013 adjudicated: VALUE must admit a COMP-5 item's whole container range; the fix is filed as PB4113

kb/Work PB4013 asked whether `PIC 9(4) COMP-5 VALUE 65535` and `PIC S9(4) COMP-5 VALUE -32768` may compile. The compiler
refuses them with COBOL1625. A cobol-adjudicator ruled that they may, which makes the refusal a rejects-legal-source
defect, and a cobol-refuter upheld the ruling. COMP-5 is not in ISO/IEC 1989:2023, so it is a §4.2.10 1) extension.
§13.18.63.3 SR2 bounds a VALUE literal by the range of "the PICTURE clause or the USAGE clause". For COMP-5 the
compiler's own determination, DOC-A.1-208, sets the range to the binary container, and so do GnuCOBOL 3.2.0 and IBM's
documented VALUE rule. MOVE, arithmetic, SET of an 88 and comparisons already use the container. Only VALUE disagrees,
because `ValidateNumericValue` runs its PICTURE digit test first, and the COMP-5 band check after it can never be
reached.

The refuter corrected one step of the reasoning: ISO does not tie standard COMPUTATIONAL's range to its PICTURE either.
It also widened the golden scope to cover WHEN SET TO FALSE, a format-2 table, the 4- and 8-byte containers and
P-scaled pictures. The PICTURE's scale still bounds the trailing digits, so `S99V99 COMP-5 VALUE 1.234` stays refused.
PB4113 carries the fix, the DOC-A.1-208 sentence for VALUE and the goldens, and claims SR-13.18.63.3-2. PB4013 is
closed as adjudicated.

