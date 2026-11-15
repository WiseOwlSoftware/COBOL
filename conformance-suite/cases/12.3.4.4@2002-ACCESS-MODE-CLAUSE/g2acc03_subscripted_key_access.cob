      *>--- conformance-case
      *> id: g2acc03_subscripted_key_access
      *> rules: [2002-GR-12.3.4.4.3-2, 2002-GR-12.3.4.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.4.3 GR2 and GR3, the arms whose key data item is
      *> a subscripted data-name (legal at 2002: §2002:12.3.4.12.2 SR1
      *> "Data-name-1 and data-name-2 may be qualified", no OCCURS ban
      *> before 2014; kb/Work PB5602). The unsubscripted arms are
      *> l1c12_access_mode_sequential_default and
      *> l1c12_access_mode_random_dynamic.
      *> GR2 c): "For indexed files this sequence is ascending within a
      *> given key of reference according to the collating sequence for
      *> that key."
      *>   cite.py --check "§2002:12.3.4.4.3 GR2" -> OK
      *> GR3 a): "For a relative file, the value of a relative key data
      *> item indicates the record to be accessed."
      *> GR3 b): "For an indexed file, the value of a record key data
      *> item indicates the record to be accessed."
      *>   cite.py --check "§2002:12.3.4.4.3 GR3" -> OK (both arms)
      *> Derived: IXR (random, RECORD KEY IXR-K(2)) is written "QC",
      *> "ZA", "MB" (keys C, A, B). A random READ with IXR-K(2) = "B"
      *> returns "MB" (GR3 b); with "D" it finds no record: INVD.
      *> IXS is the same physical file in sequential access (RECORD
      *> KEY IXS-K(2)): it reads in ascending order of the second
      *> character, ZA, MB, QC (GR2 c), neither the write order nor
      *> the order of the whole record.
      *> RLF (random, RELATIVE KEY RK(2)) is written "AAAA" at RK(2) =
      *> 3 with RK(1) = RK(3) = 1. Reading with RK(2) = 3 returns
      *> "AAAA" (GR3 a); with RK(2) = 1 and RK(1) = RK(3) = 3 it finds
      *> no record: INV1 (an operand read from another occurrence
      *> would find record 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACC03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXR ASSIGN TO "g2acc03-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IXR-K(2).
           SELECT IXS ASSIGN TO "g2acc03-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IXS-K(2).
           SELECT RLF ASSIGN TO "g2acc03-rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK(2).
       DATA DIVISION.
       FILE SECTION.
       FD  IXR.
       01  IXR-REC.
           05  IXR-K   PIC X OCCURS 2.
       FD  IXS.
       01  IXS-REC.
           05  IXS-K   PIC X OCCURS 2.
       FD  RLF.
       01  RREC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  RKT.
           05  RK      PIC 9(4) OCCURS 3.
       01  EOF         PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXR.
           MOVE "QC" TO IXR-REC. WRITE IXR-REC.
           MOVE "ZA" TO IXR-REC. WRITE IXR-REC.
           MOVE "MB" TO IXR-REC. WRITE IXR-REC.
           CLOSE IXR.
           OPEN INPUT IXR.
           MOVE SPACES TO IXR-REC. MOVE "B" TO IXR-K(2).
           READ IXR INVALID KEY DISPLAY "INVB" END-READ.
           DISPLAY "R:" IXR-REC.
           MOVE SPACES TO IXR-REC. MOVE "D" TO IXR-K(2).
           READ IXR INVALID KEY DISPLAY "INVD" END-READ.
           CLOSE IXR.
           OPEN INPUT IXS.
           PERFORM UNTIL EOF = "Y"
               READ IXS AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "S:" IXS-REC END-READ
           END-PERFORM.
           CLOSE IXS.
           OPEN OUTPUT RLF.
           MOVE 1 TO RK(1). MOVE 3 TO RK(2). MOVE 1 TO RK(3).
           MOVE "AAAA" TO RREC. WRITE RREC.
           CLOSE RLF.
           OPEN INPUT RLF.
           MOVE 3 TO RK(2). MOVE SPACES TO RREC.
           READ RLF INVALID KEY DISPLAY "INV3" END-READ.
           DISPLAY "R3=" RREC.
           MOVE 3 TO RK(1). MOVE 1 TO RK(2). MOVE 3 TO RK(3).
           READ RLF INVALID KEY DISPLAY "INV1" END-READ.
           CLOSE RLF.
           STOP RUN.
