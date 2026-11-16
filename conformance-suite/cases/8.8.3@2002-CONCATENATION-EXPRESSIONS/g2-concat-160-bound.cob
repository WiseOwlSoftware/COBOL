      *>--- conformance-case
      *> id: g2-concat-160-bound
      *> rules: [2002-SR-8.8.3.2-3, 2002-SR-8.8.3.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.2 SR3 - "For operands of class boolean, the length
      *> of the value resulting from concatenation shall be less than or
      *> equal to 160 boolean character positions." §2002:8.8.3.2 SR4 -
      *> the same bound, "160 national character positions", for class
      *> national. A result of exactly 160 positions is therefore legal
      *> at 2002 (the 161-position siblings g2-concat-boolean-161 and
      *> g2-concat-national-161 are refused). Each value is four
      *> 40-position literals (§2002:8.8.3.3 GR2: the concatenation of
      *> the values):
      *> boolean 120 ones, 39 zeros, a one; national 120 A, 39 B, one C.
      *> Each is the VALUE of a 161-position item, so position 161 is
      *> the padding (boolean zero, national space) and position 160 is
      *> the last literal's last character.
      *> B1 XB(1:1)=1, XB(121:1)=0, XB(160:1)=1, XB(161:1)=0: T.
      *> N1 XN(1:1)=A, XN(121:1)=B, XN(160:1)=C, XN(161:1)=space: T.
      *> A refused 160 result fails the compile; a truncated or shifted
      *> value fails a position test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XB PIC 1(161) VALUE
           B"1111111111111111111111111111111111111111"
           & B"1111111111111111111111111111111111111111"
           & B"1111111111111111111111111111111111111111"
           & B"0000000000000000000000000000000000000001".
       01  XN PIC N(161) VALUE
           N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBC".
       PROCEDURE DIVISION.
           IF XB(1:1) = B"1" AND XB(121:1) = B"0"
              AND XB(160:1) = B"1" AND XB(161:1) = B"0"
               DISPLAY "B1 T" ELSE DISPLAY "B1 F".
           IF XN(1:1) = N"A" AND XN(121:1) = N"B"
              AND XN(160:1) = N"C" AND XN(161:1) = N" "
               DISPLAY "N1 T" ELSE DISPLAY "N1 F".
           STOP RUN.
