      *>--- conformance-case
      *> id: pb5847_cancel_implicit_close_status_85
      *> rules: [1985-GR-X.5.3.4-10, 1985-GR-X.5.3.4-4, 1985-GR-VII.4.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5847. During a CANCEL "an implicit CLOSE statement
      *> without any optional phrases is executed for each file in the
      *> open mode" (1985 X.5.3.4 GR10), and a CLOSE "causes the value of
      *> the I-O status associated with file-name-1 to be updated" (VII
      *> 4.2.4 GR4), so a FILE STATUS item that outlives the cancelled
      *> program (GST, a GLOBAL item of this container) reads 00 after the
      *> CANCEL where the subprogram's last READ left 10 (B 00; it read
      *> 10 before the fix). The reCALL's OPEN gives 00, not 41: the file
      *> was closed (the second C 00). A file NOT in the open mode gets
      *> no implicit CLOSE, so the 42 of a second explicit CLOSE stands
      *> (E 42). The
      *> contained program of a cancelled program is cancelled too (GR4),
      *> its file closed with its status stored (G 00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GST PIC XX GLOBAL.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "W5847S".
           DISPLAY "A " GST.
           CANCEL "W5847S".
           DISPLAY "B " GST.
           CALL "W5847S".
           CANCEL "W5847S".
           CALL "W5847N".
           DISPLAY "D " GST.
           CANCEL "W5847N".
           DISPLAY "E " GST.
           CALL "W5847C".
           DISPLAY "F " GST.
           CANCEL "W5847C".
           DISPLAY "G " GST.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "W5847A.DAT"
               FILE STATUS IS GST.
       DATA DIVISION.
       FILE SECTION.
       FD  WF.
       01  WR PIC X(4).
       PROCEDURE DIVISION.
       P-SUB.
           OPEN OUTPUT WF.
           DISPLAY "C " GST.
           MOVE "AAAA" TO WR. WRITE WR. CLOSE WF.
           OPEN INPUT WF.
           READ WF AT END DISPLAY "UNEXPECTED EOF".
           READ WF AT END CONTINUE.
           EXIT PROGRAM.
       END PROGRAM W5847S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "W5847B.DAT"
               FILE STATUS IS GST.
       DATA DIVISION.
       FILE SECTION.
       FD  NF.
       01  NR PIC X(4).
       PROCEDURE DIVISION.
       P-NOPEN.
           OPEN OUTPUT NF. CLOSE NF. CLOSE NF.
           EXIT PROGRAM.
       END PROGRAM W5847N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847C.
       PROCEDURE DIVISION.
       P-CONT.
           CALL "W5847I".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W5847I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "W5847C.DAT"
               FILE STATUS IS GST.
       DATA DIVISION.
       FILE SECTION.
       FD  IF1.
       01  IR PIC X(4).
       PROCEDURE DIVISION.
       P-INNER.
           OPEN OUTPUT IF1. MOVE "CCCC" TO IR. WRITE IR. CLOSE IF1.
           OPEN INPUT IF1.
           READ IF1 AT END DISPLAY "UNEXPECTED EOF".
           READ IF1 AT END CONTINUE.
           EXIT PROGRAM.
       END PROGRAM W5847I.
       END PROGRAM W5847C.
       END PROGRAM W5847M.
