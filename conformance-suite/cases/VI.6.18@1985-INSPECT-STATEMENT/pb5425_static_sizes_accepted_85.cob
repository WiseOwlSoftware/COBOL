      *>--- conformance-case
      *> id: pb5425_static_sizes_accepted_85
      *> rules: [1985-SR-VI.6.18.3-7, 1985-SR-VI.6.18.3-8,
      *>     1985-SR-VI.6.18.3-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5425: the controls of the six refusals beside it;
      *> each static size equals the one the rule pairs it with.
      *>   'AB' BY W3 (3:)                 YZ, 2 = 2 -> YZCDYZCD
      *>   CHARACTERS BY W3 (4:)           Z, 1     -> ZZZZZZZZ
      *>   'ZA' TO FUNCTION UPPER-CASE (W2) YZ, 2 = 2 -> YYYYYYYY
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5425A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(8) VALUE "ABCDABCD".
       01  W2 PIC X(2) VALUE "yz".
       01  W3 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 REPLACING ALL 'AB' BY W3 (3:).
           DISPLAY W1 "|".
           INSPECT W1 REPLACING CHARACTERS BY W3 (4:).
           DISPLAY W1 "|".
           INSPECT W1 CONVERTING 'ZA' TO FUNCTION UPPER-CASE (W2).
           DISPLAY W1 "|".
           STOP RUN.
