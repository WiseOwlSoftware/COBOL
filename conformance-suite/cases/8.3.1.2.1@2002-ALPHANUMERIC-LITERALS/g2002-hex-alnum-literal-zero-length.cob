      *>--- conformance-case
      *> id: g2002-hex-alnum-literal-zero-length
      *> rules: [2002-FMT-8.3.1.2.1.1, 2002-SR-8.3.1.2.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.1.1 writes the content of every alphanumeric
      *> literal
      *> in braces, one or more occurrences, and §2002:8.3.1.2.1.2 SR1
      *> requires its length to "be greater than zero": X"" on the
      *> DISPLAY
      *> is not a COBOL-2002 literal and the program is refused (the
      *> zero-length literal is a COBOL-2014 addition).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALN04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
           DISPLAY X""
           STOP RUN.
