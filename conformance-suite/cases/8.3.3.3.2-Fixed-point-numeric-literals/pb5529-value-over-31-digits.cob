      *>--- conformance-case
      *> id: pb5529-value-over-31-digits
      *> rules: [GR-8.3.3.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5529. 8.3.3.3.2: "The implementor shall allow for
      *> fixed-point numeric literals of 1 through 31 digits in length";
      *> this compiler's limit is 31, so the 32-digit VALUE literal below
      *> is refused at every edition (COBOL0801), as a 32-digit literal
      *> in a MOVE already was.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5529B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B PIC 9(31) VALUE 12345678901234567890123456789012.
       PROCEDURE DIVISION.
           DISPLAY B
           STOP RUN.
