      *>--- conformance-case
      *> id: pb5375-inspect-scaled-numeric-characters
      *> rules: [1985-GR-VI.6.18.4-3, 1985-GR-VI.5.9.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5375. §1985:VI.6.18.4 GR3b: an unsigned numeric data
      *> item "is inspected as though it had been redefined as
      *> alphanumeric", and §1985:VI.5.9.4 GR8 says of the PICTURE symbol
      *> P that "the scaling position character 'P' is not counted in the
      *> size of the data item", so the item holds only the characters
      *> its 9s describe. A9 is PIC 9PP holding 500: one character, "5",
      *> so it holds no zero, TALLYING FOR CHARACTERS counts 1 and
      *> REPLACING ALL "5" BY "7" leaves the group of A9 and the four
      *> characters after it five characters long. V9 is PIC VPP9 holding
      *> .007: the one character "7". A signed item (S9 is S9PP holding
      *> -500) is inspected as an unsigned numeric item of the same
      *> length (GR3c), so its CHARACTERS also count 1. The group G holds
      *> A9 and "ABCD", and is five characters long before and after.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5375A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A9 PIC 9PP VALUE 500.
           05  AX PIC X(4) VALUE "ABCD".
       01  V9 PIC VPP9 VALUE .007.
       01  S9 PIC S9PP VALUE -500.
       01  T1 PIC 99 VALUE 0.
       01  T2 PIC 99 VALUE 0.
       01  T3 PIC 99 VALUE 0.
       01  T4 PIC 99 VALUE 0.
       01  T5 PIC 99 VALUE 0.
       01  TZ PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT A9 TALLYING T1 FOR ALL "0".
           INSPECT A9 TALLYING T2 FOR CHARACTERS.
           INSPECT V9 TALLYING T3 FOR ALL "0".
           INSPECT V9 TALLYING T4 FOR CHARACTERS.
           INSPECT S9 TALLYING T5 FOR CHARACTERS.
           INSPECT AX TALLYING TZ FOR ALL "5".
           DISPLAY "TALLY " T1 " " T2 " " T3 " " T4 " " T5 " " TZ.
           DISPLAY "BEFORE [" G "]".
           INSPECT G TALLYING TZ FOR ALL "0".
           INSPECT A9 REPLACING ALL "5" BY "7".
           DISPLAY "AFTER [" G "] " TZ.
           INSPECT V9 REPLACING ALL "7" BY "8".
           DISPLAY "V9 " V9.
           STOP RUN.
