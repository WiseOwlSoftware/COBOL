      *>--- conformance-case
      *> id: g85_bf22_02
      *> rules: [1985-SR-VIII.3.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.3.2.3 SR2 - the clauses after file-name-1 in any order
      *> (relative file). RULE (cite.py OK): "The clauses which follow
      *> file-name-1 may appear in any order." The printed order
      *> (§1985:VIII.3.2.2) is BLOCK, RECORD, LABEL, VALUE OF, DATA. REL-F
      *> writes them REVERSED: DATA / LABEL / RECORD / BLOCK. The program
      *> must translate at 85 and the file must work. DERIVATION: two
      *> 4-character records QRST, UVWX written in sequential access,
      *> read back in order, then AT END: R1=QRST R2=UVWX EOF.
      *> A leg fails if the reversed order is refused or the records
      *> differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2202.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-F ASSIGN TO "G85BF222.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F
           DATA RECORD IS REL-F-REC
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 4 CHARACTERS
           BLOCK CONTAINS 1 RECORDS.
       01  REL-F-REC PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT REL-F.
           MOVE "QRST" TO REL-F-REC.
           WRITE REL-F-REC INVALID KEY DISPLAY "BADW".
           MOVE "UVWX" TO REL-F-REC.
           WRITE REL-F-REC INVALID KEY DISPLAY "BADW".
           CLOSE REL-F.
           OPEN INPUT REL-F.
           READ REL-F AT END DISPLAY "EOF1".
           DISPLAY "R1=" REL-F-REC.
           READ REL-F AT END DISPLAY "EOF2".
           DISPLAY "R2=" REL-F-REC.
           READ REL-F AT END DISPLAY "EOF".
           CLOSE REL-F.
           STOP RUN.
