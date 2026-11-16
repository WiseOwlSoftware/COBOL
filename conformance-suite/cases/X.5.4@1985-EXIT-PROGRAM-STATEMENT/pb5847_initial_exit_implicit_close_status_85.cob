      *>--- conformance-case
      *> id: pb5847_initial_exit_implicit_close_status_85
      *> rules: [1985-GR-X.5.4.4-3, 1985-GR-X.5.3.4-10, 1985-GR-VII.4.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5847. The EXIT PROGRAM of a called program with the
      *> initial attribute "is equivalent also to executing a CANCEL
      *> statement referencing that program" (1985 X.5.4.4 GR3), whose
      *> implicit CLOSE (X.5.3.4 GR10) updates the I-O status (VII 4.2.4
      *> GR4). GST, a GLOBAL item of this container, holds 10 inside the
      *> initial program after its read to the end (S 10) and 00 once it
      *> has returned (A 00; it read 10 before the fix). Its next
      *> activation's OPEN OUTPUT gives 00, not 41 (O 00 twice): the file
      *> was closed. A file the initial program closed itself gets no
      *> implicit CLOSE, so the 42 of its second CLOSE stands (B 42).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GST PIC XX GLOBAL.
       01  OP  PIC X GLOBAL.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "R" TO OP.
           CALL "W5847Y".
           DISPLAY "A " GST.
           CALL "W5847Y".
           DISPLAY "A " GST.
           MOVE "N" TO OP.
           CALL "W5847Y".
           DISPLAY "B " GST.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847Y IS INITIAL PROGRAM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT YF ASSIGN TO "W5847Y.DAT"
               FILE STATUS IS GST.
       DATA DIVISION.
       FILE SECTION.
       FD  YF.
       01  YR PIC X(4).
       PROCEDURE DIVISION.
       P-INIT.
           OPEN OUTPUT YF.
           IF OP = "N"
               CLOSE YF
               CLOSE YF
               GO TO P-EXIT.
           DISPLAY "O " GST.
           MOVE "YYYY" TO YR. WRITE YR. CLOSE YF.
           OPEN INPUT YF.
           READ YF AT END DISPLAY "UNEXPECTED EOF".
           READ YF AT END DISPLAY "S " GST.
       P-EXIT.
           EXIT PROGRAM.
       END PROGRAM W5847Y.
       END PROGRAM W5847X.
