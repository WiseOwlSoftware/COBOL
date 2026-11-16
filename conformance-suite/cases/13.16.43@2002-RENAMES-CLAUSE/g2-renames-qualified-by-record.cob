      *>--- conformance-case
      *> id: g2-renames-qualified-by-record
      *> rules: [2002-SR-13.16.43.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR3 - "Data-name-1 may be qualified only by the
      *> names of the associated level 01, FD, CD, or SD entries." Four
      *> level-66 entries share the name RX, one after a record of each
      *> kind: FREC under FD F1, SREC under SD S1, WREC in working storage
      *> and CREC under CD CDIN. Each RX is referenced once by its 01 name
      *> and, where there is one, once by its FD, SD or CD name; RX alone
      *> would be ambiguous, so every reference resolves only if that
      *> qualifier is admitted. Expected lines, from §2002:13.16.43.3 GR2
      *> ("data-name-1 defines an alphanumeric group item that includes all
      *> elementary items starting with data-name-2 ... and concluding with
      *> data-name-3") and GR1 (RENAMES without THRU takes data-name-2's
      *> attributes and storage):
      *>   CD=AAABBB and C1=AAABBB - RX OF CDIN and RX OF CREC are CA THRU
      *>     CB of CREC, which holds AAABBBCCC in its first 9 positions;
      *>   WS=WWW - RX OF WREC is WA, which holds WWW;
      *>   FD=05 and F1=05 - RX OF F1 and RX OF FREC are FA (2) THRU FB (3);
      *>   SD=07 and S1=07 - RX OF S1 and RX OF SREC are SA (4) THRU SB (3).
      *> The file and sort records are measured with FUNCTION LENGTH
      *> (§2002:15.39 returns the length of the argument), so no file is
      *> opened. A leg fails if a qualifier is refused, or if a reference
      *> resolves to another record's RX (the lengths 3, 5, 6 and 7 differ).
      *> The prohibitions of SR3 are refused in g2-renames-dn1-qualifier,
      *> g2-renames-cd-occurs-operand and g2-renames-occurs-ancestor.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN20.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2REN20.DAT".
           SELECT S1 ASSIGN TO "G2REN20.SRT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  FREC.
           05 FA PIC X(2).
           05 FB PIC X(3).
       66  RX RENAMES FA THRU FB.
       SD  S1.
       01  SREC.
           05 SA PIC X(4).
           05 SB PIC X(3).
       66  RX RENAMES SA THRU SB.
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       01  WREC.
           05 WA PIC X(3).
       66  RX RENAMES WA.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       01  CREC.
           05 CA PIC X(3).
           05 CB PIC X(3).
           05 CC PIC X(3).
           05 CZ PIC X(78).
       66  RX RENAMES CA THRU CB.
       PROCEDURE DIVISION.
           MOVE "AAA" TO CA
           MOVE "BBB" TO CB
           MOVE "CCC" TO CC
           MOVE "WWW" TO WA
           DISPLAY "CD=" RX OF CDIN
           DISPLAY "C1=" RX OF CREC
           DISPLAY "WS=" RX OF WREC
           MOVE FUNCTION LENGTH(RX OF F1) TO N
           DISPLAY "FD=" N
           MOVE FUNCTION LENGTH(RX OF FREC) TO N
           DISPLAY "F1=" N
           MOVE FUNCTION LENGTH(RX OF S1) TO N
           DISPLAY "SD=" N
           MOVE FUNCTION LENGTH(RX OF SREC) TO N
           DISPLAY "S1=" N
           STOP RUN.
