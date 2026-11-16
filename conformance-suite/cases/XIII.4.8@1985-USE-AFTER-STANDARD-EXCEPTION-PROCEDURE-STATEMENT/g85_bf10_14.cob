      *>--- conformance-case
      *> id: g85_bf10_14
      *> rules: [1985-GR-XIII.4.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR1 - "Declarative procedures may be included in any
      *> COBOL source program irrespective of whether the program contains or is
      *> contained within another program. A declarative is invoked when any of
      *> the conditions described in the USE statement which prefaces the
      *> declarative occurs while the program is being executed. Only a
      *> declarative within the separately compiled program that contains the
      *> statement which caused the qualifying condition is invoked ..."
      *> G85BF1014 (with a declarative) contains G85BF1014I (with its own
      *> declarative); G85BF1014S is a second separately compiled program in the
      *> same source, with its own declarative. Each opens its own sequential
      *> file, reads the one record it wrote, then causes the at end condition (I-O status 10) with no
      *> AT END phrase (legal because an applicable USE exists).
      *>   MAIN-DECL / INNER-DECL / SEP-DECL 10 : each program's own
      *>     declarative runs for its own statement, in the containing program,
      *>     the contained one and the separately compiled one.
      *>   L=D=M001DATA : the main program is a Report Writer program (RD R1);
      *>     the detail it GENERATEd after the successful READ is read back.
      *>   ... RESUMED : control returns to the next executable statement
      *>     after the input-output statement (§1985:XIII.4.8.4 GR6: 10 is not a
      *>     critical status, which only 3x and 4x are).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1014.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "bf1014m.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS MF-ST.
           SELECT RPT-FILE ASSIGN TO "bf1014.rpt".
           SELECT CHK-FILE ASSIGN TO "bf1014.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  MF
           LABEL RECORDS ARE STANDARD.
       01  MF-REC                 PIC X(8).
       FD  RPT-FILE
           LABEL RECORDS ARE OMITTED
           REPORT IS R1.
       FD  CHK-FILE
           LABEL RECORDS ARE OMITTED
           RECORD CONTAINS 1 CHARACTERS.
       01  CHK-CH                  PIC X.
       WORKING-STORAGE SECTION.
       01  MF-ST                  PIC XX.
       01  WS-EOF                  PIC X VALUE "N".
       01  WS-LINE                 PIC X(20) VALUE SPACES.
       01  WS-LEN                  PIC 99 VALUE 0.
       REPORT SECTION.
       RD  R1.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1            PIC X(2) VALUE "D=".
           05  COLUMN 3            PIC X(8) SOURCE MF-REC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       M-D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON MF.
       M-D-P.
           DISPLAY "MAIN-DECL " MF-ST.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-1.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           OPEN OUTPUT MF.
           MOVE "M001DATA" TO MF-REC.
           WRITE MF-REC.
           CLOSE MF.
           OPEN INPUT MF.
           READ MF.
           DISPLAY "MAIN READ " MF-REC.
           GENERATE DL.
           READ MF.
           DISPLAY "MAIN RESUMED".
           CLOSE MF.
           TERMINATE R1.
           CLOSE RPT-FILE.
           OPEN INPUT CHK-FILE.
           PERFORM M-2 UNTIL WS-EOF = "Y".
           PERFORM M-3.
           CLOSE CHK-FILE.
           CALL "G85BF1014I".
           CALL "G85BF1014S".
           DISPLAY "MAIN END".
           STOP RUN.
       M-2.
           READ CHK-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               IF CHK-CH < SPACE
                   PERFORM M-3
               ELSE
                   ADD 1 TO WS-LEN
                   MOVE CHK-CH TO WS-LINE (WS-LEN:1).
       M-3.
           IF WS-LEN > 0
               DISPLAY "L=" WS-LINE (1:10)
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-LEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1014I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "bf1014i.dat"
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
       END PROGRAM G85BF1014I.
       END PROGRAM G85BF1014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1014S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "bf1014s.dat"
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
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SF.
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
       END PROGRAM G85BF1014S.
