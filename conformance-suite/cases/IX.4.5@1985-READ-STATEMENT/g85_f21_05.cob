      *>--- conformance-case
      *> id: g85_f21_05
      *> rules: [1985-SR-IX.4.5.3-3, 1985-SR-IX.4.5.3-4, 1985-SR-IX.4.5.3-6, 1985-GR-IX.4.5.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> One indexed file, G85F2105.DAT (prime key cols 1-2, alternate
      *> key cols 3-5), under three file-names, one per access mode,
      *> opened one at a time. WS-DUP repeats the key names of IXR-REC,
      *> so those names must be qualified to be unique.
      *> §1985:IX.4.5.3 SR4 - "Format 1 must be used for all files in
      *> sequential access mode": IXS is read only with format 1 (AT
      *> END), S1..S4, in prime key order, then the at end condition.
      *> §1985:IX.4.5.3 SR6 - "Format 2 is used for files in random
      *> access mode or for files in dynamic access mode when records
      *> are to be retrieved randomly": IXR (random) and IXD (dynamic)
      *> are read with format 2 (RA, RP, RQ, D1).
      *> §1985:IX.4.5.3 SR3 - "Data-name-1 may be qualified": RA and RQ
      *> name the alternate key as KEY IS IXR-ALT OF IXR-REC; GR15
      *> makes it the key of reference, so RA finds the record whose
      *> alternate key is YYY (K2 BBB) and RQ finds none (status 23).
      *> §1985:IX.4.5.4 GR19 - "The END-READ phrase delimits the scope
      *> of the READ statement": the DISPLAY after END-READ in each IF
      *> is outside the READ. RA succeeds; without the delimiting its
      *> DISPLAY would belong to INVALID KEY and not run. RQ fails;
      *> without the delimiting its DISPLAY would belong to NOT INVALID
      *> KEY and not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2105.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXS ASSIGN TO "G85F2105.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IXS-KEY
               ALTERNATE RECORD KEY IS IXS-ALT
               FILE STATUS IS WS-STAT.
           SELECT IXR ASSIGN TO "G85F2105.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IXR-KEY OF IXR-REC
               ALTERNATE RECORD KEY IS IXR-ALT OF IXR-REC
               FILE STATUS IS WS-STAT.
           SELECT IXD ASSIGN TO "G85F2105.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXD-KEY
               ALTERNATE RECORD KEY IS IXD-ALT
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IXS
           LABEL RECORDS ARE STANDARD.
       01  IXS-REC.
           05  IXS-KEY  PIC X(2).
           05  IXS-ALT  PIC X(3).
           05  IXS-DATA PIC X(3).
       FD  IXR
           LABEL RECORDS ARE STANDARD.
       01  IXR-REC.
           05  IXR-KEY  PIC X(2).
           05  IXR-ALT  PIC X(3).
           05  IXR-DATA PIC X(3).
       FD  IXD
           LABEL RECORDS ARE STANDARD.
       01  IXD-REC.
           05  IXD-KEY  PIC X(2).
           05  IXD-ALT  PIC X(3).
           05  IXD-DATA PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-FLAG PIC 9 VALUE 1.
       01  WS-DUP.
           05  IXR-KEY  PIC X(2) VALUE "ZZ".
           05  IXR-ALT  PIC X(3) VALUE "ZZZ".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXS.
           MOVE "K1ZZZAAA" TO IXS-REC.
           WRITE IXS-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "K2YYYBBB" TO IXS-REC.
           WRITE IXS-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE "K3XXXCCC" TO IXS-REC.
           WRITE IXS-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE IXS.
           OPEN INPUT IXS.
           READ IXS NEXT RECORD AT END DISPLAY "S1 AT END".
           DISPLAY "S1 " IXS-KEY " " IXS-DATA " " WS-STAT.
           READ IXS RECORD AT END DISPLAY "S2 AT END".
           DISPLAY "S2 " IXS-KEY " " IXS-DATA " " WS-STAT.
           READ IXS AT END DISPLAY "S3 AT END".
           DISPLAY "S3 " IXS-KEY " " IXS-DATA " " WS-STAT.
           READ IXS AT END DISPLAY "S4 AT END " WS-STAT.
           CLOSE IXS.
           OPEN INPUT IXR.
           MOVE "YYY" TO IXR-ALT OF IXR-REC.
           IF WS-FLAG = 1
               READ IXR RECORD KEY IS IXR-ALT OF IXR-REC
                   INVALID KEY DISPLAY "RA INVALID " WS-STAT
               END-READ
               DISPLAY "RA " IXR-KEY OF IXR-REC " " IXR-DATA " "
                   WS-STAT
           END-IF.
           MOVE "K3" TO IXR-KEY OF IXR-REC.
           READ IXR
               INVALID KEY DISPLAY "RP INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "RP " IXR-ALT OF IXR-REC " "
                   IXR-DATA
           END-READ.
           MOVE "QQQ" TO IXR-ALT OF IXR-REC.
           IF WS-FLAG = 1
               READ IXR KEY IS IXR-ALT OF IXR-REC
                   INVALID KEY DISPLAY "RQ INVALID " WS-STAT
                   NOT INVALID KEY DISPLAY "RQ FOUND"
               END-READ
               DISPLAY "AFTER END-READ RQ"
           END-IF.
           CLOSE IXR.
           OPEN INPUT IXD.
           MOVE "K1" TO IXD-KEY.
           READ IXD RECORD
               INVALID KEY DISPLAY "D1 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "D1 " IXD-ALT " " IXD-DATA
           END-READ.
           CLOSE IXD.
           DISPLAY "WS-DUP " WS-DUP.
           STOP RUN.
