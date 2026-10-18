*>--- conformance-case
*> id: pb1880_odo_read_explicit_record_clause
*> rules: [GR-9.1.13.6-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.6 item 4 b) - I-O status '34' for a READ of a record made variable by OCCURS DEPENDING ON,
      *> when the file description carries an EXPLICIT RECORD clause. The rule names "the value specified or implied by the
      *> file description entry": "A READ statement is unsuccessfully executed because the records are variable in length
      *> because of an OCCURS DEPENDING ON clause in the record in the associated record description entry and the
      *> associated DEPENDING ON item contains a value that makes the number of bytes in the record exceed the value
      *> specified or implied by the file description entry." The sibling golden pb1513_read_odo_record_exceeds_maximum
      *> covers the IMPLIED value (no RECORD clause: the largest description, §13.18.43.4 GR5); this one covers the
      *> SPECIFIED value, which a record clause can set ABOVE the largest description (§13.18.43.3 SR3 forbids only a
      *> description larger than integer-1). The record's size at a count N is the fixed part plus N occurrences
      *> (§13.18.43.4 GR13 c); docs/CONFORMANCE.md D-ODO2), so the READ is unsuccessful exactly when 2 + N exceeds the specified value.
      *>   OC  RECORD CONTAINS 14 CHARACTERS (Format 1, §13.18.43.4 GR6), table 1 TO 12: the specified value 14 equals the
      *>       description's own maximum. N = 12 is 14 bytes - not more than 14 - so '00'; N = 13 is 15 bytes: '34'.
      *>   OE  RECORD CONTAINS 16 CHARACTERS over the same 14-byte description: the specified value is 16, not the implied
      *>       14. N = 14 is 16 bytes, which does not exceed 16, so '00' (a READ that took the implied value would answer
      *>       '34'); N = 15 is 17 bytes: '34'.
      *>   OV  RECORD IS VARYING IN SIZE FROM 2 TO 16 CHARACTERS (Format 2, §13.18.43.4 GR7: integer-3 "specifies the maximum number of bytes in any record of the file"): the specified value is the
      *>       range's upper bound, 16 - the same two counts, the same two answers, on a variable-length file.
      *> An unsuccessful READ runs no NOT AT END phrase (§9.1.13.1: for a fatal condition "Any NOT AT END or NOT INVALID KEY
      *> phrase specified for that statement is ignored"), and sets FILE STATUS to '34'.
      *> WHY EACH LEG CAN FAIL: the writer FD (PIC X on the same physical file) gives every record any count, and a leg that
      *> ignored the explicit value would answer '34' at OE's N = 14 and OV's N = 14; a leg that ignored the rule altogether
      *> would answer '00' at the second READ of each file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880OA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OC ASSIGN TO "pb1880oc.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT OCW ASSIGN TO "pb1880oc.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT OE ASSIGN TO "pb1880oe.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT OEW ASSIGN TO "pb1880oe.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT OV ASSIGN TO "pb1880ov.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT OVW ASSIGN TO "pb1880ov.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  OC RECORD CONTAINS 14 CHARACTERS.
       01  OC-R.
           05 OC-N PIC 99.
           05 OC-T PIC X OCCURS 1 TO 12 TIMES DEPENDING ON OC-N.
       FD  OCW RECORD CONTAINS 14 CHARACTERS.
       01  OCW-R PIC X(14).
       FD  OE RECORD CONTAINS 16 CHARACTERS.
       01  OE-R.
           05 OE-N PIC 99.
           05 OE-T PIC X OCCURS 1 TO 12 TIMES DEPENDING ON OE-N.
       FD  OEW RECORD CONTAINS 16 CHARACTERS.
       01  OEW-R PIC X(16).
       FD  OV RECORD IS VARYING IN SIZE FROM 2 TO 16 CHARACTERS
               DEPENDING ON OV-L.
       01  OV-R.
           05 OV-N PIC 99.
           05 OV-T PIC X OCCURS 1 TO 12 TIMES DEPENDING ON OV-N.
       FD  OVW RECORD IS VARYING IN SIZE FROM 2 TO 16 CHARACTERS
               DEPENDING ON OVW-L.
       01  OVW-R PIC X(16).
       WORKING-STORAGE SECTION.
       01  FS    PIC XX.
       01  OV-L  PIC 99.
       01  OVW-L PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT OCW.
           MOVE "12ABCDEFGHIJKL" TO OCW-R.
           WRITE OCW-R.
           MOVE "13ABCDEFGHIJKL" TO OCW-R.
           WRITE OCW-R.
           CLOSE OCW.
           OPEN INPUT OC.
           READ OC NOT AT END DISPLAY "OC1 NAE N=" OC-N END-READ.
           DISPLAY "OC1 " FS.
           READ OC NOT AT END DISPLAY "OC2 NAE N=" OC-N END-READ.
           DISPLAY "OC2 " FS.
           CLOSE OC.
           OPEN OUTPUT OEW.
           MOVE "14ABCDEFGHIJKLMN" TO OEW-R.
           WRITE OEW-R.
           MOVE "15ABCDEFGHIJKLMN" TO OEW-R.
           WRITE OEW-R.
           CLOSE OEW.
           OPEN INPUT OE.
           READ OE NOT AT END DISPLAY "OE1 NAE N=" OE-N END-READ.
           DISPLAY "OE1 " FS.
           READ OE NOT AT END DISPLAY "OE2 NAE N=" OE-N END-READ.
           DISPLAY "OE2 " FS.
           CLOSE OE.
           OPEN OUTPUT OVW.
           MOVE 16 TO OVW-L.
           MOVE "14ABCDEFGHIJKLMN" TO OVW-R.
           WRITE OVW-R.
           MOVE "15ABCDEFGHIJKLMN" TO OVW-R.
           WRITE OVW-R.
           CLOSE OVW.
           OPEN INPUT OV.
           READ OV NOT AT END DISPLAY "OV1 NAE N=" OV-N END-READ.
           DISPLAY "OV1 " FS.
           READ OV NOT AT END DISPLAY "OV2 NAE N=" OV-N END-READ.
           DISPLAY "OV2 " FS.
           CLOSE OV.
           STOP RUN.
