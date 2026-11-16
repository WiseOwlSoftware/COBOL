      *>--- conformance-case
      *> id: g85_f06_07
      *> rules: [1985-GR-X.4.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.2.4 GR2 - the Inter-Program Communication file
      *>   description entry: G-FILE IS GLOBAL (X-24) and X-FILE IS
      *>   EXTERNAL (X-23), each with LABEL RECORDS, whose rules are
      *>   in the Sequential I-O module.
      *> GLOBAL leg (§1985:X.4.6.4 GR2): the contained program
      *>   G85F0607I writes G-FILE's record without describing the
      *>   file; the container reads it back: "G READ=".
      *> EXTERNAL leg (§1985:X.4.5.4 GR4): the separately compiled
      *>   G85F0607X writes through the same external file connector
      *>   that G85F0607 opened, so the container, after CLOSE and
      *>   OPEN INPUT, reads its own record then the other program's:
      *>   "X READ 1=" and "X READ 2=".
      *> A leg fails if either FD form is refused, if the contained
      *> program cannot reach G-FILE, or if G85F0607X gets a connector
      *> of its own (its WRITE fails or the second record is absent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0607.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G-FILE ASSIGN TO G85F07G
               ORGANIZATION IS SEQUENTIAL.
           SELECT X-FILE ASSIGN TO G85F07X
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  G-FILE IS GLOBAL
           LABEL RECORDS ARE STANDARD.
       01  G-REC PIC X(6).
       FD  X-FILE IS EXTERNAL
           LABEL RECORDS ARE STANDARD.
       01  X-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT G-FILE.
           CALL "G85F0607I".
           CLOSE G-FILE.
           OPEN INPUT G-FILE.
           READ G-FILE AT END MOVE "EOF" TO G-REC.
           DISPLAY "G READ=" G-REC.
           CLOSE G-FILE.
           OPEN OUTPUT X-FILE.
           MOVE "MAIN01" TO X-REC.
           WRITE X-REC.
           CALL "G85F0607X".
           CLOSE X-FILE.
           OPEN INPUT X-FILE.
           READ X-FILE AT END MOVE "EOF" TO X-REC.
           DISPLAY "X READ 1=" X-REC.
           READ X-FILE AT END MOVE "EOF" TO X-REC.
           DISPLAY "X READ 2=" X-REC.
           READ X-FILE AT END MOVE "Y" TO WS-EOF.
           DISPLAY "X AT END=" WS-EOF.
           CLOSE X-FILE.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0607I.
       PROCEDURE DIVISION.
       P-INNER.
           MOVE "INNER1" TO G-REC.
           WRITE G-REC.
           EXIT PROGRAM.
       END PROGRAM G85F0607I.
       END PROGRAM G85F0607.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0607X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X-FILE ASSIGN TO G85F07X
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  X-FILE IS EXTERNAL
           LABEL RECORDS ARE STANDARD.
       01  X-REC PIC X(6).
       PROCEDURE DIVISION.
       P-X.
           MOVE "SEPX02" TO X-REC.
           WRITE X-REC.
           EXIT PROGRAM.
       END PROGRAM G85F0607X.
