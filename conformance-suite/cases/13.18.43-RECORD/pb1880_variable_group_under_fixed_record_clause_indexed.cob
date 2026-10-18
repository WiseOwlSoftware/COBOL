*>--- conformance-case
*> id: pb1880_variable_group_under_fixed_record_clause_indexed
*> rules: [SR-13.18.43.3-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2014 §13.18.43 (RECORD clause Format 1) over a VARIABLE-LENGTH GROUP record (§8.5.1.12.1) in an INDEXED
      *> file: the indexed sibling of pb1562_variable_group_under_fixed_record_clause (record sequential) and of its relative
      *> twin. The record holds two DYNAMIC LENGTH members around the RECORD KEY, in a file described with RECORD CONTAINS
      *> 20 CHARACTERS: legal because §13.18.43.3 SR3 bounds the description by its maximum (9 + 3 + 8 = 20, D-FRA (iv)).
      *> What the indexed organization adds is the KEY. §9.1.7.4: "For each key, the number of such components, their
      *> length, and their relative positions within the record area are fixed file attributes and shall not be changed
      *> once the physical file has been created." The key KY sits BEHIND the variable member A, so a store that placed
      *> the key where the CURRENT record puts it (after 3 characters in one record, after 1 in the next, after none in
      *> the third) would give the key a different position in every record. D-FRA (vi) (docs/CONFORMANCE.md) puts each
      *> member at the position it has at its maximum size - KY at byte 10 of 20 in every record - so the key window is
      *> the same in all of them, and a record is found, ordered and replaced by the key the program wrote.
      *>   W1 W2 W3  KB1, KA2, KC3 written in that order: '00' each (A holds 3, 1 and 0 characters).
      *>   W4        KA2 again: the prime record key is unique (§14.9.51.4 GR42 b); §9.1.7.4: "shall be unique"), the invalid key condition
      *>             '22', INVALID KEY runs.
      *>   RK1 RK3   random READ of KB1 and KC3 with junk in the members: every member comes back.
      *>   ST N1-N3  START KEY EQUAL KA2 then READ NEXT three times: the records come back in ASCENDING KEY order
      *>             (§9.1.8.2: "ascending based on the value of the key of reference"), KA2, KB1, KC3, not in the order
      *>             written; after KC3 the file is exhausted (N4 '10').
      *>   RW RK1B   REWRITE of KB1 with other member lengths; READ shows the new members and the same key.
      *>   DL RK2B   DELETE of KA2, then a READ of KA2: '23'; START KEY GREATER than KC3: '23'.
      *> WHY EACH LEG CAN FAIL: the members are overwritten with junk before each READ, so a value that is right can only
      *> have come from the file; a key window that moved with the record's lengths would answer '23' for KC3 (whose A is
      *> empty) and order the records by the wrong bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880FI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1880fi.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS KY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 20 CHARACTERS.
       01  R.
           05 A  PIC X DYNAMIC LENGTH LIMIT 9.
           05 KY PIC X(3).
           05 C  PIC X DYNAMIC LENGTH LIMIT 8.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AAA" TO A.
           MOVE "KB1" TO KY.
           MOVE "CCCC" TO C.
           WRITE R.
           DISPLAY "W1 " FS.
           MOVE "B" TO A.
           MOVE "KA2" TO KY.
           MOVE "DDDDDD" TO C.
           WRITE R.
           DISPLAY "W2 " FS.
           MOVE "" TO A.
           MOVE "KC3" TO KY.
           MOVE "" TO C.
           WRITE R.
           DISPLAY "W3 " FS.
           MOVE "XXXXXXXXX" TO A.
           MOVE "KA2" TO KY.
           MOVE "Z" TO C.
           WRITE R INVALID KEY DISPLAY "W4 IK" NOT INVALID KEY DISPLAY "W4 NIK" END-WRITE.
           DISPLAY "W4 " FS.
           CLOSE F.
           OPEN I-O F.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "KB1" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "RK1 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "KC3" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "RK3 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "KA2" TO KY.
           START F KEY IS EQUAL TO KY.
           DISPLAY "ST " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           READ F NEXT.
           DISPLAY "N1 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           READ F NEXT.
           DISPLAY "N2 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           READ F NEXT.
           DISPLAY "N3 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           READ F NEXT AT END DISPLAY "N4 AT END" NOT AT END DISPLAY "N4 NAE" END-READ.
           DISPLAY "N4 " FS.
           MOVE "KB1" TO KY.
           READ F.
           MOVE "Q" TO A.
           MOVE "EEEEEEEE" TO C.
           REWRITE R.
           DISPLAY "RW " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "KB1" TO KY.
           MOVE "Y" TO C.
           READ F.
           DISPLAY "RK1B A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "KA2" TO KY.
           DELETE F INVALID KEY DISPLAY "DL IK" END-DELETE.
           DISPLAY "DL " FS.
           READ F INVALID KEY DISPLAY "RK2B IK" NOT INVALID KEY DISPLAY "RK2B NIK" END-READ.
           DISPLAY "RK2B " FS.
           MOVE "KC3" TO KY.
           START F KEY IS GREATER THAN KY INVALID KEY DISPLAY "ST2 IK" END-START.
           DISPLAY "ST2 " FS.
           CLOSE F.
           STOP RUN.
