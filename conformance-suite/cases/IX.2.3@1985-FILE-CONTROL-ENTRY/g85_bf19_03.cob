      *>--- conformance-case
      *> id: g85_bf19_03
      *> rules: [1985-GR-IX.2.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR4 - OPTIONAL applies to a file opened INPUT, I-O
      *>   or EXTEND and is required for a file not necessarily present.
      *>   cite.py: OK §1985:IX.2.3.4 GR4 "The OPTIONAL phrase applies
      *>   only to files opened in the input, I-O, or extend mode".
      *> Derivation (each line of the .out):
      *>   OPEN INPUT absent optional: 05, §1985:IX.1.3.4 GRL2.1 "An OPEN
      *>   statement is successfully executed but the referenced optional
      *>   file is not present"; first READ: 10, §1985:IX.1.3.4 GRL2.2
      *>   "attempted for the first time on an optional input file that
      *>   is not present" (OPEN Table 1, §1985:IX.4.4.4 GR1).
      *>   OPEN I-O / EXTEND absent optional: 05 and the file is created
      *>   (§1985:IX.4.4.4 GR15
      *>   "phrase creates the file"),
      *>   so the reopen INPUT is 00; the I-O file holds no record (10),
      *>   the EXTEND file holds EX01.
      *>   OPEN OUTPUT: OPTIONAL does not apply to output mode (GR4);
      *>   Table 1 OUTPUT/unavailable "Open causes the file to be
      *>   created": 00.
      *>   OPEN INPUT of the absent non-optional REQ-IN: 35, §1985:IX.1.3.4
      *>   GRL2.4 "is attempted on a non-optional file that is not present".
      *> Why each leg can fail: an implementation that ignores OPTIONAL
      *>   gives 35 on the first three OPENs; one that applies it to
      *>   OUTPUT gives 05 there; one that does not create the file on
      *>   I-O or EXTEND gives 35 on the reopen; one that treats a
      *>   non-OPTIONAL absent file as optional gives 05 for REQ-IN.
      *> REQ-IN's 35 is a critical status, so what follows its USE
      *>   procedure is the implementor's: nothing is DISPLAYed after it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1903.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL OPT-IN
               ASSIGN TO "BF1903I.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OPT-IN-KEY
               FILE STATUS IS ST-IN.
           SELECT OPTIONAL OPT-IO
               ASSIGN TO "BF1903U.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OPT-IO-KEY
               FILE STATUS IS ST-IO.
           SELECT OPTIONAL OPT-EX
               ASSIGN TO "BF1903E.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OPT-EX-KEY
               FILE STATUS IS ST-EX.
           SELECT OPTIONAL OPT-OU
               ASSIGN TO "BF1903O.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OPT-OU-KEY
               FILE STATUS IS ST-OU.
           SELECT REQ-IN
               ASSIGN TO "BF1903R.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS REQ-IN-KEY
               FILE STATUS IS ST-RQ.
       DATA DIVISION.
       FILE SECTION.
       FD  OPT-IN.
       01  OPT-IN-REC.
           05  OPT-IN-KEY PIC X(4).
       FD  OPT-IO.
       01  OPT-IO-REC.
           05  OPT-IO-KEY PIC X(4).
       FD  OPT-EX.
       01  OPT-EX-REC.
           05  OPT-EX-KEY PIC X(4).
       FD  OPT-OU.
       01  OPT-OU-REC.
           05  OPT-OU-KEY PIC X(4).
       FD  REQ-IN.
       01  REQ-IN-REC.
           05  REQ-IN-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-IN PIC XX VALUE "ZZ".
       01  ST-IO PIC XX VALUE "ZZ".
       01  ST-EX PIC XX VALUE "ZZ".
       01  ST-OU PIC XX VALUE "ZZ".
       01  ST-RQ PIC XX VALUE "ZZ".
       PROCEDURE DIVISION.
       DECLARATIVES.
       REQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REQ-IN.
       REQ-ERR-PARA.
           DISPLAY "REQ OPEN INPUT " ST-RQ.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
      * INPUT on an absent OPTIONAL file: 05, first READ 10.
           OPEN INPUT OPT-IN.
           DISPLAY "OPT OPEN INPUT " ST-IN.
           READ OPT-IN AT END
               DISPLAY "OPT READ AT END " ST-IN.
           CLOSE OPT-IN.
      * I-O on an absent OPTIONAL file: 05 and the file is created.
           OPEN I-O OPT-IO.
           DISPLAY "OPT OPEN I-O " ST-IO.
           CLOSE OPT-IO.
           OPEN INPUT OPT-IO.
           DISPLAY "OPT I-O REOPEN " ST-IO.
           READ OPT-IO AT END
               DISPLAY "OPT I-O FILE EMPTY " ST-IO.
           CLOSE OPT-IO.
      * EXTEND on an absent OPTIONAL file: 05, created, written.
           OPEN EXTEND OPT-EX.
           DISPLAY "OPT OPEN EXTEND " ST-EX.
           MOVE "EX01" TO OPT-EX-KEY.
           WRITE OPT-EX-REC
               INVALID KEY DISPLAY "BAD WRITE".
           CLOSE OPT-EX.
           MOVE SPACES TO OPT-EX-KEY.
           OPEN INPUT OPT-EX.
           DISPLAY "OPT EXTEND REOPEN " ST-EX.
           READ OPT-EX AT END
               DISPLAY "OPT EXTEND NO RECORD".
           DISPLAY "OPT EXTEND RECORD " OPT-EX-KEY.
           CLOSE OPT-EX.
      * OUTPUT: the OPTIONAL phrase does not apply; normal create.
           OPEN OUTPUT OPT-OU.
           DISPLAY "OPT OPEN OUTPUT " ST-OU.
           CLOSE OPT-OU.
      * A file not declared OPTIONAL and not present: 35.
           OPEN INPUT REQ-IN.
           STOP RUN.
