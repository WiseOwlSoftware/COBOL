      *>--- conformance-case
      *> id: g2ins43-sr3-cd-numeric-literal-refused
      *> rules: [2002-SR-14.8.21.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.2 SR3 - "Each literal shall be an alphanumeric,
      *> boolean, or national literal". CX is an alphanumeric record
      *> item under a 2002 output CD and literal-1 at the INSPECT line
      *> is the numeric literal 5, so it is refused at 2002
      *> (COBOL1757).
      *> Why it can fail: the compiler accepts the literal for a CD
      *> item or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C2 PIC 99 VALUE 0.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 CX PIC X(5) VALUE "A  BC".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CX TALLYING C2 FOR ALL 5.
           DISPLAY "C2=" C2.
           STOP RUN.
       END PROGRAM G2INS43.
