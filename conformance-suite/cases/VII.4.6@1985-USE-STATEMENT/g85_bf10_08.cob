      *>--- conformance-case
      *> id: g85_bf10_08
      *> rules: [1985-GR-VII.4.6.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.4 GR1 - "Declarative procedures may be included in any
      *> COBOL source program irrespective of whether the program contains or is
      *> contained within another program. A declarative is invoked when any of
      *> the conditions described in the USE statement which prefaces the
      *> declarative occurs while the program is being executed. Only a
      *> declarative within the separately compiled program that contains the
      *> statement which caused the qualifying condition is invoked ..."
      *> G85BF1008 (with a declarative) contains G85BF1008I (with its own
      *> declarative); G85BF1008S is a second separately compiled program in the
      *> same source, with its own declarative. Each opens its own sequential
      *> file, reads the one record it wrote, then causes the at end condition (I-O status 10, §1985:VII.1.3.5) with no
      *> AT END phrase (legal because an applicable USE exists).
      *>   MAIN-DECL / INNER-DECL / SEP-DECL 10 : each program's own
      *>     declarative runs for its own statement, in the containing program,
      *>     the contained one and the separately compiled one.
      *>   ... RESUMED : control returns to the next executable statement
      *>     after the input-output statement (§1985:VII.4.6.4 GR6: 10 is not a
      *>     critical status, which only 3x and 4x are).
      *> The CALLer's USE ... ON INPUT would qualify for G85BF1008S's input file
      *> too, were declaratives not confined to their own separately compiled
      *> program; it must not run for that file (SEP-DECL alone, once).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1008.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "bf1008m.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS MF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  MF
           LABEL RECORDS ARE STANDARD.
       01  MF-REC                 PIC X(8).
       WORKING-STORAGE SECTION.
       01  MF-ST                  PIC XX.
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
           MOVE "M001DATA" TO MF-REC.
           WRITE MF-REC.
           CLOSE MF.
           OPEN INPUT MF.
           READ MF.
           DISPLAY "MAIN READ " MF-REC.
           READ MF.
           DISPLAY "MAIN RESUMED".
           CLOSE MF.
           CALL "G85BF1008I".
           CALL "G85BF1008S".
           DISPLAY "MAIN END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1008I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "bf1008i.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS IF1-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IF1
           LABEL RECORDS ARE STANDARD.
       01  IF1-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  IF1-ST                 PIC XX.
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
           MOVE "I001DATA" TO IF1-REC.
           WRITE IF1-REC.
           CLOSE IF1.
           OPEN INPUT IF1.
           READ IF1.
           DISPLAY "INNER READ " IF1-REC.
           READ IF1.
           DISPLAY "INNER RESUMED".
           CLOSE IF1.
           EXIT PROGRAM.
       END PROGRAM G85BF1008I.
       END PROGRAM G85BF1008.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1008S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "bf1008s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           LABEL RECORDS ARE STANDARD.
       01  SF-REC                 PIC X(8).
       WORKING-STORAGE SECTION.
       01  SF-ST                  PIC XX.
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
           MOVE "S001DATA" TO SF-REC.
           WRITE SF-REC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF.
           DISPLAY "SEP READ " SF-REC.
           READ SF.
           DISPLAY "SEP RESUMED".
           CLOSE SF.
           EXIT PROGRAM.
       END PROGRAM G85BF1008S.
