      *>--- conformance-case
      *> id: g85_bf03_07
      *> rules: [1985-GR-IX.2.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR2 - the native character set is assumed for
      *> data on the external media of a indexed file (it has no
      *> CODE-SET clause). Each character written therefore reaches the
      *> medium and returns unconverted: D1 holds "Az9", a space,
      *> LOW-VALUE (native ordinal 1), HIGH-VALUE (native ordinal
      *> 65536) and the character of native ordinal 234 (SYMBOLIC
      *> CHARACTERS, §1985:VI.4.5.4 GR8). Each is read back
      *> and its ORD (native sequence, §1985:VI.4.4.4 GR6) printed;
      *> any conversion on the way out or back changes an ordinal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0307.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS C234 IS 234.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g85bf0307.dat"
               ORGANIZATION IS INDEXED ACCESS IS RANDOM
               RECORD KEY IS K1 FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1.
           05 K1 PIC X(2).
           05 D1 PIC X(7).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(4).
       01  I  PIC 9.
       01  N5 PIC 9(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "K1" TO K1.
           MOVE "Az9 " TO D1.
           MOVE LOW-VALUE TO D1(5:1).
           MOVE HIGH-VALUE TO D1(6:1).
           MOVE C234 TO D1(7:1).
           WRITE R1 INVALID KEY DISPLAY "W-INVALID".
           DISPLAY "WRITE=" FS.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE SPACES TO D1.
           MOVE "K1" TO K1.
           READ F1 INVALID KEY DISPLAY "R-INVALID".
           DISPLAY "READ=" FS " " D1(1:4).
           PERFORM P2 VARYING I FROM 1 BY 1 UNTIL I > 7.
           CLOSE F1.
           STOP RUN.
       P2.
           COMPUTE N5 = FUNCTION ORD(D1(I:1)).
           DISPLAY "ORD-" I "=" N5.
