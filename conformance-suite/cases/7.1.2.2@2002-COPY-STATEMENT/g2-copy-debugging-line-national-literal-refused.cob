      *>--- conformance-case
      *> id: g2-copy-debugging-line-national-literal-refused
      *> rules: [2002-SR-7.1.2.2-5]
      *> files: [G2CPL.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR5 - "Literal-1 and literal-2 shall be
      *> alphanumeric literals". The COPY on the D line names its
      *> library text by a national literal. Without WITH DEBUGGING MODE
      *> the D line becomes a comment only after COPY processing
      *> (§2002:6.2.7), so the COPY is still processed and the program
      *> is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN".
      D    COPY N"G2CPL".
           STOP RUN.
