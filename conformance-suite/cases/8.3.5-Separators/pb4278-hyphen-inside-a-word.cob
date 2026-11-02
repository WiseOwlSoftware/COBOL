      *>--- conformance-case
      *> id: pb4278-hyphen-inside-a-word
      *> rules: [SR-8.3.5-1, SR-8.3.3.3.2-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4278 - ISO 8.3.5 1): "The COBOL character space is
      *> a separator", and the hyphen is none, so the space alone
      *> splits WS-B - 3 into three character-strings (8.3.1) while
      *> WS-B, A-1-B and MAIN-PARA stay one word each, the hyphen
      *> inside them a word character (8.3.2.1: "The hyphen or
      *> underscore shall not appear as the first or last character
      *> in such words"). Beside its spaces the hyphen is the
      *> arithmetic minus (8.7.1: "preceded by a space and followed
      *> by a space"); touching its digits it is the sign of a
      *> literal (8.3.3.3.2 2): "If a sign is used, it shall appear
      *> as the leftmost character"). None of these is a word's edge;
      *> the refused spellings NAME- and -NAME are the sibling
      *> negatives (cases/8.1.3-COBOL-character-repertoire/pb4278-*).
      *> Expected: 5 - -2 = 7, 5 - 3 = 2, - (-12) = 12, then WORD.
      *> A leg fails if a spaced minus or a sign is read as a word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4278P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC S9(3) VALUE -12.
       01 WS-B PIC 9(3) VALUE 5.
       01 WS-C PIC 9(3).
       01 A-1-B PIC X(4) VALUE "WORD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-C = WS-B - -2.
           DISPLAY WS-C.
           COMPUTE WS-C = WS-B - 3.
           DISPLAY WS-C.
           MOVE -12 TO WS-A.
           COMPUTE WS-C = - WS-A.
           DISPLAY WS-C.
           DISPLAY A-1-B.
           STOP RUN.
