      *>--- conformance-case
      *> id: g85_bf10_12
      *> rules: [1985-GR-IX.4.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.4 GR1 - "Declarative procedures may be included in any
      *> COBOL source program irrespective of whether the program contains or is
      *> contained within another program. A declarative is invoked when any of
      *> the conditions described in the USE statement which prefaces the
      *> declarative occurs while the program is being executed. Only a
      *> declarative within the separately compiled program that contains the
      *> statement which caused the qualifying condition is invoked ..."
      *> G85BF1012 (with a declarative) contains G85BF1012I (with its own
      *> declarative); G85BF1012S is a second separately compiled program in the
      *> same source, with its own declarative. Each opens its own indexed
      *> file, reads the one record it wrote (MOVE K001 to the RECORD KEY
      *> first: after WRITE the record area is undefined, §1985:IX.4.9.4 GR2), then causes the invalid key condition (I-O status 23, §1985:IX.1.3.4:
      *> READ of key K002, which does not exist) with no
      *> INVALID KEY phrase (legal because an applicable USE exists).
      *>   MAIN-DECL / INNER-DECL / SEP-DECL 23 : each program's own
      *>     declarative runs for its own statement, in the containing program,
      *>     the contained one and the separately compiled one.
      *>   ... RESUMED : control returns to the next executable statement
      *>     after the input-output statement (§1985:IX.4.8.4 GR6: 23 is not a
      *>     critical status, which only 3x and 4x are).
      *> The CALLer's USE ... ON INPUT would qualify for G85BF1012S's input file
      *> too, were declaratives not confined to their own separately compiled
      *> program; it must not run for that file (SEP-DECL alone, once).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1012.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "bf1012m.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS MF-K
               FILE STATUS IS MF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  MF
           LABEL RECORDS ARE STANDARD.
       01  MF-REC.
           05  MF-K             PIC X(4).
           05  MF-V             PIC X(4).
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
           MOVE "K001" TO MF-K.
           MOVE "M001DATA" TO MF-V.
           WRITE MF-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE MF.
           OPEN INPUT MF.
           MOVE "K001" TO MF-K.
           READ MF.
           DISPLAY "MAIN READ " MF-V.
           MOVE "K002" TO MF-K.
           READ MF.
           DISPLAY "MAIN RESUMED".
           CLOSE MF.
           CALL "G85BF1012I".
           CALL "G85BF1012S".
           DISPLAY "MAIN END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1012I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "bf1012i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IF1-K
               FILE STATUS IS IF1-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IF1
           LABEL RECORDS ARE STANDARD.
       01  IF1-REC.
           05  IF1-K            PIC X(4).
           05  IF1-V            PIC X(4).
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
           MOVE "K001" TO IF1-K.
           MOVE "I001DATA" TO IF1-V.
           WRITE IF1-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE IF1.
           OPEN INPUT IF1.
           MOVE "K001" TO IF1-K.
           READ IF1.
           DISPLAY "INNER READ " IF1-V.
           MOVE "K002" TO IF1-K.
           READ IF1.
           DISPLAY "INNER RESUMED".
           CLOSE IF1.
           EXIT PROGRAM.
       END PROGRAM G85BF1012I.
       END PROGRAM G85BF1012.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1012S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "bf1012s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS SF-K
               FILE STATUS IS SF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           LABEL RECORDS ARE STANDARD.
       01  SF-REC.
           05  SF-K             PIC X(4).
           05  SF-V             PIC X(4).
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
           MOVE "K001" TO SF-K.
           MOVE "S001DATA" TO SF-V.
           WRITE SF-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE SF.
           OPEN INPUT SF.
           MOVE "K001" TO SF-K.
           READ SF.
           DISPLAY "SEP READ " SF-V.
           MOVE "K002" TO SF-K.
           READ SF.
           DISPLAY "SEP RESUMED".
           CLOSE SF.
           EXIT PROGRAM.
       END PROGRAM G85BF1012S.
