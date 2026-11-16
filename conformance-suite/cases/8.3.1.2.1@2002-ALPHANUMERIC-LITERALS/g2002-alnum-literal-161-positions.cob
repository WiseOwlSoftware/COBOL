      *>--- conformance-case
      *> id: g2002-alnum-literal-161-positions
      *> rules: [2002-SR-8.3.1.2.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.1.2 SR1 - the length "shall be greater than zero
      *> and less than or equal to 160 alphanumeric character
      *> positions".
      *> The continued literal below holds 161 positions (80 A, 81 B),
      *> one
      *> past the 2002 bound, and is refused. Its 160-position twin is
      *> g2002_alnum_literal_forms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALN05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 W PIC X(161).
       PROCEDURE DIVISION.
           MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"
               TO W
           STOP RUN.
