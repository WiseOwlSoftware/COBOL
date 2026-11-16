      *>--- conformance-case
      *> id: pb5801_relative_key_level66_85
      *> rules: [1985-SR-VIII.2.4.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §1985:VIII.2.4.3 SR2: "Data-name-1 must reference
      *>   an unsigned integer data item whose description does not
      *>   contain the PICTURE symbol 'P'". K66 RENAMES the PIC 9(3) item
      *>   G-K and has its attributes (§1985:VI.5.11.4 GR2): legal.
      *> DERIVATION. The two WRITEs place THREE at record 3 and ONE at
      *>   record 1, the value of the relative key each time. A random
      *>   READ with K66 = 3 makes THREE available. §1985:VIII.4.5.4 GR14:
      *>   a format 1 READ "moves the relative record number of the
      *>   record made available to the relative key data item": the
      *>   sequential READs give 001 ONE, then 003 THREE, seen in G-K.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or the key is read from or stored into anywhere but
      *>   G-K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801RK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801RK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS K66.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(5).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-K PIC 9(3).
       66  K66 RENAMES G-K.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE 3 TO K66.
           MOVE "THREE" TO R1.
           WRITE R1 INVALID KEY DISPLAY "INV 3".
           MOVE 1 TO G-K.
           MOVE "ONE" TO R1.
           WRITE R1 INVALID KEY DISPLAY "INV 1".
           CLOSE F1.
           OPEN INPUT F1.
           MOVE 3 TO K66.
           READ F1 INVALID KEY DISPLAY "NF 3".
           DISPLAY "RANDOM " R1.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE 0 TO G-K.
           READ F1 NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "NEXT " G-K " " R1.
           READ F1 NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "NEXT " K66 " " R1.
           CLOSE F1.
           STOP RUN.
