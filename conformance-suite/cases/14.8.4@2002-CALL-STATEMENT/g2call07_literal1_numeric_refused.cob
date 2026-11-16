      *>--- conformance-case
      *> id: g2call07_literal1_numeric_refused
      *> rules: [2002-SR-14.8.4.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR2 (ALL FORMATS) - "Literal-1 shall be an
      *> alphanumeric or national literal." 123 is a numeric literal, so
      *> this Format 1 (on-overflow) CALL shall be refused at the CALL
      *> line. The accepted categories are g2call06_literal1_categories.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LTN.
       PROCEDURE DIVISION.
       MAIN.
           CALL 123 ON OVERFLOW DISPLAY "OVF" END-CALL
           STOP RUN.
       END PROGRAM G2LTN.
