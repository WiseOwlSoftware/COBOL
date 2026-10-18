*>--- conformance-case
*> id: pb1880_variable_group_under_fixed_record_clause_relative
*> rules: [SR-13.18.43.3-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2014 §13.18.43 (RECORD clause Format 1) over a VARIABLE-LENGTH GROUP record (§8.5.1.12.1) in a RELATIVE
      *> file: the relative sibling of pb1562_variable_group_under_fixed_record_clause (record sequential). The record holds
      *> two DYNAMIC LENGTH members around a fixed one, in a file described with the fixed-length clause RECORD CONTAINS 20
      *> CHARACTERS. It is LEGAL: §13.18.43.3 SR3 - "No record description entry for the file may specify a number of bytes
      *> greater than integer-1" - bounds the description by its MAXIMUM (§13.18.43.4 GR8 b), by D-FRA (iv): a
      *> dynamic-length item counts at its maximum size), and that maximum is its LIMIT (§8.5.1.10.1): 9 + 3 + 8 = 20
      *> bytes, exactly integer-1. A variable-length group "behaves in all respects as though it were in fact contiguous
      *> with its neighbors" (§8.5.1.11.2), so every record is the concatenation of its members as they stand at the WRITE,
      *> and a READ makes that very record available again: each member takes back exactly the characters it was written
      *> with. The physical form is the implementor's (§13.18.43.4 GR2); docs/CONFORMANCE.md D-FRA (vi) fixes it for a
      *> FIXED-length file of ANY organization - each member at the position it has at its maximum size, space padded, read
      *> back without the padding - because a relative record is 20 bytes whatever it holds (GR6).
      *> The relative record number is the record's identity (§9.1.7.3; §14.9.51.4 GR29), and the ARMS below pin what the
      *> record number does and does not change:
      *>   W1 W3 W2  three records written out of order into slots 1, 3 and 2: '00' each.
      *>   R3 R1     random READ of slot 3 then slot 1 with the members overwritten with junk first: every member comes back.
      *>   RW1       REWRITE of slot 1 with other lengths in the same three members: '00', and R1B reads the new members.
      *>   R2        the empty-member record (both variable members empty, the minimum the description describes).
      *> and the exception paths of the same statements, because the fixed form must not hide them:
      *>   W1D       a WRITE of slot 1 again: the invalid key condition (§14.9.51.4 GR33 a): the record number names a record that already exists), '22', INVALID KEY runs.
      *>   R4        a READ of slot 4, never written: '23', INVALID KEY runs and NOT INVALID KEY does not.
      *>   D2 R2B    DELETE of slot 2, then READ of slot 2: '23' (the fixed form leaves nothing behind in the slot).
      *> WHY EACH LEG CAN FAIL: the members are overwritten with junk before every READ, so a value that is right can only
      *> have come from the file. Before kb/Work PB1562 the record came back as the 20 characters poured into the first
      *> dynamic member (A=[AAAKEYCCCC       ] KY=[   ] C=[]).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880FR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1880fr.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK
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
       01  RK PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AAA" TO A.
           MOVE "KEY" TO KY.
           MOVE "CCCC" TO C.
           MOVE 1 TO RK.
           WRITE R.
           DISPLAY "W1 " FS.
           MOVE "B" TO A.
           MOVE "K2K" TO KY.
           MOVE "DDDDDD" TO C.
           MOVE 3 TO RK.
           WRITE R.
           DISPLAY "W3 " FS.
           MOVE "" TO A.
           MOVE "K3K" TO KY.
           MOVE "" TO C.
           MOVE 2 TO RK.
           WRITE R.
           DISPLAY "W2 " FS.
           MOVE 1 TO RK.
           WRITE R INVALID KEY DISPLAY "W1D IK" NOT INVALID KEY DISPLAY "W1D NIK" END-WRITE.
           DISPLAY "W1D " FS.
           CLOSE F.
           OPEN I-O F.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           MOVE 3 TO RK.
           READ F.
           DISPLAY "R3 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           MOVE 1 TO RK.
           READ F.
           DISPLAY "R1 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "Q" TO A.
           MOVE "RWR" TO KY.
           MOVE "EEEEEEEE" TO C.
           REWRITE R.
           DISPLAY "RW1 " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           MOVE 1 TO RK.
           READ F.
           DISPLAY "R1B A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE "ZZZZZZZZ" TO A.
           MOVE "???" TO KY.
           MOVE "Y" TO C.
           MOVE 2 TO RK.
           READ F.
           DISPLAY "R2 A=[" A "] KY=[" KY "] C=[" C "] " FS.
           MOVE 4 TO RK.
           READ F INVALID KEY DISPLAY "R4 IK" NOT INVALID KEY DISPLAY "R4 NIK" END-READ.
           DISPLAY "R4 " FS.
           MOVE 2 TO RK.
           DELETE F INVALID KEY DISPLAY "D2 IK" END-DELETE.
           DISPLAY "D2 " FS.
           READ F INVALID KEY DISPLAY "R2B IK" NOT INVALID KEY DISPLAY "R2B NIK" END-READ.
           DISPLAY "R2B " FS.
           CLOSE F.
           STOP RUN.
