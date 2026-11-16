      *>--- conformance-case
      *> id: g2002_national_literal_forms
      *> rules: [2002-FMT-8.3.1.2.4.1, 2002-SR-8.3.1.2.4.2-1, 2002-SR-8.3.1.2.4.2-5, 2002-GR-8.3.1.2.4.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.4.1 - format 1 N"..." / N'...' and format 2
      *> NX"..." / NX'...', each with one or more characters, accepted.
      *> §2002:8.3.1.2.4.2 SR1 - "greater than zero and less than or
      *> equal
      *> to 160 national character positions": N"AB" (2) and the
      *> 160-position
      *> literal continued with the floating indicator "- (§2002:6.1.2)
      *> are
      *> accepted; the zero-length and 161-position twins are refused.
      *> §2002:8.3.1.2.4.2 SR3 - N'XY''' holds one apostrophe: XY'.
      *> §2002:8.3.1.2.4.2 SR5 - each hex-character-sequence-1 has the
      *> implementor's digit count per national character, four
      *> (docs/CONFORMANCE.md DOC-A.1-97): NX"00410042" is two groups,
      *> NX'0043' one; the misgrouped twin is refused.
      *> §2002:8.3.1.2.4.3 GR4 - each group is one national character of
      *> that bit configuration: in the implementor's UTF-16 national
      *> set
      *> 0041 0042 are A B and 0043 is C; N3 (from NX) equals N"AB" and
      *> N2 (from N"AB") equals NX"00410042" (line 3).
      *> EXPECTED, DERIVED: 1[XY'][AB] 2[AB][C] 3[NX-EQ] 4[AB][B]
      *> 5[160].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NAT06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 N1 PIC N(3) VALUE N'XY'''.
           01 N2 PIC N(2) VALUE N"AB".
           01 N3 PIC N(2) VALUE NX"00410042".
           01 N4 PIC N(1) VALUE NX'0043'.
           01 N160 PIC N(160).
           01 L3 PIC 999.
       PROCEDURE DIVISION.
           DISPLAY "1[" N1 "][" N2 "]"
           DISPLAY "2[" N3 "][" N4 "]"
           IF N3 = N"AB" AND N2 = NX"00410042"
               DISPLAY "3[NX-EQ]"
           ELSE
               DISPLAY "3[NX-NE]"
           END-IF
           MOVE N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"-
           "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBB"-
           "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"-
           "BBBBBBBBBB"
               TO N160
           DISPLAY "4[" N160(80:2) "][" N160(160:1) "]"
           MOVE FUNCTION LENGTH(N160) TO L3
           DISPLAY "5[" L3 "]"
           STOP RUN.
