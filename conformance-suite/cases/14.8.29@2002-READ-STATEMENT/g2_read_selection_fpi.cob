      *>--- conformance-case
      *> id: g2_read_selection_fpi
      *> rules: [2002-GR-14.8.29.3-18]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.3 GR18 - the record a sequential READ selects
      *> from the file position indicator (FPI). Pins:
      *> OPT  GR18b: an optional input file not present: "the I-O
      *>      status value associated with file-name-1 is set to '10',
      *>      the at end condition exists" -> 10, AT END taken.
      *> SEQ  GR18c1 after OPEN, the first record ">= the FPI" (A);
      *>      GR18d after a READ, "greater than the FPI" (B).
      *> REL  relative file, records 9 and 10 only, RELATIVE KEY PIC 9.
      *>      GR18c1 selects record 9 (00, and by GR22 RK1 = 9); then
      *>      GR18d selects record 10, whose 2 significant digits exceed
      *>      the 1-digit relative key: "the I-O status value ... is set
      *>      to '14', the at end condition exists" -> 14, AT END taken.
      *> DUP  alternate key WITH DUPLICATES, written 1Z 2Z 3Z 4Y 5Y;
      *>      GR23 orders each set of duplicates as written. START = Z
      *>      then NEXT: GR18c1 -> 1; GR18e1a (next in the set) -> 2, 3;
      *>      GR18e1b: no key greater than Z -> DUPEND 10.
      *> PRV  START = Z, NEXT -> 1; PREVIOUS: no duplicate before 1, so
      *>      GR18e2b "the last record within the set of duplicates ...
      *>      whose key value is the first key value less than the FPI"
      *>      -> 5 (last of the Y set); GR18e2a -> 4; then nothing less
      *>      than Y -> PRVEND 10.
      *> NOP  GR18a: a failed random READ (invalid key, 23) leaves no
      *>      valid record position (GR15), so the next sequential READ
      *>      "is unsuccessful": 46 (§2002:9.1.12.6), not an at end, so
      *>      AT END is not taken (GR12).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD18.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL FO ASSIGN TO "g2rd18-absent.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS S1.
           SELECT FS ASSIGN TO "g2rd18s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS S2.
           SELECT RW ASSIGN TO "g2rd18r.dat"
               ORGANIZATION RELATIVE ACCESS RANDOM
               RELATIVE KEY RK2 FILE STATUS S3.
           SELECT RR ASSIGN TO "g2rd18r.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               RELATIVE KEY RK1 FILE STATUS S3.
           SELECT FI ASSIGN TO "g2rd18i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IK
               ALTERNATE RECORD KEY AK WITH DUPLICATES
               FILE STATUS S4.
       DATA DIVISION.
       FILE SECTION.
       FD FO.
       01 RO PIC X.
       FD FS.
       01 RS PIC X.
       FD RW.
       01 RWR PIC X.
       FD RR.
       01 RRR PIC X.
       FD FI.
       01 RI.
          05 IK PIC X.
          05 AK PIC X.
       WORKING-STORAGE SECTION.
       01 S1 PIC XX.
       01 S2 PIC XX.
       01 S3 PIC XX.
       01 S4 PIC XX.
       01 S5 PIC XX.
       01 RK2 PIC 99.
       01 RK1 PIC 9.
       01 I PIC 99.
       01 AE PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT FO.
           MOVE "N" TO AE.
           READ FO AT END MOVE "Y" TO AE END-READ.
           DISPLAY "OPT " S1 " " AE.
           CLOSE FO.
           OPEN OUTPUT FS.
           MOVE "A" TO RS. WRITE RS. MOVE "B" TO RS. WRITE RS.
           CLOSE FS.
           OPEN INPUT FS.
           READ FS. DISPLAY "SEQ " RS WITH NO ADVANCING.
           READ FS. DISPLAY " " RS.
           CLOSE FS.
           OPEN OUTPUT RW.
           PERFORM VARYING I FROM 9 BY 1 UNTIL I > 10
               MOVE I TO RK2 MOVE "X" TO RWR WRITE RWR
           END-PERFORM.
           CLOSE RW.
           OPEN INPUT RR.
           READ RR AT END DISPLAY "REL09 AT END" END-READ.
           DISPLAY "REL09 " S3 " " RK1.
           MOVE "N" TO AE.
           READ RR AT END MOVE "Y" TO AE END-READ.
           DISPLAY "REL10 " S3 " " AE.
           CLOSE RR.
           OPEN OUTPUT FI.
           MOVE "1Z" TO RI. WRITE RI.
           MOVE "2Z" TO RI. WRITE RI.
           MOVE "3Z" TO RI. WRITE RI.
           MOVE "4Y" TO RI. WRITE RI.
           MOVE "5Y" TO RI. WRITE RI.
           CLOSE FI.
           OPEN INPUT FI.
           MOVE "Z" TO AK.
           START FI KEY IS EQUAL TO AK.
           READ FI NEXT. DISPLAY "DUP " IK WITH NO ADVANCING.
           READ FI NEXT. DISPLAY " " IK WITH NO ADVANCING.
           READ FI NEXT. DISPLAY " " IK.
           READ FI NEXT AT END CONTINUE END-READ.
           DISPLAY "DUPEND " S4.
           MOVE "Z" TO AK.
           START FI KEY IS EQUAL TO AK.
           READ FI NEXT. DISPLAY "PRV " IK WITH NO ADVANCING.
           READ FI PREVIOUS. DISPLAY " " IK WITH NO ADVANCING.
           READ FI PREVIOUS. DISPLAY " " IK.
           READ FI PREVIOUS AT END CONTINUE END-READ.
           DISPLAY "PRVEND " S4.
           MOVE "9" TO IK.
           READ FI KEY IS IK INVALID KEY CONTINUE END-READ.
           MOVE S4 TO S5.
           MOVE "N" TO AE.
           READ FI NEXT AT END MOVE "Y" TO AE END-READ.
           DISPLAY "NOP " S5 " " S4 " " AE.
           CLOSE FI.
           STOP RUN.
