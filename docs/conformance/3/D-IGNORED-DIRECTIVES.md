- **D-IGNORED-DIRECTIVES — Recognized-and-ignored compiler directives (§2023:7.3 — the implementor-disposition set)**: the following standard
  directives are RECOGNIZED (consumed during text manipulation so the program compiles unchanged) and carry no
  effect, each because WiseOwl COBOL provides a single behaviour with no alternative to select: **>>CALL-CONVENTION**
  (§2023:7.3.9) — CALL uses the single .NET managed calling convention, so only `>>CALL-CONVENTION COBOL` is accepted and
  any other call-convention-name is an error, COBOL1911 (A.1 item 68 in §7; kb/Work PB1383). GnuCOBOL 3.1.2 and 3.2.0 additionally accept EXTERN, STDCALL and STATIC (measured on both, kb/Work PB1945); owner decision 2026-10-03: keep COBOL only, and revisit only if a .NET mapping is added, which may not be required at all; **>>LEAP-SECOND** (§2023:7.3.17) — the REPORTED side only: the .NET clock never reports a 60th second (A.1 item
  111 in §7); the directive's ARGUMENT side (a 60 in a seconds subfield, a time form to 86,400.99 under ON) is honoured —
  `LeapSecondDirectiveProcessor`, kb/Work PB65; **>>LISTING** / **>>PAGE** (§2023:7.3.18 / §2023:7.3.19) — no source listing
  is produced, so listing on/off and page ejects are inert. The **>>DISPLAY** directive (§2023:7.3.12) is NOT in this set:
  §2023:7.3.12.4 GR2 leaves the no-listing result to the implementor, and GnuCOBOL — the precedent CLAUDE.md rule 1 names for
  implementor latitude — writes the operands to its standard output at compile time, so WiseOwl COBOL parses and evaluates
  the operands and writes them at compile time (kb/Work PB807, PB1538; A.1 items 53–55 in §7 state the determination). The **>>FLAG-02 / >>FLAG-14**
  flagging directives (§2023:7.3.14 / §2023:7.3.15) are NOT in this set: they emit the warnings they request (COBOL1620 /
  COBOL1621 — A.1 items 79 and 80 in §7). The **>>FLAG-85 / >>FLAG-NATIVE-ARITHMETIC** directives (§2002:7.2.11 / §2002:7.2.12, COBOL-2002 and COBOL-2014 only)
  are NOT in this set either, and are no longer parsed-and-ignored (kb/Work PB5600): `>>FLAG-85 { ALL | option… } { ON | OFF }`
  and `>>FLAG-NATIVE-ARITHMETIC { ON | OFF }` are syntax-checked (COBOL1622 for a word the format does not print) and their
  state is folded per option per line, ON from the line after the directive. The warning mechanism that §2002:7.2.11.3 GR1
  and §2002:7.2.12.3 GR1 require, and that 2002 A.1 item 60 (FLAG-85) requires the implementor to document, is a compile-time
  warning that does not change the compiled program: COBOL7390 for each construct a FLAG-85 option covers (CORRESPONDING: an
  ADD, MOVE or SUBTRACT CORRESPONDING with a subscript that is not a numeric literal; DE-EDITING: a MOVE of a numeric-edited
  item to a numeric or numeric-edited item; DIVIDE: DIVIDE … REMAINDER with an unsigned quotient and a signed divisor or
  dividend; FUNCTION-ARGUMENT: FUNCTION RANDOM with a parenthesized argument inside a function whose catalog row takes
  several numeric arguments; MOVE: an alphanumeric literal or item of more than 31 characters to a numeric item; NUMVAL:
  FUNCTION NUMVAL / NUMVAL-C; SET: a condition-setting SET whose condition-name belongs to a group holding an OCCURS DEPENDING
  table; STANDARD-1 / STANDARD-2: the ALPHABET clause naming it; ZERO-LENGTH: a READ of a file whose smallest record can be
  empty, or a statement referencing an item whose minimum length is zero, once per statement), and COBOL7391 for an arithmetic
  statement, a SUM clause or an arithmetic expression with an operator whose operand is described with usage binary-char,
  -short, -long, -double, float-short, -long or -extended (COMP-1, COMP-2) while ARITHMETIC IS STANDARD is in effect for the
  source element (§2002:8.8.1.3). **Every one of these is edition-GATED**, which is the half that was missing until
  kb/Work PB725: the roster is the `directiveWords` column of `tests/version-matrix/constructs/`, inverted by
  `CompilerDirectiveCatalog` and checked once, where `ConditionalCompilationProcessor` recognizes a `>>` word
  (`>>SOURCE FORMAT` one stage earlier, in `ReferenceFormatProcessor`, which consumes its line first). Recognizing
  a directive the targeted edition does not have is not a leniency, it is an under-rejection — the `>>` facility
  itself is a COBOL-2002 introduction (VCR Table 7 row 7.21) and `>>FLAG-85` / `>>FLAG-NATIVE-ARITHMETIC` were
  REMOVED in 2023 (Annex E.2 item 21), so all three edges are enforced: COBOL0900 below the introducing
  edition, COBOL0903 at an obsoleting one, COBOL0902 at a removing one.
