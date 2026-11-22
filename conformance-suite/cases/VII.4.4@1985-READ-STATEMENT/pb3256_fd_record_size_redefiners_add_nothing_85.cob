      *>--- conformance-case
      *> id: pb3256_fd_record_size_redefiners_add_nothing_85
      *> rules: [1985-GR-VII.3.8.4-1, 1985-GR-VI.5.10.4-1, 1985-GR-VII.4.4.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3256, the 1985 twin of
      *> pb3256_fd_record_size_redefiners_add_nothing. FD F has no
      *> RECORD clause, so each record's size is the one its record
      *> description entry defines (§1985:VII.3.8.4 GR1). Every
      *> record is 4 characters: in RC, CR and CB REDEFINES CA, and
      *> a redefiner's storage starts at the item it redefines
      *> (§1985:VI.5.10.4 GR1), so neither adds to RC's size. The
      *> compiler once counted both and gave F a 12-character record.
      *> G writes one 6-character record "12AB56" to the same
      *> physical file. The implementor's fixed-length record
      *> sequential format carries no record length on the medium
      *> (docs/CONFORMANCE.md DOC-A.1-146), so F reads the 6
      *> characters as one full 4-character record and a short final
      *> record of 2. §1985:VII.4.4.4 GR13: a record shorter than
      *> the minimum is read successfully with the record length
      *> conflict status '04', the area to the right of the
      *> characters read undefined (only RA(1:2) is shown).
      *> Expected: R1 00 12AB, R2 04 56, then the AT END phrase
      *> with '10' (the declarative does not run), then a READ with
      *> no AT END phrase after the end, '46', which runs the USE
      *> declarative. At a 12-character record the first READ is
      *> the short one: R1 04 12AB, then AT END on the second READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3256B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "PB3256B.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F ASSIGN TO "PB3256B.DAT"
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
