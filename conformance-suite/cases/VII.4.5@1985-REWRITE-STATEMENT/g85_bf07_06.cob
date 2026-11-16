      *>--- conformance-case
      *> id: g85_bf07_06
      *> rules: [1985-GR-VII.4.5.4-3, 1985-GR-VII.4.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR3 - "The logical record released by a
      *> successful execution of the REWRITE statement is no longer
      *> available in the record area unless the file-name associated
      *> with record-name-1 is specified in a SAME RECORD AREA clause.
      *> The logical record is also available to the program as a
      *> record of other files referenced in the same SAME RECORD AREA
      *> clause as the associated I-O file, as well as the file
      *> associated with record-name-1."
      *> §1985:VII.4.5.4 GR5 - "After the execution of the REWRITE
      *> statement is complete, the information in the area referenced
      *> by identifier-1 is available, even though the information in
      *> the area referenced by record-name-1 is not available except
      *> as specified by the SAME RECORD AREA clause."
      *> SQ-A (sequential, mass storage, open I-O) and SQ-B share a
      *> SAME RECORD AREA. SQ-A holds OLDVAL01; READ it, then
      *> REWRITE A-REC FROM WS-NEW (NEWVAL01): by GR4 that is
      *> MOVE WS-NEW TO A-REC and the REWRITE.
      *> ST  - the REWRITE succeeds: status 00.
      *> RWA - A-REC still holds NEWVAL01 (GR3, first sentence).
      *> RWB - SQ-B's record (two 4-character fields) holds NEWV/AL01,
      *>   the released record as a record of the other file (GR3).
      *> FRM - WS-NEW still holds NEWVAL01 (GR5).
      *> FIL - SQ-A read back holds NEWVAL01 (the record was replaced).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0706.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-A ASSIGN TO "BF0706A"
               FILE STATUS IS ST-A.
           SELECT SQ-B ASSIGN TO "BF0706B".
       I-O-CONTROL.
           SAME RECORD AREA FOR SQ-A SQ-B.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-A.
       01  A-REC PIC X(8).
       FD  SQ-B.
       01  B-REC.
           05  B-1 PIC X(4).
           05  B-2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  WS-NEW PIC X(8) VALUE "NEWVAL01".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SQ-A.
           MOVE "OLDVAL01" TO A-REC.
           WRITE A-REC.
           CLOSE SQ-A.
           OPEN I-O SQ-A.
           OPEN OUTPUT SQ-B.
           READ SQ-A AT END DISPLAY "EOF".
           REWRITE A-REC FROM WS-NEW.
           DISPLAY "ST=" ST-A.
           DISPLAY "RWA=" A-REC.
           DISPLAY "RWB=" B-1 "/" B-2.
           DISPLAY "FRM=" WS-NEW.
           CLOSE SQ-A SQ-B.
           OPEN INPUT SQ-A.
           READ SQ-A AT END DISPLAY "EOF".
           DISPLAY "FIL=" A-REC.
           CLOSE SQ-A.
           STOP RUN.
