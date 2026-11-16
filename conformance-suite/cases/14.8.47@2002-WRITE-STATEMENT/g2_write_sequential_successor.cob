      *>--- conformance-case
      *> id: g2_write_sequential_successor
      *> rules: [2002-GR-14.8.47.3-16, 2002-GR-14.8.47.3-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR16 - "The successor relationship of a
      *> sequential file is established by the order of execution of
      *> WRITE statements when the physical file is created. The
      *> relationship does not change except when records are added to
      *> the end of a physical file." The file is created by writing
      *> C1, A2, B3 (not in collating order): reading it back yields
      *> exactly that order (lines CRE-n).
      *> §2002:14.8.47.3 GR17 - with open mode extend the WRITE adds
      *> records to the end "as though the open mode of the file
      *> connector were output. If there are records in the physical
      *> file, the first record written after the execution of the OPEN
      *> statement with the EXTEND phrase is the successor of the last
      *> record in the physical file." Z4 and Y5 written after OPEN
      *> EXTEND follow B3, in write order, and the three original
      *> records keep their order (lines EXT-n, COUNT=5). A leg fails if
      *> EXTEND overwrites (COUNT=2), prepends, or reorders.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g2wr03.tmp" ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SR PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 N  PIC 9 VALUE 0.
       01 EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SF
           WRITE SR FROM "C1"
           WRITE SR FROM "A2"
           WRITE SR FROM "B3"
           CLOSE SF
           OPEN INPUT SF
           MOVE "N" TO EOF-SW MOVE 0 TO N
           PERFORM UNTIL EOF-SW = "Y"
               READ SF
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END ADD 1 TO N DISPLAY "CRE-" N "=" SR
               END-READ
           END-PERFORM
           CLOSE SF
           OPEN EXTEND SF
           WRITE SR FROM "Z4"
           DISPLAY "EXT-WRITE " FS
           WRITE SR FROM "Y5"
           CLOSE SF
           OPEN INPUT SF
           MOVE "N" TO EOF-SW MOVE 0 TO N
           PERFORM UNTIL EOF-SW = "Y"
               READ SF
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END ADD 1 TO N DISPLAY "EXT-" N "=" SR
               END-READ
           END-PERFORM
           CLOSE SF
           DISPLAY "COUNT=" N
           STOP RUN.
