      *>--- conformance-case
      *> id: pb2509_record_sequential_print_stream_form
      *> rules: [GR-14.9.51.4-25, GR-13.18.43.4-6,
      *>    DOC-A.1-146, DOC-A.1-151]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2509 -- the FORM A RECORD HAS ON THE MEDIUM when it is written
      *> to a RECORD SEQUENTIAL file with an ADVANCING phrase (no LINAGE clause,
      *> not a report file).
      *>
      *> RULES (each run through scripts/spec/cite.py --check) --
      *>   9.1.7.2         "In record sequential files the length of each record
      *>                   is determined by any information the implementor may
      *>                   add to the record on the physical storage medium" --
      *>                   the on-medium form is the implementor's, so CLAUDE.md
      *>                   rule 1's precedence takes GnuCOBOL's.
      *>   14.9.51.4 GR21  the trailing-space strip is stated for "a line
      *>                   sequential file" alone: a record sequential record is
      *>                   not stripped.
      *>   13.18.43.4 GR6  "Integer-1 specifies the number of bytes contained in
      *>                   each record in the file" -- an FD with no RECORD
      *>                   clause is the implied Format 1 of its 8-byte record.
      *>   docs/CONFORMANCE.md DOC-A.1-151 (a): a variable-length record
      *>                   sequential record is framed by a 4-byte little-endian
      *>                   length prefix.
      *>
      *> DERIVATION (the print controls are 14.9.51.4 GR25: AFTER ADVANCING 1
      *> LINE puts a line feed before the record, and CLOSE ends the open line) --
      *>   FIXED   LF, "AA" + 6 spaces, LF, "BB" + 6 spaces, LF -- 19 bytes:
      *>           010 065 065 032 032 032 032 032 032 010
      *>           066 066 032 032 032 032 032 032 010
      *>   VARYING LF, length word 3 (03 00 00 00), "AAA", LF, length word 2
      *>           (02 00 00 00), "BB", LF -- 16 bytes:
      *>           010 003 000 000 000 065 065 065 010
      *>           002 000 000 000 066 066 010
      *>   Each file is read back one byte per record and every byte shown as
      *>   its value (000-255), so the stream's exact bytes are the expected
      *>   output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2509FM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFIX ASSIGN TO "pb2509-fixed.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-PFIX.
           SELECT PVAR ASSIGN TO "pb2509-varying.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-PVAR.
           SELECT BFIX ASSIGN TO "pb2509-fixed.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-BFIX.
           SELECT BVAR ASSIGN TO "pb2509-varying.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-BVAR.
       DATA DIVISION.
       FILE SECTION.
       FD  PFIX.
       01  PFIX-REC PIC X(8).
       FD  PVAR RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS
               DEPENDING ON PVAR-LEN.
       01  PVAR-REC PIC X(8).
       FD  BFIX.
       01  BFIX-REC PIC X.
       FD  BVAR.
       01  BVAR-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS-PFIX PIC XX.
       01  FS-PVAR PIC XX.
       01  FS-BFIX PIC XX.
       01  FS-BVAR PIC XX.
       01  PVAR-LEN PIC 9(4) COMP.
       01  BYTE-X PIC X.
       01  BYTE-N REDEFINES BYTE-X BINARY-CHAR UNSIGNED.
       01  BYTE-SHOW PIC 999.
       01  TAB-FIX.
           05  FIX-V OCCURS 19 PIC 999.
       01  TAB-VAR.
           05  VAR-V OCCURS 16 PIC 999.
       01  COUNT-FIX PIC 99 VALUE 0.
       01  COUNT-VAR PIC 99 VALUE 0.
       01  MORE-SW PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PFIX
           MOVE "AA" TO PFIX-REC
           WRITE PFIX-REC AFTER ADVANCING 1 LINE
           MOVE "BB" TO PFIX-REC
           WRITE PFIX-REC AFTER ADVANCING 1 LINE
           CLOSE PFIX
           DISPLAY "FIXED-WRITE=" FS-PFIX
           OPEN OUTPUT PVAR
           MOVE "AAA" TO PVAR-REC
           MOVE 3 TO PVAR-LEN
           WRITE PVAR-REC AFTER ADVANCING 1 LINE
           MOVE "BB" TO PVAR-REC
           MOVE 2 TO PVAR-LEN
           WRITE PVAR-REC AFTER ADVANCING 1 LINE
           CLOSE PVAR
           DISPLAY "VARYING-WRITE=" FS-PVAR
           OPEN INPUT BFIX
           MOVE "Y" TO MORE-SW
           PERFORM UNTIL MORE-SW = "N"
               READ BFIX
                   AT END MOVE "N" TO MORE-SW
                   NOT AT END
                       ADD 1 TO COUNT-FIX
                       MOVE BFIX-REC TO BYTE-X
                       MOVE BYTE-N TO BYTE-SHOW
                       IF COUNT-FIX NOT > 19
                           MOVE BYTE-SHOW TO FIX-V (COUNT-FIX)
                       END-IF
               END-READ
           END-PERFORM
           CLOSE BFIX
           DISPLAY "FIXED-BYTES=" COUNT-FIX
           DISPLAY "FIXED=" TAB-FIX
           OPEN INPUT BVAR
           MOVE "Y" TO MORE-SW
           PERFORM UNTIL MORE-SW = "N"
               READ BVAR
                   AT END MOVE "N" TO MORE-SW
                   NOT AT END
                       ADD 1 TO COUNT-VAR
                       MOVE BVAR-REC TO BYTE-X
                       MOVE BYTE-N TO BYTE-SHOW
                       IF COUNT-VAR NOT > 16
                           MOVE BYTE-SHOW TO VAR-V (COUNT-VAR)
                       END-IF
               END-READ
           END-PERFORM
           CLOSE BVAR
           DISPLAY "VARYING-BYTES=" COUNT-VAR
           DISPLAY "VARYING=" TAB-VAR
           STOP RUN.
