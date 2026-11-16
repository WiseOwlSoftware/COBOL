      *>--- conformance-case
      *> id: pb5729_fd_later_record_redefines_85
      *> rules: [1985-GR-VII.4.4.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR6: the records of one FD "share the same record
      *> area" (an implicit redefinition). A REDEFINES written under
      *> the SECOND or a LATER record names a sibling inside that
      *> record, which the area makes an alias of the first record's
      *> bytes; the clause is legal there (it is level 05, not the
      *> level 01 SR3 bars). WRITE through RA puts "12AB" in the
      *> area, so after READ: RG-1 = 12, RG-1X (redefining RG-1) = 12,
      *> RG-2 = AB; and in RC, CA = CR = 12AB with CB (a group over
      *> the same area) splitting into 12 and AB. A leg fails if the
      *> compiler crashes or refuses a REDEFINES under record 2 or 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5729A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5729A.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  RA PIC X(4).
       01  RG.
           05 RG-1 PIC 99.
           05 RG-1X REDEFINES RG-1 PIC XX.
           05 RG-2 PIC XX.
       01  RC.
           05 CA PIC X(4).
           05 CR REDEFINES CA PIC X(4).
           05 CB REDEFINES CA.
              10 CB-1 PIC X(2).
              10 CB-2 PIC X(2).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F.
           MOVE "12AB" TO RA.
           WRITE RA.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF".
           DISPLAY "RG-1=" RG-1 " RG-1X=" RG-1X " RG-2=" RG-2.
           DISPLAY "CA=" CA " CR=" CR " CB=" CB-1 "/" CB-2.
           CLOSE F.
           STOP RUN.
