      *>--- conformance-case
      *> id: g2mrg01-merge-anywhere-but-declaratives
      *> rules: [2002-SR-14.8.23.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.2 SR1 - "A MERGE statement may appear anywhere in
      *> the procedure division except in the declaratives portion."
      *> 2002 forbids only the declaratives portion, so a MERGE written
      *> inside a MERGE output procedure (MOUT) and inside a SORT input
      *> procedure (SIN) is accepted. §2002:14.8.23.3 GR8 forbids only
      *> EXECUTING a MERGE from an output procedure's range, so those two
      *> MERGEs sit behind a condition that is never true. A refusal of
      *> either (the 2023 rule's ban) fails the case.
      *> The MERGEs that execute, in the main paragraph and in a
      *> PERFORMed section, merge F1 (A001 C003) and F2 (B002 D004) in
      *> ascending key order (§2002:14.8.23.3 GR3a):
      *>   M=A001..D004 from the output procedure MOUT,
      *>   S=Y008 S=Z009 from the SORT whose input procedure is SIN,
      *>   G=A001..D004 from the MERGE in section PM, GIVING F4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRG01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2mrg01w.tmp".
           SELECT SW2 ASSIGN TO "g2mrg01v.tmp".
           SELECT F1 ASSIGN TO "g2mrg011.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "g2mrg012.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "g2mrg013.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F4 ASSIGN TO "g2mrg014.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       SD SW2.
       01 SR2.
          05 SK2 PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       FD F4.
       01 R4 PIC X(4).
       WORKING-STORAGE SECTION.
       01 NEVER PIC X VALUE "N".
       01 EOF-M PIC X VALUE "N".
       01 EOF-F PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN SECTION.
       M0.
           OPEN OUTPUT F1.
           MOVE "A001" TO R1.
           WRITE R1.
           MOVE "C003" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "B002" TO R2.
           WRITE R2.
           MOVE "D004" TO R2.
           WRITE R2.
           CLOSE F2.
           MERGE SW ON ASCENDING KEY SK USING F1 F2
               OUTPUT PROCEDURE IS MOUT.
           SORT SW2 ON ASCENDING KEY SK2
               INPUT PROCEDURE IS SIN GIVING F3.
           OPEN INPUT F3.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ F3 AT END MOVE "Y" TO EOF-F
                   NOT AT END DISPLAY "S=" R3
               END-READ
           END-PERFORM.
           CLOSE F3.
           PERFORM PM.
           OPEN INPUT F4.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ F4 AT END MOVE "Y" TO EOF-F
                   NOT AT END DISPLAY "G=" R4
               END-READ
           END-PERFORM.
           CLOSE F4.
           STOP RUN.
       MOUT SECTION.
       MO1.
           PERFORM UNTIL EOF-M = "Y"
               RETURN SW AT END MOVE "Y" TO EOF-M
                   NOT AT END DISPLAY "M=" SK
               END-RETURN
           END-PERFORM.
           IF NEVER = "Y"
               MERGE SW2 ON ASCENDING KEY SK2 USING F1 F2 GIVING F4
           END-IF.
       SIN SECTION.
       SI1.
           MOVE "Z009" TO SR2.
           RELEASE SR2.
           MOVE "Y008" TO SR2.
           RELEASE SR2.
           IF NEVER = "Y"
               MERGE SW ON ASCENDING KEY SK USING F1 F2 GIVING F4
           END-IF.
       PM SECTION.
       PM1.
           MERGE SW ON ASCENDING KEY SK USING F1 F2 GIVING F4.
       END PROGRAM G2MRG01.
