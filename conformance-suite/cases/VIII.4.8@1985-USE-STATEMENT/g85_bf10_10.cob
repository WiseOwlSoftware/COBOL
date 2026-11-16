      *>--- conformance-case
      *> id: g85_bf10_10
      *> rules: [1985-GR-VIII.4.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.4 GR1 - "Declarative procedures may be included in any
      *> COBOL source program irrespective of whether the program contains or is
      *> contained within another program. A declarative is invoked when any of
      *> the conditions described in the USE statement which prefaces the
      *> declarative occurs while the program is being executed. Only a
      *> declarative within the separately compiled program that contains the
      *> statement which caused the qualifying condition is invoked ..."
      *> G85BF1010 (with a declarative) contains G85BF1010I (with its own
      *> declarative); G85BF1010S is a second separately compiled program in the
      *> same source, with its own declarative. Each opens its own relative
      *> file, reads the one record it wrote, then causes the invalid key condition (I-O status 23, §1985:VIII.1.3.4:
      *> READ of relative record 2, which does not exist) with no
      *> INVALID KEY phrase (legal because an applicable USE exists).
      *>   MAIN-DECL / INNER-DECL / SEP-DECL 23 : each program's own
      *>     declarative runs for its own statement, in the containing program,
      *>     the contained one and the separately compiled one.
      *>   ... RESUMED : control returns to the next executable statement
      *>     after the input-output statement (§1985:VIII.4.8.4 GR6: 23 is not a
      *>     critical status, which only 3x and 4x are).
      *> The CALLer's USE ... ON INPUT would qualify for G85BF1010S's input file
      *> too, were declaratives not confined to their own separately compiled
      *> program; it must not run for that file (SEP-DECL alone, once).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1010.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "bf1010m.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS MF-RK
               FILE STATUS IS MF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  MF
           LABEL RECORDS ARE STANDARD.
       01  MF-REC                 PIC X(8).
       WORKING-STORAGE SECTION.
       01  MF-ST                  PIC XX.
       01  MF-RK                  PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       M-D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       M-D-P.
           DISPLAY "MAIN-DECL " MF-ST.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-1.
           OPEN OUTPUT MF.
           MOVE 1 TO MF-RK.
           MOVE "M001DATA" TO MF-REC.
           WRITE MF-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE MF.
           OPEN INPUT MF.
           READ MF.
           DISPLAY "MAIN READ " MF-REC.
           MOVE 2 TO MF-RK.
           READ MF.
           DISPLAY "MAIN RESUMED".
           CLOSE MF.
           CALL "G85BF1010I".
           CALL "G85BF1010S".
           DISPLAY "MAIN END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1010I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "bf1010i.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS IF1-RK
               FILE STATUS IS IF1-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IF1
           LABEL RECORDS ARE STANDARD.
       01  IF1-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  IF1-ST                 PIC XX.
       01  IF1-RK                 PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       I-D SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IF1.
       I-D-P.
           DISPLAY "INNER-DECL " IF1-ST.
       END DECLARATIVES.
       I-MAIN SECTION.
       I-1.
           OPEN OUTPUT IF1.
           MOVE 1 TO IF1-RK.
           MOVE "I001DATA" TO IF1-REC.
           WRITE IF1-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE IF1.
           OPEN INPUT IF1.
           READ IF1.
           DISPLAY "INNER READ " IF1-REC.
           MOVE 2 TO IF1-RK.
           READ IF1.
           DISPLAY "INNER RESUMED".
           CLOSE IF1.
           EXIT PROGRAM.
       END PROGRAM G85BF1010I.
       END PROGRAM G85BF1010.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1010S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "bf1010s.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS SF-RK
               FILE STATUS IS SF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           LABEL RECORDS ARE STANDARD.
       01  SF-REC                 PIC X(8).
       WORKING-STORAGE SECTION.
       01  SF-ST                  PIC XX.
       01  SF-RK                  PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       S-D SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
       S-D-P.
           DISPLAY "SEP-DECL " SF-ST.
       END DECLARATIVES.
       S-MAIN SECTION.
       S-1.
           OPEN OUTPUT SF.
           MOVE 1 TO SF-RK.
           MOVE "S001DATA" TO SF-REC.
           WRITE SF-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE SF.
           OPEN INPUT SF.
           READ SF.
           DISPLAY "SEP READ " SF-REC.
           MOVE 2 TO SF-RK.
           READ SF.
           DISPLAY "SEP RESUMED".
           CLOSE SF.
           EXIT PROGRAM.
       END PROGRAM G85BF1010S.
