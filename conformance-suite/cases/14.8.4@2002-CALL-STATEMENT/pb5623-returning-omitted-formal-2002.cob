*>--- conformance-case
*> id: pb5623-returning-omitted-formal-2002
*> rules: [2002-GR-14.8.4.3-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5623 - §2002:14.8.4.3 GR12: "If a parameter for which the omitted-argument condition is true is
      *> referenced in a called program, except as an argument or in the omitted-argument condition, the
      *> EC-PROGRAM-ARG-OMITTED exception condition is set to exist." The RETURNING identifier-3 of a CALL is not an
      *> argument and not the omitted-argument condition, so a CALL ... RETURNING that names a formal parameter
      *> (whole, or a subordinate of a group formal) whose argument was omitted references it, and §2002:14.8.4.3 GR3
      *> a) identifies it "at the beginning of the execution of the CALL statement": the condition exists there,
      *> before the activated program runs. RESUME AT NEXT STATEMENT abandons the CALL, so the activated programs
      *> (Q5623G, Q5623H, and Q5623R for the level-66 RENAMES alias of the group formal, kb/Work PB5623 review R1-1) never display "RAN". The same formal as an ARGUMENT (Q5623F) is exempt and raises nothing:
      *> Q5623F sees its own formal omitted (§2002:8.8.4.1.7.3 GR1c).
      *> Why each leg can fail: with the old emission the whole-formal and the subordinate RETURNING legs each ran
      *> the activated program and N stayed 00; the second CALL of Q5623C supplies every argument, so no reference
      *> raises (N keeps the 03 of the first activation: a program that is not INITIAL keeps its storage) and the
      *> results arrive (RX=0007, G2=12 from the alias).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623C IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       01 RX PIC 9(4).
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       PROCEDURE DIVISION USING OPTIONAL X OPTIONAL RX OPTIONAL G.
       END PROGRAM Q5623C.
      >>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q5623C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MX PIC X(4) VALUE "ABCD".
       01 MRX PIC 9(4) VALUE 0.
       01 MG.
          05 MG1 PIC X(2) VALUE "ZZ".
          05 MG2 PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "OMITTED:"
           CALL "Q5623C" AS Q5623C USING OMITTED OMITTED OMITTED
           DISPLAY "SUPPLIED:"
           CALL "Q5623C" AS Q5623C USING MX MRX MG
           DISPLAY "RX=" MRX " G2=" MG2
           STOP RUN.
       END PROGRAM Q5623M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(2) VALUE 0.
       LINKAGE SECTION.
       01 X PIC X(4).
       01 RX PIC 9(4).
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       66 GR66 RENAMES G1 THRU G2.
       PROCEDURE DIVISION USING OPTIONAL X OPTIONAL RX OPTIONAL G.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-ARG-OMITTED.
       H-P.
           ADD 1 TO N
           DISPLAY "  CAUGHT " N "=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           CALL "Q5623F" USING X
           CALL "Q5623G" RETURNING RX
           CALL "Q5623H" RETURNING G2
           CALL "Q5623R" RETURNING GR66
           DISPLAY "  N=" N
           GOBACK.
       END PROGRAM Q5623C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FX PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL FX.
           IF FX IS OMITTED
               DISPLAY "  F-X-OMITTED"
           ELSE
               DISPLAY "  F-X=" FX
           END-IF
           GOBACK.
       END PROGRAM Q5623F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 GR PIC 9(4).
       PROCEDURE DIVISION RETURNING GR.
           MOVE 7 TO GR
           DISPLAY "  G-RAN"
           GOBACK.
       END PROGRAM Q5623G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623H.
       DATA DIVISION.
       LINKAGE SECTION.
       01 HR PIC 9(2).
       PROCEDURE DIVISION RETURNING HR.
           MOVE 5 TO HR
           DISPLAY "  H-RAN"
           GOBACK.
       END PROGRAM Q5623H.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623R.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RR PIC X(4).
       PROCEDURE DIVISION RETURNING RR.
           MOVE "AB12" TO RR
           DISPLAY "  R-RAN"
           GOBACK.
       END PROGRAM Q5623R.
