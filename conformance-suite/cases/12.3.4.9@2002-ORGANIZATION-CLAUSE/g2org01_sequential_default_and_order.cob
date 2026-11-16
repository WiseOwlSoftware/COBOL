      *>--- conformance-case
      *> id: g2org01_sequential_default_and_order
      *> rules: [2002-GR-12.3.4.9.2-2, 2002-GR-12.3.4.9.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.9.2 GR2 - "The SEQUENTIAL phrase specifies that the
      *> file organization is sequential. Sequential organization is a
      *> permanent logical file structure in which a record is
      *> identified by a predecessor-successor relationship established
      *> when the record is placed into the file."  -> cite.py OK
      *> §2002:12.3.4.9.2 GR5 - "When the ORGANIZATION clause is not
      *> specified, sequential organization is implied." -> cite.py OK
      *> §2002:12.3.4.9.2 GR1 - "The file organization is established at
      *> the time a physical file is created and cannot subsequently be
      *> changed." §2002:14.8.26.3 GR8: on OPEN the connector's attributes
      *> "are compared with the fixed file attributes of the physical
      *> file", a mismatch gives '39'; the implementor's validated set
      *> (docs/conformance DOC-A.1-129 (3)) makes a RELATIVE OPEN of a
      *> file that is not a keyed store a '39'.  -> cite.py OK (both)
      *> F0 has NO ORGANIZATION clause (GR5 leg); FS says ORGANIZATION
      *> IS SEQUENTIAL (GR2 leg); FR says RELATIVE. All name one file.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   F0 writes ZZZZ, AAAA, then X"410A4220" (a line-feed byte
      *>   inside the record) in that order: OPEN 00.
      *>   FS reads them back in the order they were placed (GR2: the
      *>   predecessor-successor relationship, not key or collating
      *>   order), the third unchanged (records, not lines), then at end:
      *>     SEQ 1 00 ZZZZ / SEQ 2 00 AAAA / SEQ 3 00 SAME / SEQ 4 10
      *>   F0's file was created with sequential organization (GR5), so
      *>   the RELATIVE connector conflicts with it      => REL OPEN 39
      *> A leg fails if the omitted clause gives another organization
      *> (REL OPEN 00, or FS cannot read it), if records come back in
      *> another order (SEQ 1 AAAA), or if the X"0A" byte splits or
      *> alters the record (SEQ 3 DIFF or an extra record).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ORG01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F0 ASSIGN TO "G2ORG01.DAT"
               FILE STATUS IS FS0.
           SELECT FS ASSIGN TO "G2ORG01.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSS.
           SELECT FR ASSIGN TO "G2ORG01.DAT"
               ORGANIZATION IS RELATIVE
               FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  F0.
       01  R0          PIC X(4).
       FD  FS.
       01  RS          PIC X(4).
       FD  FR.
       01  RR          PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS0         PIC XX.
       01  FSS         PIC XX.
       01  FSR         PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F0.
           DISPLAY "OPEN " FS0.
           MOVE "ZZZZ" TO R0. WRITE R0.
           MOVE "AAAA" TO R0. WRITE R0.
           MOVE X"410A4220" TO R0. WRITE R0.
           CLOSE F0.
           OPEN INPUT FS.
           READ FS AT END CONTINUE END-READ.
           DISPLAY "SEQ 1 " FSS " " RS.
           READ FS AT END CONTINUE END-READ.
           DISPLAY "SEQ 2 " FSS " " RS.
           READ FS AT END CONTINUE END-READ.
           IF RS = X"410A4220"
               DISPLAY "SEQ 3 " FSS " SAME"
           ELSE
               DISPLAY "SEQ 3 " FSS " DIFF"
           END-IF.
           READ FS AT END CONTINUE END-READ.
           DISPLAY "SEQ 4 " FSS.
           CLOSE FS.
           OPEN INPUT FR.
           DISPLAY "REL OPEN " FSR.
           STOP RUN.
