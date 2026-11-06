      *>--- conformance-case
      *> id: pb4810-debug-explicit-references
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING general rule 5: the
      *> section runs for "any other COBOL statement that explicitly
      *> references identifier-1", and rules 5 and 6 close: "If identifier-1
      *> is specified in a phrase that is not executed or evaluated, the
      *> associated debugging section is not executed."
      *> Operands the statement MEANS and the program did not write are no
      *> reference: the members MOVE CORRESPONDING pairs (G3 and G2 are what
      *> the statement names), the conditional variable of a condition-name,
      *> the RELATIVE KEY item a START without a KEY phrase compares, the
      *> record area READ INTO and RETURN INTO send. The INTO identifier is
      *> written, and its phrase runs only when a record was delivered. A
      *> SORT key and the item a MOVE writes are explicit references.
      *> Why each leg can fail: a walk of the lowered statement fires
      *> DBX for the pairs, DBC for the 88, DBR for START; a walk that skips
      *> the SORT key phrase never runs DBK; an INTO phrase planned as part
      *> of the whole statement runs DBV after a READ or RETURN that hit
      *> AT END and moved nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810XR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4810xr.dat" ORGANIZATION RELATIVE
              ACCESS DYNAMIC RELATIVE KEY RK FILE STATUS ST.
           SELECT S ASSIGN TO "pb4810xr.srt".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR       PIC X(4).
       SD  S.
       01  SR.
           02  SK   PIC 99.
           02  SD1  PIC XX.
       WORKING-STORAGE SECTION.
       01  RK       PIC 99.
       01  ST       PIC XX.
       01  V        PIC X(4) VALUE SPACES.
       01  W        PIC X(4) VALUE "0102".
       01  C8       PIC 9 VALUE 1.
           88  YES8 VALUE 1.
       01  G2.
           02  X    PIC 9 VALUE 0.
           02  Y2   PIC 9 VALUE 0.
       01  G3.
           02  X    PIC 9 VALUE 7.
           02  Z3   PIC 9 VALUE 8.
       01  W-NAME   PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBK SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF SK.
       DBK-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBK [" W-NAME "]".
       DBX SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF X OF G2.
       DBX-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBX [" W-NAME "]".
       DBC SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF C8.
       DBC-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBC [" W-NAME "]".
       DBR SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF RK.
       DBR-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBR [" W-NAME "]".
       DBV SECTION.
           USE FOR DEBUGGING ON V.
       DBV-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBV [" W-NAME "]".
       DBG SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF G2.
       DBG-B.
           MOVE DEBUG-NAME TO W-NAME.
           DISPLAY "DBG [" W-NAME "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "1 MOVE CORRESPONDING".
           MOVE CORRESPONDING G3 TO G2.
           DISPLAY "2 CONDITION-NAME".
           IF YES8 DISPLAY "YES".
           OPEN OUTPUT F.
           DISPLAY "3 MOVE TO RK".
           MOVE 1 TO RK.
           MOVE "AAAA" TO FR.
           WRITE FR.
           CLOSE F.
           OPEN I-O F.
           DISPLAY "4 START".
           START F INVALID KEY DISPLAY "IK".
           DISPLAY "5 READ INTO".
           READ F NEXT INTO V AT END DISPLAY "AT END".
           DISPLAY "6 READ AT END".
           READ F NEXT INTO V AT END DISPLAY "AT END".
           CLOSE F.
           DISPLAY "7 SORT".
           SORT S ON ASCENDING KEY SK
              INPUT PROCEDURE IP OUTPUT PROCEDURE OP.
           DISPLAY "8 DONE".
           STOP RUN.
       IP SECTION.
       IP1.
           MOVE W TO SR.
           RELEASE SR.
       OP SECTION.
       OP1.
           RETURN S INTO V AT END DISPLAY "AT END".
           RETURN S INTO V AT END DISPLAY "AT END".
