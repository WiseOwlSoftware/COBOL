*>--- conformance-case
*> id: pb1880_national_pad_record_sequential_and_relative
*> rules: [GR-14.9.30.4-14, GR-14.9.51.4-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> WiseOwl COBOL's DETERMINATION for the content of record-area positions the standard leaves undefined, on a
      *> NATIONAL record area, in the two fixed organizations other than LINE SEQUENTIAL (whose national fill is
      *> standard-defined and witnessed by pb327_national_line_sequential_fill, §14.9.30.4 GR15). The standard says only
      *> that the bytes of a WRITE past the end of record-name-1 are undefined (§14.9.51.4 GR13, Annex A.2 item 64) and
      *> that on a short record sequential READ "the portion of the record area that is to the right of the last valid
      *> character read is undefined" (§14.9.30.4 GR14); docs/CONFORMANCE.md D-WRT1 and docs/COBOLNET_FILES_DESIGN.md D30
      *> fix the content: a position no record description occupies, and a position past the end of a short stored record,
      *> is a SPACE - and in a national record area the national space, the two bytes 00 20 (D-N1: UTF-16BE), never the
      *> byte pair 20 20 (U+2020, a character that is not a space at all). One pad serves every organization
      *> (FileConnector.FitRecord, kb/Work PB679).
      *> ORD over the byte view is what tells the pads apart: the byte 00 is ORD 001 and the byte 20 is ORD 033, so a
      *> national space reads 001, 033 and a byte-level space pair reads 033, 033.
      *>   S/R  RECORD CONTAINS 12 CHARACTERS, the record N(4) holding "CDEF" (8 bytes): positions 9 to 12 belong to no
      *>        record description, so the record on the medium ends 001 033 001 033, in a record sequential file
      *>        (S9-S12) and in a relative file (R9-R12), read back through a 12-byte alphanumeric description.
      *>   V/Q  RECORD IS VARYING IN SIZE FROM 2 TO 8, the record N(4) written at 4 bytes ("CD" of "CDEF"): the READ of the
      *>        short record makes 4 bytes available (RL = 04) and fills positions 5 to 8 with the national space:
      *>        V5-V8 (record sequential) and Q5-Q8 (relative), both 001 033 001 033.
      *> Every statement is checked through FILE STATUS ('00' for each success).
      *> WHY EACH LEG CAN FAIL: a byte-level PadRight(' ') anywhere in the four paths yields 033 033 033 033 for the tail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880NP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1880nus.dat"
              ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT SR ASSIGN TO "pb1880nus.dat"
              ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT RW ASSIGN TO "pb1880nur.dat"
              ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
              RELATIVE KEY IS RKW FILE STATUS IS FS.
           SELECT RR ASSIGN TO "pb1880nur.dat"
              ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
              RELATIVE KEY IS RK FILE STATUS IS FS.
           SELECT VW ASSIGN TO "pb1880nvs.dat"
              ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT VR ASSIGN TO "pb1880nvs.dat"
              ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT QW ASSIGN TO "pb1880nvr.dat"
              ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
              RELATIVE KEY IS RKW FILE STATUS IS FS.
           SELECT QR ASSIGN TO "pb1880nvr.dat"
              ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
              RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SW RECORD CONTAINS 12 CHARACTERS.
       01 SW-REC PIC N(4).
       FD SR RECORD CONTAINS 12 CHARACTERS.
       01 SR-BYTES PIC X(12).
       FD RW RECORD CONTAINS 12 CHARACTERS.
       01 RW-REC PIC N(4).
       FD RR RECORD CONTAINS 12 CHARACTERS.
       01 RR-BYTES PIC X(12).
       FD VW RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
              DEPENDING ON WL.
       01 VW-REC PIC N(4).
       FD VR RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
              DEPENDING ON RL.
       01 VR-REC PIC N(4).
       01 VR-BYTES PIC X(8).
       FD QW RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
              DEPENDING ON WL.
       01 QW-REC PIC N(4).
       FD QR RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS
              DEPENDING ON RL.
       01 QR-REC PIC N(4).
       01 QR-BYTES PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 WL  PIC 99.
       01 RL  PIC 99.
       01 RK  PIC 99.
       01 RKW PIC 99.
       01 W-I PIC 99.
       01 W-O PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SW.
           MOVE N"CDEF" TO SW-REC.
           WRITE SW-REC.
           DISPLAY "SW " FS.
           CLOSE SW.
           OPEN INPUT SR.
           READ SR AT END DISPLAY "EOF".
           DISPLAY "SR " FS.
           PERFORM VARYING W-I FROM 9 BY 1 UNTIL W-I > 12
               MOVE FUNCTION ORD(SR-BYTES(W-I:1)) TO W-O
               DISPLAY "S" W-I "=" W-O
           END-PERFORM.
           CLOSE SR.
           OPEN OUTPUT RW.
           MOVE N"CDEF" TO RW-REC.
           MOVE 1 TO RKW.
           WRITE RW-REC.
           DISPLAY "RW " FS.
           CLOSE RW.
           OPEN INPUT RR.
           MOVE 1 TO RK.
           READ RR.
           DISPLAY "RR " FS.
           PERFORM VARYING W-I FROM 9 BY 1 UNTIL W-I > 12
               MOVE FUNCTION ORD(RR-BYTES(W-I:1)) TO W-O
               DISPLAY "R" W-I "=" W-O
           END-PERFORM.
           CLOSE RR.
           OPEN OUTPUT VW.
           MOVE N"CDEF" TO VW-REC.
           MOVE 4 TO WL.
           WRITE VW-REC.
           DISPLAY "VW " FS.
           CLOSE VW.
           OPEN INPUT VR.
           MOVE N"ZZZZ" TO VR-REC.
           READ VR AT END DISPLAY "EOF".
           DISPLAY "VR " FS " RL=" RL.
           PERFORM VARYING W-I FROM 5 BY 1 UNTIL W-I > 8
               MOVE FUNCTION ORD(VR-BYTES(W-I:1)) TO W-O
               DISPLAY "V" W-I "=" W-O
           END-PERFORM.
           CLOSE VR.
           OPEN OUTPUT QW.
           MOVE N"CDEF" TO QW-REC.
           MOVE 4 TO WL.
           MOVE 1 TO RKW.
           WRITE QW-REC.
           DISPLAY "QW " FS.
           CLOSE QW.
           OPEN INPUT QR.
           MOVE N"ZZZZ" TO QR-REC.
           MOVE 1 TO RK.
           READ QR.
           DISPLAY "QR " FS " RL=" RL.
           PERFORM VARYING W-I FROM 5 BY 1 UNTIL W-I > 8
               MOVE FUNCTION ORD(QR-BYTES(W-I:1)) TO W-O
               DISPLAY "Q" W-I "=" W-O
           END-PERFORM.
           CLOSE QR.
           STOP RUN.
