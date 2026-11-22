      *>--- conformance-case
      *> id: pb3256_fd_record_size_redefiners_add_nothing
      *> rules: [GR-13.18.43.4-5, GR-13.18.44.4-1, GR-14.9.30.4-14]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3256. FD F has no RECORD clause, so its record
      *> size is that of its largest record description entry
      *> (§2023:13.18.43.4 GR5a). Every record is 4 bytes: in RC,
      *> CR and CB REDEFINES CA, and a redefiner's storage starts at
      *> the first bit of the item it redefines (§2023:13.18.44.4
      *> GR1), so neither adds to RC's size. The compiler once
      *> counted both and gave F a 12-byte record.
      *> G writes one 6-byte record "12AB56" to the same physical
      *> file. The implementor's fixed-length record sequential
      *> format carries no record length on the medium
      *> (docs/CONFORMANCE.md DOC-A.1-146), so F reads the 6 bytes
      *> as one full 4-byte record and a short final record of 2.
      *> §2023:14.9.30.4 GR14: a record shorter than the minimum is
      *> read successfully with I-O status '04', the area to the
      *> right of the bytes read undefined (only RA(1:2) is shown).
      *> Expected: R1 00 12AB, R2 04 56, then the AT END phrase
      *> with '10' (the declarative does not run), then a READ with
      *> no AT END phrase after the end, '46', which runs the USE
      *> declarative. At a 12-byte record the first READ is the
      *> short one: R1 04 12AB, then AT END on the second READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3256A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "PB3256A.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F ASSIGN TO "PB3256A.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  G.
       01  GR PIC X(6).
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
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       F-ERR-1.
           DISPLAY "DECL " FS.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT G.
           MOVE "12AB56" TO GR.
           WRITE GR.
           CLOSE G.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF-1 " FS.
           DISPLAY "R1 " FS " " RA.
           READ F AT END DISPLAY "EOF-2 " FS.
           DISPLAY "R2 " FS " " RA(1:2).
           READ F AT END DISPLAY "AT END " FS.
           READ F.
           DISPLAY "R4 " FS.
           CLOSE F.
           STOP RUN.
