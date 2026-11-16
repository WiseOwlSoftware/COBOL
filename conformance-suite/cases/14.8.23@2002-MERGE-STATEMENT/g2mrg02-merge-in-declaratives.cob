      *>--- conformance-case
      *> id: g2mrg02-merge-in-declaratives
      *> rules: [2002-SR-14.8.23.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.2 SR1 - "A MERGE statement may appear anywhere in
      *> the procedure division except in the declaratives portion."
      *> The MERGE in the USE procedure D1 is in the declaratives
      *> portion and is refused at its line. The accepted placements are
      *> g2mrg01-merge-anywhere-but-declaratives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRG02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2mrg02w.tmp".
           SELECT F1 ASSIGN TO "g2mrg021.dat".
           SELECT F2 ASSIGN TO "g2mrg022.dat".
           SELECT F3 ASSIGN TO "g2mrg023.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F3.
       D1-P.
           MERGE SW ON ASCENDING KEY SK USING F1 F2 GIVING F3.
       END DECLARATIVES.
       MAIN SECTION.
       M0.
           DISPLAY "X".
           STOP RUN.
       END PROGRAM G2MRG02.
