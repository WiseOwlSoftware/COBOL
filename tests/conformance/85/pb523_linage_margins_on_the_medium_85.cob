      *> ⛔ THE LINAGE TOP AND BOTTOM MARGINS ARE LINES OF THE LOGICAL
      *> PAGE, ON THE MEDIUM — the COBOL-85 half of the kb/Work PB523
      *> assertion. §13.18.34's LINAGE clause, its LINES AT TOP / LINES AT
      *> BOTTOM phrases and §14.9.51.4's ADVANCING rules are unchanged
      *> across the 1985, 2002, 2014 and 2023 editions, so the SAME
      *> physical layout is owed at every one; this fixture and its 2023
      *> twin (tests/conformance/2023/pb523_linage_margins_on_the_medium
      *> .cob) bracket the supported range.
      *>
      *> ⛔ WHY THIS TWIN READS BYTES WHERE THE 2023 ONE READS LINES. The
      *> 2023 fixture reads its print files back with ORGANIZATION IS LINE
      *> SEQUENTIAL, so each DISPLAYed index IS a physical line number.
      *> LINE SEQUENTIAL does not exist at COBOL-85 (it is the 2023
      *> ORGANIZATION phrase; this compiler rejects it with COBOLNET0900
      *> at --std 85), so the medium is read here one CHARACTER at a time
      *> through a record sequential file with a PIC X record — which
      *> observes the same bytes one level lower: the file's LENGTH and
      *> the OFFSET of each written record within it.
      *>
      *> THE RULES.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The logical
      *>   page size is the sum of the values referenced by each phrase
      *>   except the FOOTING phrase"
      *>   -> OK  §13.18.34.4 1)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Integer-3 or
      *>   the value of the data item referenced by data-name-3 specifies
      *>   the number of lines in the top margin on the logical page"
      *>   -> OK  §13.18.34.4 4)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Integer-4 or
      *>   the value of the data item referenced by data-name-4 specifies
      *>   the number of lines in the bottom margin on the logical page"
      *>   -> OK  §13.18.34.4 5)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Each logical
      *>   page is contiguous to the next with no additional spacing
      *>   provided."   -> OK  §13.18.34.4 8)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the device is
      *>   repositioned to the first line that may be written on the next
      *>   logical page"   -> OK  §14.9.51.4 26)  (General rules)
      *>
      *> THE LOGICAL PAGE. LINAGE IS 2 LINES LINES AT TOP 3 LINES AT
      *> BOTTOM 2. GR1 makes the logical page size the sum of every phrase
      *> but FOOTING — 3 + 2 + 2 = 7 lines — and GR8 lays the pages
      *> contiguously, so page 1 is physical lines 1-7 and page 2 begins
      *> at physical line 8. GR4 gives the top margin the first 3 lines of
      *> a page, GR2 the page body the next 2, GR5 the bottom margin the
      *> last 2; page-1 body lines 1-2 are physical lines 4-5 and page-2
      *> body lines 1-2 are physical lines 11-12. GR7 d) puts the device
      *> on body line 1 at OPEN OUTPUT.
      *>
      *> THE THREE WRITES. §14.9.51.4 GR25 f) presents the line after the
      *> advance and §13.18.34.4 GR7 c) 2 adds the advance to the counter:
      *>   W1: counter 1 -> 2, AAAA on page-1 body line 2  = line 5.
      *>   W2: the counter would reach 3, past the page size — §14.9.51.4
      *>       GR26 a)'s page overflow — and with the AFTER phrase "the
      *>       device is repositioned to the first line that may be
      *>       written on the next logical page and the logical record is
      *>       presented on that line": page-2 body line 1 = line 11.
      *>       GR7 c) 4 resets the counter to one.
      *>   W3: counter 1 -> 2, CCCC on page-2 body line 2 = line 12.
      *>
      *> THE BYTES THAT FOLLOWS FROM. Every line the device travels is a
      *> physical newline (CR LF on this implementation's print medium, 2
      *> characters), and a written record contributes its 4 characters:
      *>   lines 1-4 travelled blank          8 characters   (offsets 1-8)
      *>   AAAA                               4               (9-12)
      *>   lines 5-10 travelled blank        12              (13-24)
      *>   BBBB                               4              (25-28)
      *>   line 11 travelled blank            2              (29-30)
      *>   CCCC                               4              (31-34)
      *>   line 12 terminated at CLOSE        2              (35-36)
      *> so LEN=0036, A@=0009, B@=0025, C@=0031. A compiler that keeps the
      *> margins in the LINAGE-COUNTER but not on the medium writes the 5
      *> margin lines between AAAA and BBBB as ONE, and produces LEN=0020,
      *> A@=0003, B@=0009, C@=0015 — every one of the four values differs,
      *> so no single arithmetic accident can make this fixture pass.
      *>
      *> ⛔ WHY EACH WRITE IS PRECEDED BY ITS OWN MOVE. §14.9.51.4 GR4 —
      *>   python scripts/spec/cite.py --check 14.9.51.4 "The logical
      *>   record released by the successful execution of the WRITE
      *>   statement is no longer available in the record area unless the
      *>   file-name associated with record-name-1 is specified in a SAME
      *>   RECORD AREA clause."   -> OK  §14.9.51.4 4)  (General rules)
      *> PRTA is not named in a SAME RECORD AREA clause, so the record
      *> area is unavailable after each WRITE and a second WRITE of the
      *> same item would release bytes the standard does not define.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB523L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTA ASSIGN TO "pb523l-a.prt".
           SELECT RDA ASSIGN TO "pb523l-a.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTA LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2.
       01 A-REC PIC X(4).
       FD RDA.
       01 R-CHAR PIC X.
       WORKING-STORAGE SECTION.
       01 POSN PIC 9(4) VALUE 0.
       01 EOF-SW PIC 9 VALUE 0.
       01 AT-A PIC 9(4) VALUE 0.
       01 AT-B PIC 9(4) VALUE 0.
       01 AT-C PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTA.
           MOVE "AAAA" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           MOVE "BBBB" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           MOVE "CCCC" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           CLOSE PRTA.
           OPEN INPUT RDA.
           PERFORM UNTIL EOF-SW = 1
               READ RDA
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       ADD 1 TO POSN
                       IF R-CHAR = "A" AND AT-A = 0
                           MOVE POSN TO AT-A
                       END-IF
                       IF R-CHAR = "B" AND AT-B = 0
                           MOVE POSN TO AT-B
                       END-IF
                       IF R-CHAR = "C" AND AT-C = 0
                           MOVE POSN TO AT-C
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDA.
           DISPLAY "LEN=" POSN.
           DISPLAY "A@=" AT-A.
           DISPLAY "B@=" AT-B.
           DISPLAY "C@=" AT-C.
           STOP RUN.
