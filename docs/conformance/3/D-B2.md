- **D-B2 — the figurative constant SPACE against a BOOLEAN receiving operand is the boolean character '0'**
  (kb/Work PB425). The standard does not define the value, and §2023:14.9.25.4 GR2 makes the case reachable:
  `MOVE "" TO PIC 1(4)` is legal source (Table 16's Alphanumeric sending row gives "Yes" against a Boolean
  receiving operand, and §2023:14.9.25.3 SR7's prohibition names figurative constants *written in the source*, which
  a zero-length literal is not), and GR2 then treats the literal "as if it were the figurative constant SPACE".
  Table 17 gives SPACE against a Boolean receiving operand the category **boolean**, but §2023:8.3.3.6.4 GR5 defines
  the space format only as "one or more of the character space in the computer's runtime coded character set"
  and names no boolean value, while §2023:8.3.3.4.3 SR2 admits only '0' and '1' in a boolean item. WiseOwl COBOL fills
  with the boolean character '0' — the only boolean character the standard attaches to a fill (§2023:14.6.8.6 aligns
  a boolean receiver "with zero fill or truncation to the right"; §2023:8.3.3.6.4 GR4 gives the ZERO format "one or
  more of the boolean character '0'"), and the same answer GR3 gives a boolean zero-length literal, so the two
  GR arms agree. Written once, in `FigurativeConstants.FillChar` — the compiler's one fill-character
  computation; witnessed by `conformance:cases/8.3.3.2-Alphanumeric-literals/pb425_zero_length_literal_move` (`A-BOOL=[0000]`).
  *(Unrelated to the superseded D-B1 boolean-representation determination in `DATA_MODEL_DESIGN.md`.)*
