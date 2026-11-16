      *>--- conformance-case
      *> id: g2ins44-sr3-cd-all-figurative-refused
      *> rules: [2002-SR-14.8.21.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.2 SR3 - "Literal-1, literal-2, literal-3,
      *> literal-4 shall not be a figurative constant that begins with
      *> the word ALL". CX is an alphanumeric record item under a 2002
      *> output CD and literal-1 at the INSPECT line is ALL SPACE, so
      *> it is refused at 2002 (COBOL1757).
      *> Why it can fail: the compiler accepts the literal for a CD
      *> item or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS44.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C2 PIC 99 VALUE 0.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 CX PIC X(5) VALUE "A  BC".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CX TALLYING C2 FOR ALL ALL SPACE.
           DISPLAY "C2=" C2.
           STOP RUN.
       END PROGRAM G2INS44.
