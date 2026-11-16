      *>--- conformance-case
      *> id: g85_bf13_02
      *> rules: [1985-GR-VI.5.14.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR7 - a USAGE IS INDEX item holds a value that
      *> corresponds to an occurrence number of a table element: an
      *> index-name's position saved in IDX-1 and restored later selects
      *> the same element, an index data item copied to another keeps
      *> it, and the two compare equal. The representation itself is the
      *> implementor's and is never shown (§1985:VI.5.14.3 SR5 confines
      *> explicit references to SET, SEARCH and relation conditions).
      *> A leg fails if the saved value does not map back to occurrence
      *> 4 (DDD, 4) or 2 (BBB), or if a copy compares unequal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1302.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TAB-V               PIC X(15) VALUE "AAABBBCCCDDDEEE".
       01  TAB REDEFINES TAB-V.
           05  T-E             PIC X(3) OCCURS 5 TIMES INDEXED BY TX.
       01  IDX-1               USAGE IS INDEX.
       01  IDX-2               INDEX.
       01  W-N                 PIC 9.
       PROCEDURE DIVISION.
       P-MAIN.
           SET TX TO 4.
           SET IDX-1 TO TX.
           SET TX TO 1.
           SET TX TO IDX-1.
           DISPLAY T-E (TX).
           SET IDX-2 TO IDX-1.
           IF IDX-2 = IDX-1
               DISPLAY "EQUAL"
           ELSE
               DISPLAY "UNEQUAL".
           SET TX TO 1.
           SET TX TO IDX-2.
           SET W-N TO TX.
           DISPLAY W-N.
           SET TX DOWN BY 2.
           SET IDX-1 TO TX.
           SET TX TO 5.
           SET TX TO IDX-1.
           DISPLAY T-E (TX).
           STOP RUN.
