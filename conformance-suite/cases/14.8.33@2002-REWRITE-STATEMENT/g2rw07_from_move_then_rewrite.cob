      *>--- conformance-case
      *> id: g2rw07_from_move_then_rewrite
      *> rules: [2002-GR-14.8.33.3-6, 2002-GR-14.8.33.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.3 GR6 - REWRITE record-name-1 FROM is "MOVE
      *>   identifier-1 TO record-name-1" or "MOVE literal-1 TO
      *>   record-name-1" "according to the rules specified for the
      *>   MOVE statement", then "The same REWRITE statement without
      *>   the FROM phrase." (cite.py: OK). Each leg rewrites the one
      *>   record of its file, closes it, reopens it INPUT, READs it
      *>   and displays the record BEFORE the CLOSE: §2002:14.8.33.3
      *>   GR5 releases the record from the area at REWRITE and
      *>   §2002:14.8.6.3 GR7 makes the area unavailable after CLOSE
      *>   (no SAME RECORD AREA here), so only a just-READ record is
      *>   defined content. §2002:14.8.33.3 GR16 ('44' when the size
      *>   differs from the record being replaced) does not fire for
      *>   the 2-byte W-2, the literal 42, the 5-byte W-NE or the one
      *>   space character of GR7: GR6 b) makes the statement executed
      *>   the REWRITE without FROM, whose record is record-name-1.
      *>   The file content shows the MOVE's result:
      *>   M1 R-A X(5) FROM W-2 X(2) "AB": alphanumeric receiver, left
      *>      aligned, space filled (§2002:14.8.24.3 GR4 a): [AB   ].
      *>   M2 R-N 9(5) FROM W-NE PIC 9,999 holding "1,234": a numeric-
      *>      edited sender to a numeric receiver is de-edited
      *>      (§2002:14.8.24.3 GR3), so the record is [01234], not the
      *>      edited characters. Fails if FROM copies bytes.
      *>   M3 R-N FROM the literal 42: numeric, zero filled: [00042].
      *> §2002:14.8.33.3 GR7 - "The figurative constant SPACE when
      *>   specified in the REWRITE statement references one
      *>   alphanumeric space character." (cite.py: OK). MOVE of that
      *>   one character to an alphanumeric receiver fills the rest
      *>   with spaces (GR4 a):
      *>   S1 R-A (holding "AB   ") FROM SPACE: [     ].
      *>   S2 the group record G-REC (holding "12345") FROM SPACE: a
      *>      group move is an alphanumeric move (§2002:14.8.24.3 GR2):
      *>      [     ].
      *> Fails if the FROM phrase is not moved first, or not by the
      *> MOVE rules, or if SPACE does not fill the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g2rw07a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FN ASSIGN TO "g2rw07n.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FG ASSIGN TO "g2rw07g.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  R-A PIC X(5).
       FD  FN.
       01  R-N PIC 9(5).
       FD  FG.
       01  G-REC.
           05 G-N PIC 9(3).
           05 G-X PIC X(2).
       WORKING-STORAGE SECTION.
       01  W-2  PIC X(2) VALUE "AB".
       01  W-NE PIC 9,999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FA.
           MOVE "QQQQQ" TO R-A.
           WRITE R-A.
           CLOSE FA.
           OPEN OUTPUT FN.
           MOVE 99999 TO R-N.
           WRITE R-N.
           CLOSE FN.
           OPEN OUTPUT FG.
           MOVE 123 TO G-N.
           MOVE "45" TO G-X.
           WRITE G-REC.
           CLOSE FG.
           OPEN I-O FA. READ FA.
           REWRITE R-A FROM W-2.
           CLOSE FA.
           OPEN INPUT FA. READ FA.
           DISPLAY "M1 [" R-A "]".
           CLOSE FA.
           MOVE 1234 TO W-NE.
           OPEN I-O FN. READ FN.
           REWRITE R-N FROM W-NE.
           CLOSE FN.
           OPEN INPUT FN. READ FN.
           DISPLAY "M2 [" R-N "] " W-NE.
           CLOSE FN.
           OPEN I-O FN. READ FN.
           REWRITE R-N FROM 42.
           CLOSE FN.
           OPEN INPUT FN. READ FN.
           DISPLAY "M3 [" R-N "]".
           CLOSE FN.
           OPEN I-O FA. READ FA.
           REWRITE R-A FROM SPACE.
           CLOSE FA.
           OPEN INPUT FA. READ FA.
           DISPLAY "S1 [" R-A "]".
           CLOSE FA.
           OPEN I-O FG. READ FG.
           REWRITE G-REC FROM SPACE.
           CLOSE FG.
           OPEN INPUT FG. READ FG.
           DISPLAY "S2 [" G-REC "]".
           CLOSE FG.
           STOP RUN.
