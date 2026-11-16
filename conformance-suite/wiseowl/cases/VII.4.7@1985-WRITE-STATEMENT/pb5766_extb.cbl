      *> kb/Work PB5766: the separately compiled describer of the
      *> EXTERNAL file of pb5766_external_advancing_separate_unit. Its
      *> one WRITE names the ADVANCING phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5766B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P5766-FILE ASSIGN TO "pb5766ext.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  P5766-FILE IS EXTERNAL.
       01  P5766-REC PIC X(4).
       PROCEDURE DIVISION.
       B-PARA.
           MOVE "A2" TO P5766-REC.
           WRITE P5766-REC AFTER ADVANCING 2 LINES.
           EXIT PROGRAM.
       END PROGRAM PB5766B.
