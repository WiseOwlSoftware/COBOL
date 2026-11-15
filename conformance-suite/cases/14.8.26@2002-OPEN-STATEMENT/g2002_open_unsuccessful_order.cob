      *>--- conformance-case
      *> id: g2002_open_unsuccessful_order
      *> rules: [2002-GR-14.8.26.3-25]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.3 GR25 - "If the execution of the OPEN statement is
      *> unsuccessful, the physical file is not affected and the following
      *> actions take place in the following order: a) A value is placed
      *> in the I-O status ... b) The level-3 EC-I-O exception condition
      *> associated with the I-O status value is set to exist. c) Any
      *> applicable USE FOR EXCEPTION or USE AFTER EXCEPTION procedure is
      *> executed".
      *> Checking for EC-I-O is turned on, so b) raises the condition
      *> (§2002:14.5.12.1: "When checking for EC-I-O exception conditions
      *> is enabled, the associated exception conditions are raised based
      *> on the resulting I-O status value"). Step c) is a format 3
      *> USE AFTER EXCEPTION CONDITION EC-I-O FILE f (§2002:14.8.45.2
      *> SR13), selected by §2002:14.8.45.3 GR3d (level-2 name, FILE).
      *> Inside it the status already holds a)'s value and EXCEPTION-
      *> STATUS already names b)'s condition, so a) and b) both precede
      *> c). The format 3 USE names an exception-name of a higher level
      *> in the condition's hierarchy, so §2002:14.5.12.1.2 item 2
      *> applies (a format 1 USE names none, and item 4 would end the
      *> run). The declarative ends in RESUME AT NEXT STATEMENT, so it
      *> does not complete normally (§2002:14.5.12.1.1) and the run
      *> continues.
      *> EXCEPTION-STATUS is 31 characters, space-filled (§2002:15.28.2
      *> RV1); the DISPLAY appends "|".
      *>   Leg 1, F is not optional and not present:
      *>   USE FS=35 EXC=EC-I-O-PERMANENT-ERROR  §2002:9.1.12.5 item 4;
      *>                 the '3' class is EC-I-O-PERMANENT-ERROR (9.1.12).
      *>   AFTER1=35 USE=1
      *>   USE FS=35 ... and AGAIN=35 USE=2: the failed OPEN INPUT did not
      *>                 create the file ("the physical file is not
      *>                 affected").
      *>   Leg 2, G is open OUTPUT and holds AAAA:
      *>   USE GS=41 EXC=EC-I-O-LOGIC-ERROR      §2002:14.8.26.3 GR1;
      *>                 the '4' class is EC-I-O-LOGIC-ERROR.
      *>   AFTER2=41 USE=3
      *>   W2=00         G is still open OUTPUT: the second OPEN OUTPUT
      *>                 neither closed it nor emptied it (an OPEN OUTPUT
      *>                 that took effect would leave "no records", GR16).
      *>   G1=AAAA G2=BBBB G3=10: both records survive.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2opn07-absent.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "g2opn07g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       01 WS-USE PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-F SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O FILE F.
       IO-F-P.
           ADD 1 TO WS-USE
           DISPLAY "USE FS=" FS " EXC=" FUNCTION EXCEPTION-STATUS "|"
           RESUME AT NEXT STATEMENT.
       IO-G SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O FILE G.
       IO-G-P.
           ADD 1 TO WS-USE
           DISPLAY "USE GS=" GS " EXC=" FUNCTION EXCEPTION-STATUS "|"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       LEG-1.
           OPEN INPUT F
           DISPLAY "AFTER1=" FS " USE=" WS-USE
           OPEN INPUT F
           DISPLAY "AGAIN=" FS " USE=" WS-USE.
       LEG-2.
           OPEN OUTPUT G
           DISPLAY "OPEN-G=" GS
           MOVE "AAAA" TO G-REC
           WRITE G-REC
           OPEN OUTPUT G
           DISPLAY "AFTER2=" GS " USE=" WS-USE
           MOVE "BBBB" TO G-REC
           WRITE G-REC
           DISPLAY "W2=" GS
           CLOSE G
           OPEN INPUT G
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G1=" G-REC
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G2=" G-REC
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G3=" GS
           CLOSE G
           STOP RUN.
