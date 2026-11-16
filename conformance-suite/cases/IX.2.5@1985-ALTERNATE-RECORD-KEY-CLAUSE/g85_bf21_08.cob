      *>--- conformance-case
      *> id: g85_bf21_08
      *> rules: [1985-SR-IX.2.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.3 SR1 - data-name-1 of the ALTERNATE RECORD KEY
      *> clause may be qualified. A-KEY and B-KEY are also declared in
      *> WORKING-STORAGE, so only the qualifiers (A-KEY OF A-GRP OF
      *> I-REC, two levels; B-KEY IN I-REC) name the record's items. The
      *> working-storage items hold other values, so a key bound to them
      *> (or a refused qualified name) changes the "ALT" lines: each
      *> keyed READ must find the record whose own positions match.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2108.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-F ASSIGN TO "BF2108.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS P-KEY
               ALTERNATE RECORD KEY IS A-KEY OF A-GRP OF I-REC
                   WITH DUPLICATES
               ALTERNATE RECORD KEY IS B-KEY IN I-REC.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-F.
       01  I-REC.
           05  P-KEY            PIC X(2).
           05  A-GRP.
               10  A-KEY        PIC X(2).
           05  B-KEY            PIC X(2).
           05  I-DATA           PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  A-GRP.
               10  A-KEY        PIC X(2) VALUE "QQ".
           05  B-KEY            PIC X(2) VALUE "ZZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-F.
           MOVE "P1AAX1FIRST" TO I-REC.
           WRITE I-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "P2BBX2SECND" TO I-REC.
           WRITE I-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE "P3AAX3THIRD" TO I-REC.
           WRITE I-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE IDX-F.
           OPEN INPUT IDX-F.
           MOVE "BB" TO A-KEY OF I-REC.
           READ IDX-F KEY IS A-KEY OF A-GRP OF I-REC
               INVALID KEY DISPLAY "ALT A BB INVALID".
           DISPLAY "ALT A BB " P-KEY " [" I-DATA "]".
           MOVE "AA" TO A-KEY OF I-REC.
           READ IDX-F KEY IS A-KEY OF I-REC
               INVALID KEY DISPLAY "ALT A AA INVALID".
           DISPLAY "ALT A AA " P-KEY " [" I-DATA "]".
           READ IDX-F NEXT AT END DISPLAY "ALT A NEXT END".
           DISPLAY "ALT A NEXT " P-KEY " [" I-DATA "]".
           MOVE "X3" TO B-KEY OF I-REC.
           READ IDX-F KEY IS B-KEY IN I-REC
               INVALID KEY DISPLAY "ALT B X3 INVALID".
           DISPLAY "ALT B X3 " P-KEY " [" I-DATA "]".
           CLOSE IDX-F.
           DISPLAY "WS " A-KEY OF WS-REC " " B-KEY OF WS-REC.
           STOP RUN.
