      *>--- conformance-case
      *> id: g85_bf09_09
      *> rules: [1985-GR-XIII.3.19.4-3, 1985-GR-XIII.3.19.4-8,
      *>    1985-GR-XIII.3.19.4-10, 1985-GR-XIII.3.19.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR3 - one sum counter per entry however many
      *> SUM clauses it has (S3: two SUM clauses, each with its own
      *> UPON).
      *> §1985:XIII.3.19.4 GR8 - UPON subtotals only on the named
      *> details (S8 UPON D1; S3's clauses).
      *> §1985:XIII.3.19.4 GR10 - an addend named twice is added twice,
      *> and a detail named twice in UPON increments twice (S10: 4 x
      *> WS-A per D1).
      *> §1985:XIII.3.19.4 GR11 - INITIATE sets the counters to zero;
      *> without RESET a counter is reset when its own control footing
      *> is processed (S3 S8 S10 S11B); RESET ON FINAL resets it at the
      *> FINAL level (S11).
      *>   cite.py: OK  §1985:XIII.3.19.4 GR3 "Only one sum counter
      *>     exists for an elementary report entry"
      *>   cite.py: OK  §1985:XIII.3.19.4 GR8 "The UPON phrase provides
      *>     the capability to accomplish selective subtotalling"
      *>   cite.py: OK  §1985:XIII.3.19.4 GR10 "the incrementing occurs
      *>     as many times as the data-name appears in the UPON phrase"
      *>   cite.py: OK  §1985:XIII.3.19.4 GR11 "Sum counters are
      *>     initially set to zero by the report writer control system
      *>     during the execution of the INITIATE statement"
      *>   (GR10 and GR11 are text the transcription reconstructs,
      *>     kb/Work PB4952; the printed §1985:XIII.4.4.4 GR1 "All sum
      *>     counters are set to zero" and §1985:XIII.3.20.4 GR10 a-f
      *>     state the same times.)
      *>   cite.py: OK  §1985:XIII.3.19.4 GR7 "Subtotalling is
      *>     accomplished only during execution of GENERATE statements
      *>     and after any control break processing"
      *> DERIVATION. Every counter is set to 500 before INITIATE;
      *> INITIATE zeroes them (GR11), so no 5nn appears. GENERATEs (key,
      *> detail, A, B): (1,D1,1) (1,D2,2,10) (1,D1,3) (2,D2,4,20)
      *> (2,D1,5). Key group 1: S3 = 1 + 10 + 3 = 14 (A on D1, B on D2,
      *> one counter: GR3); S8 = 1 + 3 = 4 (GR8); S10 = 4 x (1 + 3) = 16
      *> (GR10); S11 = S11B = 1 + 2 + 3 = 6. The 4th GENERATE breaks:
      *> CF-K's USE (§1985:XIII.3.20.4 GR10 c) shows 014 004 016 006
      *> 006, then S3 S8 S10 S11B reset and S11 does not (GR11). Its
      *> subtotalling follows the break (GR7): S3 = 20, S11 = 10, S11B =
      *> 4; the 5th adds A 5 on D1: S3 25, S8 5, S10 20, S11 15, S11B 9.
      *> TERMINATE processes CF-K (025 005 020 015 009) and then CF-F,
      *> whose USE shows SF = 15 and S11 = 15 (still not reset); the
      *> FINAL level then resets S11 and SF, and CF-K's level reset
      *> S11B: END 000 000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0909.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0909.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9     VALUE 1.
       01  WS-A    PIC 9     VALUE 0.
       01  WS-B    PIC 99    VALUE 0.
       01  WS-O1   PIC 999.
       01  WS-O2   PIC 999.
       01  WS-O3   PIC 999.
       01  WS-O4   PIC 999.
       01  WS-O5   PIC 999.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE FINAL WS-KEY.
       01  D1 TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-A.
       01  D2 TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC 99 SOURCE WS-B.
       01  CF-K TYPE CF WS-KEY LINE PLUS 1.
           05  S3 COLUMN 1 PIC 999 SUM WS-A UPON D1 SUM WS-B UPON D2.
           05  S8 COLUMN 5 PIC 999 SUM WS-A UPON D1.
           05  S10 COLUMN 9 PIC 999 SUM WS-A WS-A UPON D1 D1.
           05  S11 COLUMN 13 PIC 999 SUM WS-A RESET ON FINAL.
           05  S11B COLUMN 17 PIC 999 SUM WS-A.
       01  CF-F TYPE CF FINAL LINE PLUS 1.
           05  SF COLUMN 1 PIC 999 SUM WS-A.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CFK-SEC SECTION.
           USE BEFORE REPORTING CF-K.
       CFK-PARA.
           MOVE S3 TO WS-O1.
           MOVE S8 TO WS-O2.
           MOVE S10 TO WS-O3.
           MOVE S11 TO WS-O4.
           MOVE S11B TO WS-O5.
           DISPLAY "CF-K " WS-O1 " " WS-O2 " " WS-O3 " " WS-O4 " "
               WS-O5.
       CFF-SEC SECTION.
           USE BEFORE REPORTING CF-F.
       CFF-PARA.
           MOVE SF TO WS-O1.
           MOVE S11 TO WS-O4.
           DISPLAY "CF-F " WS-O1 " " WS-O4.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           MOVE 500 TO S3 S8 S10 S11 S11B SF.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE 1 TO WS-KEY MOVE 1 TO WS-A GENERATE D1.
           MOVE 1 TO WS-KEY MOVE 2 TO WS-A MOVE 10 TO WS-B GENERATE D2.
           MOVE 1 TO WS-KEY MOVE 3 TO WS-A GENERATE D1.
           MOVE 2 TO WS-KEY MOVE 4 TO WS-A MOVE 20 TO WS-B GENERATE D2.
           MOVE 2 TO WS-KEY MOVE 5 TO WS-A GENERATE D1.
           TERMINATE R-1.
           CLOSE PRT.
           MOVE S11 TO WS-O4.
           MOVE S11B TO WS-O5.
           DISPLAY "END " WS-O4 " " WS-O5.
           STOP RUN.
