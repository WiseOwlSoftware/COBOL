      *>--- conformance-case
      *> id: pb4677_mcs_receive
      *> rules: []
      *> accept: [85, 2002]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the RECEIVE and ACCEPT MESSAGE COUNT statements
      *> over this implementation's MCS (mcs.json beside this program).
      *> ISO/IEC 1989:2002 A.2.5.3 GR3, GR8, GR9: the text fills the area left
      *> to right and leaves the rest; A.1.2.3 GR12: end key 3, 2, 1 or 0;
      *> A.1.2.3 GR5: the levels are returned; E.5.5.4: an unnamed level is
      *> chosen alphabetically, a design determination; E.5.5: the count is of
      *> complete messages. Every leg reads a seeded message the store holds,
      *> so a wrong field position, count or end key changes a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W12 PIC X(12).
       01 W3  PIC X(3).
       01 W-G.
          02 W-E PIC X(4) OCCURS 2 TIMES.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SUB-QUEUE-1 IS S1
           SYMBOLIC SUB-QUEUE-2 IS S2
           SYMBOLIC SUB-QUEUE-3 IS S3
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC SOURCE IS SRC
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "TOP" TO Q.
           MOVE SPACES TO S1 S2 S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "COUNT TOP " MC " " SK.
           MOVE "BBB" TO S1.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT BBB " MC " " SK.
           MOVE "NOPE" TO S1.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT NOPE " MC " " SK.
           MOVE SPACES TO S1 W12.
           RECEIVE CIN MESSAGE INTO W12
               NO DATA DISPLAY "NODATA".
           DISPLAY "1 [" W12 "] " TL " " EK " " SK " " SRC.
           DISPLAY "  " MD " " MT " [" Q "] [" S1 "]".
           MOVE SPACES TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "2 [" W12 "] " TL " " EK.
           MOVE ALL "*" TO W12.
           RECEIVE CIN SEGMENT INTO W12.
           DISPLAY "3 [" W12 "] " TL " " EK " " SRC.
           MOVE SPACES TO W12.
           RECEIVE CIN MESSAGE INTO W12.
           DISPLAY "4 [" W12 "] " EK " [" S1 "] [" S2 "]".
           MOVE SPACES TO S1 S2 S3 W12.
           RECEIVE CIN MESSAGE INTO W12
               WITH DATA DISPLAY "WITHDATA"
           END-RECEIVE.
           DISPLAY "5 [" W12 "] [" S1 "]".
           MOVE SPACES TO S1 S2 S3.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT TOP " MC.
           MOVE 99 TO TL.
           RECEIVE CIN MESSAGE INTO W12
               NO DATA DISPLAY "NODATA"
               WITH DATA DISPLAY "WITHDATA".
           DISPLAY "6 " TL " " SK.
           MOVE "X1" TO S2.
           RECEIVE CIN MESSAGE INTO W12
               NO DATA DISPLAY "NODATA"
               WITH DATA DISPLAY "AS-IF-DATA".
           DISPLAY "7 " SK.
           MOVE SPACES TO S2.
           MOVE "SEGQ" TO Q.
           MOVE SPACES TO W3.
           RECEIVE CIN SEGMENT INTO W3.
           DISPLAY "8 [" W3 "] " TL " " EK.
           MOVE SPACES TO W3.
           RECEIVE CIN SEGMENT INTO W3.
           DISPLAY "8 [" W3 "] " TL " " EK.
           MOVE SPACES TO W3.
           RECEIVE CIN SEGMENT INTO W3.
           DISPLAY "8 [" W3 "] " TL " " EK.
           MOVE SPACES TO W3.
           RECEIVE CIN SEGMENT INTO W3.
           DISPLAY "8 [" W3 "] " TL " " EK.
           MOVE SPACES TO W3.
           RECEIVE CIN SEGMENT INTO W3.
           DISPLAY "8 [" W3 "] " TL " " EK.
           MOVE "RECV" TO Q.
           MOVE ALL "." TO W-G.
           RECEIVE CIN MESSAGE INTO W-E (2).
           DISPLAY "9 [" W-G "] " TL.
           MOVE ALL "." TO W-G.
           RECEIVE CIN MESSAGE INTO W-G (3:2).
           DISPLAY "9 [" W-G "] " TL.
           MOVE ALL "." TO W-G.
           RECEIVE CIN MESSAGE INTO W-G.
           DISPLAY "9 [" W-G "] " TL.
           STOP RUN.
