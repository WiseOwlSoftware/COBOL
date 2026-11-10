      *>--- conformance-case
      *> id: pb5083-debug-use-procedure
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. X3.23-1985 USE FOR DEBUGGING general rule 21: "If
      *> procedure-name-1 is a USE procedure that is to be executed":
      *> DEBUG-LINE identifies the statement that causes execution of the
      *> USE procedure, DEBUG-NAME contains procedure-name-1 and
      *> DEBUG-CONTENTS contains 'USE PROCEDURE'. ERR-PROC is not a
      *> debugging section, so it may be a subject (the restriction of
      *> USE FOR DEBUGGING syntax rule 5 is on procedure-names defined
      *> within debugging sections). The empty file makes each READ fail
      *> at end with no AT END phrase, which runs the USE procedure.
      *> Expected output, in order: MAIN; the trigger and the USE
      *> procedure's IN ERR-PROC for the first READ; AFTER READ 1; the same
      *> pair for the second READ; DONE. DEBUG-NAME is the section ERR-PROC
      *> (the procedure-name the USE FOR DEBUGGING statement names), never
      *> ERR-PARA. Why it can fail: a section subject read as its paragraphs
      *> names ERR-PARA, and a USE procedure entered as an ordinary
      *> procedure reports START PROGRAM and then FALL THROUGH (the pre-fix
      *> output).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083USEP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       OBJECT-COMPUTER. X.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "pb5083-use.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  TF
           LABEL RECORDS ARE STANDARD.
       01  TF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-NAME PIC X(30).
       01  W-CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-PROC SECTION.
           USE FOR DEBUGGING ON ERR-PROC.
       DBG-PARA.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "DEBUG [" W-NAME "] [" W-CONT "]".
       ERR-PROC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON TF.
       ERR-PARA.
           DISPLAY "IN ERR-PROC".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN".
           OPEN OUTPUT TF.
           CLOSE TF.
           OPEN INPUT TF.
           READ TF.
           DISPLAY "AFTER READ 1".
           READ TF.
           DISPLAY "DONE".
           CLOSE TF.
           STOP RUN.
