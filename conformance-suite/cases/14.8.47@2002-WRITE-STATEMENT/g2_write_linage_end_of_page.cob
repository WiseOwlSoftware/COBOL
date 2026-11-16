      *>--- conformance-case
      *> id: g2_write_linage_end_of_page
      *> rules: [2002-GR-14.8.47.3-22]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR22 - the end-of-page condition of a WRITE to
      *>     a
      *>   LINAGE file: a) page overflow, with AFTER specified, AFTER
      *>   implied and BEFORE, each repositioning to the first line of
      *>   the next logical page; b) printing or spacing in the footing
      *>   area. Each cite.py --check OK:
      *>   §2002:14.8.47.3 GR22 "an end-of-page condition occurs when
      *>     the
      *>     lines written by a WRITE statement do not fit within the
      *>     current page body"
      *>   §2002:14.8.47.3 GR22a "If the BEFORE phrase is specified, the
      *>     logical record is presented and the device is repositioned"
      *>   §2002:14.8.47.3 GR22b "causes printing or spacing within the
      *>     footing area of a page body"
      *>   §2002:14.8.47.3 GR21 "automatic advancing shall be provided
      *>     by
      *>     the implementor to act as if the user has specified AFTER
      *>     ADVANCING 1 LINE"
      *>   §2002:13.16.32.3 GR3 "The footing area is the area of the
      *>     page
      *>     body between the footing start and the page size,
      *>     inclusive"
      *>   §2002:13.16.32.3 GR7 "is automatically set to one at the
      *>     time an
      *>     OPEN statement with the OUTPUT phrase is executed" and "is
      *>     automatically reset to one when the device is repositioned
      *>     to
      *>     the first line"; GR7c "the LINAGE-COUNTER is incremented by
      *>     the value one" (no ADVANCING phrase), and by the ADVANCING
      *>     integer otherwise.
      *> DERIVATION (LC = LINAGE-COUNTER after the WRITE; every WRITE
      *>     has
      *> both AT and NOT AT END-OF-PAGE; NOT = the NOT AT imperative
      *>     ran).
      *> No write lands on the line equal
      *> to the page size, so the PB686 boundary determination is not
      *> reached: every overflow below spaces past the page size.
      *>   FA: LINAGE 3, no footing (GR1: overflow is its only EOP).
      *>     OPEN                      -> FA OPEN LC=001
      *>     A1 AFTER 1 (line 2 fits)  -> A1 NOT LC=002
      *>     A2 AFTER 5 (line 7 > 3): a), AFTER specified, record on the
      *>        first line of the next page -> A2 EOP LC=001
      *>     A3 no ADVANCING = AFTER 1 -> A3 NOT LC=002
      *>     A4 BEFORE 4: presented on line 2, then 6 > 3: a), BEFORE,
      *>        repositioned to the next page   -> A4 EOP LC=001
      *>   FC: LINAGE 1. C1 no ADVANCING (implied AFTER 1) from line 1:
      *>     line 2 > 1, a) with AFTER implied  -> C1 EOP LC=001
      *>   FB: LINAGE 5 FOOTING 3 (footing area lines 3..5).
      *>     B1 AFTER 1 -> line 2, before the footing -> B1 NOT LC=002
      *>     B2 AFTER 1 -> line 3, in the footing: b), no repositioning
      *>                                      -> B2 EOP LC=003
      *>     B3 AFTER 1 -> line 4, b)          -> B3 EOP LC=004
      *>     B4 AFTER 3 -> line 7 > 5, a)      -> B4 EOP LC=001
      *>     B5 BEFORE 2 from line 1: spacing to line 3, within the
      *>        footing area, b)               -> B5 EOP LC=003
      *> Each leg fails if EOP is raised off the page body, missed on
      *> overflow or in the footing, or the counter is not reset to one
      *>     by
      *> the reposition of a).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WEOP01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g2weopa.prt".
           SELECT FB ASSIGN TO "g2weopb.prt".
           SELECT FC ASSIGN TO "g2weopc.prt".
       DATA DIVISION.
       FILE SECTION.
       FD FA LINAGE IS 3 LINES.
       01 A-REC PIC X(4).
       FD FB LINAGE IS 5 LINES WITH FOOTING AT 3.
       01 B-REC PIC X(4).
       FD FC LINAGE IS 1 LINES.
       01 C-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 LC PIC 9(3).
       01 EV PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT FA FB FC
           MOVE LINAGE-COUNTER OF FA TO LC
           DISPLAY "FA OPEN LC=" LC
           MOVE "A1A1" TO A-REC
           WRITE A-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FA TO LC
           DISPLAY "A1 " EV " LC=" LC
           MOVE "A2A2" TO A-REC
           WRITE A-REC AFTER ADVANCING 5 LINES
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FA TO LC
           DISPLAY "A2 " EV " LC=" LC
           MOVE "A3A3" TO A-REC
           WRITE A-REC
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FA TO LC
           DISPLAY "A3 " EV " LC=" LC
           MOVE "A4A4" TO A-REC
           WRITE A-REC BEFORE ADVANCING 4 LINES
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FA TO LC
           DISPLAY "A4 " EV " LC=" LC
           MOVE "C1C1" TO C-REC
           WRITE C-REC
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FC TO LC
           DISPLAY "C1 " EV " LC=" LC
           MOVE "B1B1" TO B-REC
           WRITE B-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FB TO LC
           DISPLAY "B1 " EV " LC=" LC
           MOVE "B2B2" TO B-REC
           WRITE B-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FB TO LC
           DISPLAY "B2 " EV " LC=" LC
           MOVE "B3B3" TO B-REC
           WRITE B-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FB TO LC
           DISPLAY "B3 " EV " LC=" LC
           MOVE "B4B4" TO B-REC
           WRITE B-REC AFTER ADVANCING 3 LINES
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FB TO LC
           DISPLAY "B4 " EV " LC=" LC
           MOVE "B5B5" TO B-REC
           WRITE B-REC BEFORE ADVANCING 2 LINES
               AT END-OF-PAGE MOVE "EOP" TO EV
               NOT AT END-OF-PAGE MOVE "NOT" TO EV
           END-WRITE
           MOVE LINAGE-COUNTER OF FB TO LC
           DISPLAY "B5 " EV " LC=" LC
           CLOSE FA FB FC
           STOP RUN.
