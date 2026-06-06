      *> ISO §13.18.60.4 / §8.3.3.5 / §14.6.8.5 / Table 16 — COBOL-2002 NATIONAL data.
      *> National items (USAGE NATIONAL / PIC N(n)) are UTF-16: two bytes per character position.
      *> Covers N"…" literals; MOVE national←national (left-justify, national-space pad, right
      *> truncate); national↔alphanumeric and numeric→national conversion (Latin-1 subset); literal
      *> and figurative VALUE; MOVE SPACE and INITIALIZE (national-space fill); and national
      *> comparison. (NATIONAL-EDITED, NX"…", non-Latin-1 correspondence, and collating-sequence
      *> national compare are deferred.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NATIONALDATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-NAME   PIC N(5).
       01 N-CODE   USAGE NATIONAL PIC N(3).
       01 N-WORK   PIC N(3).
       01 A-WORK   PIC X(5).
       01 NUM      PIC 9(3) VALUE 42.
       01 N-VAL    PIC N(4) VALUE N"OK".
       PROCEDURE DIVISION.
       MAIN.
      *> Exact fit: a 5-character national literal into PIC N(5).
           MOVE N"HELLO" TO N-NAME.
           DISPLAY "NAME=" N-NAME.
      *> Right truncation: 7 characters into 3 national positions. Correct 2-bytes-per-character
      *> sizing keeps "ABC"; a 1-byte layout would keep only "A".
           MOVE N"ABCDEFG" TO N-WORK.
           DISPLAY "TRUNC=" N-WORK.
      *> National-space padding: a 3-character value then a 1-character value must blank positions
      *> 2 and 3, so the result is "P" alone, never "WYZ".
           MOVE N"WYZ" TO N-WORK.
           DISPLAY "FILL=" N-WORK.
           MOVE N"P" TO N-WORK.
           DISPLAY "PAD=" N-WORK.
      *> Field-to-field national MOVE with right truncation (5 -> 3 positions).
           MOVE N-NAME TO N-CODE.
           DISPLAY "CODE=" N-CODE.
      *> VALUE clause holding a national literal (UTF-16-encoded at startup).
           DISPLAY "VAL=" N-VAL.
      *> Alphanumeric field -> national receiver (Latin-1 widening).
           MOVE "XY" TO A-WORK.
           MOVE A-WORK TO N-WORK.
           DISPLAY "A2N=" N-WORK.
      *> National field -> alphanumeric receiver (Latin-1 narrowing).
           MOVE N"GHI" TO N-WORK.
           MOVE N-WORK TO A-WORK.
           DISPLAY "N2A=" A-WORK.
      *> Numeric field -> national receiver (display digits, widened).
           MOVE NUM TO N-WORK.
           DISPLAY "NUM=" N-WORK.
      *> MOVE SPACE then refill: national-space fill clears positions 2..3, so the result is "Q".
           MOVE N"GHI" TO N-WORK.
           MOVE SPACE TO N-WORK.
           MOVE N"Q" TO N-WORK.
           DISPLAY "SPC=" N-WORK.
      *> INITIALIZE a national item (national-space fill), then a 1-char refill -> "R".
           MOVE N"GHI" TO N-WORK.
           INITIALIZE N-WORK.
           MOVE N"R" TO N-WORK.
           DISPLAY "INIT=" N-WORK.
      *> National comparison: equality against a literal, and character ordering.
           MOVE N"ABC" TO N-WORK.
           IF N-WORK = N"ABC" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
           IF N-WORK < N"ABD" THEN DISPLAY "LT=YES" ELSE DISPLAY "LT=NO".
           STOP RUN.
