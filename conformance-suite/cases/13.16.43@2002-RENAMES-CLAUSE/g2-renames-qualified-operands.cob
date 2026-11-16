      *>--- conformance-case
      *> id: g2-renames-qualified-operands
      *> rules: [2002-SR-13.16.43.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR6 - "Data-name-2 and data-name-3 may be
      *> qualified." A and B are each declared twice in R1 (under G1 and
      *> G2), so every operand below resolves only through its qualifier:
      *> OF and IN, one and two levels. Expected lines, from §2002:13.16.43.3
      *> GR2 (THRU: an alphanumeric group from data-name-2 through
      *> data-name-3) and GR1 (no THRU: data-name-2's storage and
      *> attributes), with R1 laid out A1 B1 A2 B2:
      *>   X1=A2B2 - A OF G2 THRU B OF G2;
      *>   X2=B1A2 - B OF G1 THRU A IN G2, positions 3 to 6;
      *>   X3=A1   - A OF G1 OF R1.
      *> A leg fails if a qualified operand is refused, or resolves to the
      *> other A or B (each line then reads a different pair).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN24.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05 G1.
              10 A PIC X(2) VALUE "A1".
              10 B PIC X(2) VALUE "B1".
           05 G2.
              10 A PIC X(2) VALUE "A2".
              10 B PIC X(2) VALUE "B2".
       66  X1 RENAMES A OF G2 THRU B OF G2.
       66  X2 RENAMES B OF G1 THRU A IN G2.
       66  X3 RENAMES A OF G1 OF R1.
       PROCEDURE DIVISION.
           DISPLAY "X1=" X1
           DISPLAY "X2=" X2
           DISPLAY "X3=" X3
           STOP RUN.
