      *>--- conformance-case
      *> id: g85_bf23_04
      *> rules: [1985-SR-IX.4.4.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.3 SR2 - "The files referenced in the OPEN statement need not
      *> all have the same organization or access." (Indexed I-O module)
      * A PERMISSIVE rule: the compiler must accept, and run, ONE OPEN
      * statement naming files of three organizations and three access
      * modes: FQ SEQUENTIAL/ACCESS SEQUENTIAL, FR RELATIVE/ACCESS
      * DYNAMIC, FX INDEXED/ACCESS SEQUENTIAL. It is written once with
      * OUTPUT and once with INPUT.
      * DERIVATION (each citation run through scripts/spec/cite.py):
      *  - The rule makes the mixed list legal, so the program compiles
      *    at 85 (a refusal is the failure of this leg).
      *  - §1985:IX.4.4.4 GR17: the result "is the same as if a separate OPEN
      *    statement had been written for each file-name", so each file
      *    gets its own I-O status; OUTPUT creates each file, INPUT
      *    opens it, and every OPEN and CLOSE succeeds: 00 on every
      *    status line (§1985:IX.1.3.4 "The input-output statement is
      *    successfully executed and no further information is
      *    available").
      *  - The record read back from each file is the one written to
      *    that file, so each file connector was associated with its
      *    own file: QREC=QQQQQQ, RREC=RRRRRR, XREC=X01XXX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2304.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FQ ASSIGN TO "g85bf2304q.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT FR ASSIGN TO "g85bf2304r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT FX ASSIGN TO "g85bf2304x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD  FQ.
       01  Q-REC PIC X(6).
       FD  FR.
       01  R-REC PIC X(6).
       FD  FX.
       01  X-REC.
           05 X-KEY PIC X(3).
           05 X-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01  RK   PIC 9(4).
       01  ST-Q PIC XX.
       01  ST-R PIC XX.
       01  ST-X PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FQ FR FX.
           DISPLAY "OUTQ=" ST-Q.
           DISPLAY "OUTR=" ST-R.
           DISPLAY "OUTX=" ST-X.
           MOVE "QQQQQQ" TO Q-REC.
           WRITE Q-REC.
           MOVE 1 TO RK.
           MOVE "RRRRRR" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "RWINV".
           MOVE "X01" TO X-KEY.
           MOVE "XXX" TO X-VAL.
           WRITE X-REC INVALID KEY DISPLAY "XWINV".
           CLOSE FQ FR FX.
           DISPLAY "CLSQ=" ST-Q.
           DISPLAY "CLSR=" ST-R.
           DISPLAY "CLSX=" ST-X.
           MOVE SPACES TO Q-REC R-REC X-REC.
           OPEN INPUT FQ FR FX.
           DISPLAY "INQ=" ST-Q.
           DISPLAY "INR=" ST-R.
           DISPLAY "INX=" ST-X.
           READ FQ AT END DISPLAY "QEOF".
           DISPLAY "QREC=" Q-REC.
           MOVE 1 TO RK.
           READ FR INVALID KEY DISPLAY "RINV".
           DISPLAY "RREC=" R-REC.
           READ FX AT END DISPLAY "XEOF".
           DISPLAY "XREC=" X-REC.
           CLOSE FQ FR FX.
           STOP RUN.
