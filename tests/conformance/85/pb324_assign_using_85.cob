      *> ISO 1989:2023 §14.9.27.4 GR26 -> §12.4.5.3 GR3 b) at the OLDEST supported edition. The ASSIGN clause's
      *> USING phrase is accepted and honoured at --std 85 exactly as at 2023: the connector is associated with
      *> the physical file named by data-name-1's content at the time of execution of each OPEN, so one connector
      *> writes two files and reads them back. No edition gate exists for the phrase, so 85 shall not reject it
      *> and shall not silently fall back to the file-name (kb/Work PB324).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB324D8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN8 ASSIGN USING WS-NAME
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL CHK8 ASSIGN TO "dyn8.txt"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-CK.
       DATA DIVISION.
       FILE SECTION.
       FD  DYN8.
       01  DYN-REC PIC X(5).
       FD  CHK8.
       01  CHK-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(20) VALUE "pb324c.dat".
       01  WS-ST   PIC XX.
       01  WS-CK   PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT DYN8.
           DISPLAY "OPENC=" WS-ST.
           MOVE "CCCCC" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYN8.
           MOVE "pb324d.dat" TO WS-NAME.
           OPEN OUTPUT DYN8.
           DISPLAY "OPEND=" WS-ST.
           MOVE "DDDDD" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYN8.
           OPEN INPUT CHK8.
           DISPLAY "CHK=" WS-CK.
           CLOSE CHK8.
           MOVE "pb324c.dat" TO WS-NAME.
           OPEN INPUT DYN8.
           READ DYN8.
           DISPLAY "READC=" DYN-REC.
           CLOSE DYN8.
           MOVE "pb324d.dat" TO WS-NAME.
           OPEN INPUT DYN8.
           READ DYN8.
           DISPLAY "READD=" DYN-REC.
           CLOSE DYN8.
           STOP RUN.
