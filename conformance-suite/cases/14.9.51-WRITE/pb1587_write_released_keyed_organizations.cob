*>--- conformance-case
*> id: pb1587_write_released_keyed_organizations
*> rules: [GR-14.9.51.4-12, GR-9.1.15-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1587 - the RELATIVE and INDEXED twin of
      *> pb1587_write_released_to_input_reader: a record a WRITE released
      *> is in the physical file when the WRITE returns, so the INPUT
      *> connector that §9.1.15 2) admits beside a SHARING WITH READ ONLY
      *> writer READs it, in EVERY organization.
      *>
      *> "The successful execution of a WRITE statement releases a
      *> logical record to the operating environment."
      *>   cite.py: OK  §14.9.51.4 12)  (General rules)
      *> "The sharing with read only mode restricts concurrent access
      *> to a physical file through file connectors other than this
      *> one, to input mode."
      *>   cite.py: OK  §9.1.15 2)  (Sharing mode)
      *> "If the record is not in the sequence above, the execution of
      *> the WRITE statement is unsuccessful, the invalid key condition
      *> exists, and the I-O status for the write file connector is set
      *> to '21'."
      *>   cite.py: OK  §14.9.51.4 38)  (General rules)
      *> "If the file position indicator indicates that an optional
      *> input file is not present or that no next or previous logical
      *> record exists, the I-O status value associated with file-name-1
      *> is set to '10', the at end condition exists"
      *>   cite.py: OK  §14.9.30.4 21)  (General rules)
      *>
      *> DERIVATION. RW and IW are the READ ONLY writers of a relative
      *> and an indexed file (sequential access), RR and IR their
      *> ALL OTHER readers (Table 19 admits an INPUT request of that mode
      *> beside a READ ONLY OUTPUT connector).
      *>   REL the file is empty: RR's READ is '10' (AT END phrase);
      *>     RW releases two records (relative record numbers 1 and 2 by
      *>     §14.9.51.4 GR29 a)); a fresh RR reads both, then '10'.
      *>   IDX the file is empty: IR's READ is '10'; IW releases K001
      *>     and K002; a third WRITE of K002 is not in ascending
      *>     sequence (GR38): '21', handled by the INVALID KEY phrase,
      *>     and the fourth, K001, is the same refusal with NO phrase
      *>     (the FILE STATUS item carries it and the run goes on).
      *>     Neither refused record is released, so a fresh IR reads
      *>     exactly K001 and K002, then '10'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1587KEY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "pb1587rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS RW-ST.
           SELECT RR ASSIGN TO "pb1587rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS RR-ST.
           SELECT IW ASSIGN TO "pb1587idx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IW-KEY
               SHARING WITH READ ONLY
               FILE STATUS IS IW-ST.
           SELECT IR ASSIGN TO "pb1587idx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IR-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS IR-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RW.
       01 RW-REC PIC X(8).
       FD RR.
       01 RR-REC PIC X(8).
       FD IW.
       01 IW-REC.
          05 IW-KEY PIC X(4).
          05 IW-VAL PIC X(4).
       FD IR.
       01 IR-REC.
          05 IR-KEY PIC X(4).
          05 IR-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 RW-ST  PIC XX.
       01 RR-ST  PIC XX.
       01 IW-ST  PIC XX.
       01 IR-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RW
           DISPLAY "REL OPEN RW " RW-ST
           OPEN INPUT RR
           DISPLAY "REL OPEN RR " RR-ST
           READ RR
               AT END DISPLAY "REL READ AT END " RR-ST
               NOT AT END DISPLAY "REL READ GOT " RR-REC
           END-READ
           CLOSE RR
           MOVE "REL-ONE " TO RW-REC
           WRITE RW-REC
           DISPLAY "REL WRITE " RW-ST
           MOVE "REL-TWO " TO RW-REC
           WRITE RW-REC
           DISPLAY "REL WRITE " RW-ST
           OPEN INPUT RR
           READ RR
           DISPLAY "REL READ " RR-ST " " RR-REC
           READ RR
           DISPLAY "REL READ " RR-ST " " RR-REC
           READ RR
               AT END DISPLAY "REL READ AT END " RR-ST
               NOT AT END DISPLAY "REL READ GOT " RR-REC
           END-READ
           CLOSE RR
           CLOSE RW
           OPEN OUTPUT IW
           DISPLAY "IDX OPEN IW " IW-ST
           OPEN INPUT IR
           DISPLAY "IDX OPEN IR " IR-ST
           READ IR
               AT END DISPLAY "IDX READ AT END " IR-ST
               NOT AT END DISPLAY "IDX READ GOT " IR-REC
           END-READ
           CLOSE IR
           MOVE "K001AAAA" TO IW-REC
           WRITE IW-REC
           DISPLAY "IDX WRITE " IW-ST
           MOVE "K002BBBB" TO IW-REC
           WRITE IW-REC
           DISPLAY "IDX WRITE " IW-ST
           MOVE "K002CCCC" TO IW-REC
           WRITE IW-REC
               INVALID KEY DISPLAY "IDX WRITE INVALID KEY " IW-ST
               NOT INVALID KEY DISPLAY "IDX WRITE ACCEPTED " IW-ST
           END-WRITE
           MOVE "K001DDDD" TO IW-REC
           WRITE IW-REC
           DISPLAY "IDX WRITE " IW-ST
           OPEN INPUT IR
           READ IR
           DISPLAY "IDX READ " IR-ST " " IR-REC
           READ IR
           DISPLAY "IDX READ " IR-ST " " IR-REC
           READ IR
               AT END DISPLAY "IDX READ AT END " IR-ST
               NOT AT END DISPLAY "IDX READ GOT " IR-REC
           END-READ
           CLOSE IR
           CLOSE IW
           STOP RUN.
