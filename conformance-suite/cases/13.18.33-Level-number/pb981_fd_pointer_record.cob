*>--- conformance-case
*> id: pb981_fd_pointer_record
*> rules: [GR-13.18.33.4-3, DOC-A.1-216]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB981, PB4838 - determination D-FRA (docs/CONFORMANCE.md section 3): a level-1 POINTER record is
      *> legal in the file section (ISO 13.18.60.3 SR14 - "only for an elementary data item at level 1")
      *> and ISO 13.18.33.4 GR3 makes it an implicit redefinition of the FD's area. Its storage is the area's
      *> first 8 positions, the pointer's token of DOC-A.1-216 (the owner decided on 2026-11-06 that a file carries a
      *> pointer leaf as its token and that READ decodes it back), so it is an OUT-OF-LINE record: a WRITE of it sends the
      *> token (fitted to the fixed record size) and a READ makes the record available in it by decoding the
      *> first 8 positions of the record.
      *>   record 1 "0123456789" : R2 = 0123456789; RP decodes "01234567", which this run unit never produced as an
      *>                           address, so RP is no longer P2 (and is not NULL)
      *>   record 2 : RP's token fitted to the fixed record size - RP decodes to the address it was written with (P2),
      *>              and R2 ends in the two fit spaces
      *> This used to be refused (COBOL1697, a REDEFINES rule the source never invoked, then COBOL0899).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB981PTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb981ptr.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R2 PIC X(10).
       01 RP USAGE POINTER.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "WXYZ".
       01 P2 USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET RP TO ADDRESS OF X
           SET P2 TO ADDRESS OF X
           OPEN OUTPUT F
           MOVE "0123456789" TO R2
           WRITE R2
           SET RP TO P2
           WRITE RP
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "R2=[" R2 "]"
           IF RP = P2
               DISPLAY "RP SAME"
           ELSE
               DISPLAY "RP CHANGED"
           END-IF
           IF RP = NULL
               DISPLAY "RP NULL"
           ELSE
               DISPLAY "RP NOT NULL"
           END-IF
           READ F
           DISPLAY "R2 TAIL=[" R2(9:2) "]"
           IF RP = P2
               DISPLAY "RP SAME"
           ELSE
               DISPLAY "RP CHANGED"
           END-IF
           CLOSE F
           STOP RUN.
