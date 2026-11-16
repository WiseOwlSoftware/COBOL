      *>--- conformance-case
      *> id: g85_bf23_01
      *> rules: [1985-GR-VII.3.8.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR7 - "If integer-3 is not specified, the
      *> maximum number of character positions to be contained in any
      *> record of the file is equal to the greatest number of character
      *> positions described for a record in that file."
      * FV is RECORD IS VARYING IN SIZE DEPENDING ON LEN with no
      * integer-3; its record descriptions are 8 (FIRST), 12 (SECOND)
      * and 10 (THIRD) character positions, so GR7 makes the maximum
      * 12 - the greatest description, which is neither the first (8),
      * nor the last (10), nor the record-name of every write.
      * DERIVATION (each citation run through scripts/spec/cite.py):
      *  - §1985:VII.3.8.4 GR10a: WRITE takes its size from LEN.
      *  - LEN=12 is within the maximum: W12=00 (§1985:VII.1.3.5 "The
      *    input-output statement is successfully executed and no
      *    further information is available").
      *  - LEN=13 exceeds the maximum: §1985:VII.4.7.4 GR8 "the
      *    execution of the WRITE statement is unsuccessful, the WRITE
      *    operation does not take place", status 44 (§1985:VII.1.3.5
      *    "A boundary violation exists because"): W13=44.
      *  - LEN=10 and LEN=8 are in range (8 is the least description,
      *    so GR6's minimum is not crossed): W10=00, W08=00.
      *  - The lines after W13=44 rest on the implementor's choice:
      *    44 is critical and "the implementor determines what action
      *    is taken" (§1985:VII.1.3.5); WiseOwl continues when the
      *    file has a FILE STATUS clause (docs/conformance/7/
      *    DOC-A.1-103.md).
      *  - WRITE V-FIRST with LEN=12: GR10a sizes the record by LEN,
      *    not by V-FIRST's own 8; 12 is within GR7's maximum, so
      *    §1985:VII.4.7.4 GR8 is not violated: WF12=00. The area is
      *    set after the previous WRITE (§1985:VII.4.7.4 GR2 leaves it
      *    unavailable); the 01 entries are implicit redefinitions of
      *    one area (§1985:VI.5.3.4 GR3), so its 12 positions hold
      *    abcdefghIJKL.
      *  - Read back, §1985:VII.3.8.4 GR11 sets LEN to each record's
      *    size: 12, 10, 8, 12, then AT END - the 13-position record
      *    was never written.
      * HOW EACH LEG FAILS: a maximum taken from the first description
      * (8) turns W12, W10 and WF12 into 44; one taken from the last
      * description (10) or from the written record-name's own size
      * (8 for V-FIRST) turns WF12 into 44 and drops its record; no
      * upper bound at all turns W13 into 00 and adds a record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2301.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FV ASSIGN TO "g85bf2301.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-V.
       DATA DIVISION.
       FILE SECTION.
       FD  FV
           RECORD IS VARYING IN SIZE DEPENDING ON LEN.
       01  V-FIRST  PIC X(8).
       01  V-SECOND PIC X(12).
       01  V-THIRD  PIC X(10).
       WORKING-STORAGE SECTION.
       01  LEN  PIC 99.
       01  ST-V PIC XX.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FV.
           MOVE "ABCDEFGHIJKL" TO V-SECOND.
           MOVE 12 TO LEN.
           WRITE V-SECOND.
           DISPLAY "W12=" ST-V.
           MOVE "MNOPQRSTUVWX" TO V-SECOND.
           MOVE 13 TO LEN.
           WRITE V-SECOND.
           DISPLAY "W13=" ST-V.
           MOVE "0123456789XX" TO V-SECOND.
           MOVE 10 TO LEN.
           WRITE V-SECOND.
           DISPLAY "W10=" ST-V.
           MOVE "abcdefgh" TO V-FIRST.
           MOVE 8 TO LEN.
           WRITE V-FIRST.
           DISPLAY "W08=" ST-V.
           MOVE "ABCDEFGHIJKL" TO V-SECOND.
           MOVE "abcdefgh" TO V-FIRST.
           MOVE 12 TO LEN.
           WRITE V-FIRST.
           DISPLAY "WF12=" ST-V.
           CLOSE FV.
           OPEN INPUT FV.
           PERFORM READ-ONE 5 TIMES.
           CLOSE FV.
           STOP RUN.
       READ-ONE.
           IF EOF-SW = "N"
               READ FV
                   AT END MOVE "Y" TO EOF-SW
                          DISPLAY "END-OF-FILE"
               END-READ
               IF EOF-SW = "N"
                   DISPLAY "R" LEN "=" V-SECOND (1:LEN)
               END-IF
           END-IF.
