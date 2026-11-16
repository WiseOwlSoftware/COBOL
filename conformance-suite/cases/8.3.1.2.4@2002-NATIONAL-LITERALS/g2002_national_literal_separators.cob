      *>--- conformance-case
      *> id: g2002_national_literal_separators
      *> rules: [2002-GR-8.3.1.2.4.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.4.3 GR1 - "The separators that delimit the
      *> national literal are not included in the value of the national
      *> literal."
      *> Both formats and both delimiters: N"AB", N'AB' and NX"00410042"
      *> (four hex digits per national character, DOC-A.1-97/98) each
      *> have the two-character value AB, so LENGTH (§2002:15.39.3 RV2,
      *> national character positions) is 2 and a MOVE to N(4) gives AB
      *> and two spaces. N"A""B" (§2002:8.3.1.2.4.2 SR3: the doubled
      *> quotation symbol is one quotation symbol) has the value A"B,
      *> length 3.
      *> WHY EACH LEG CAN FAIL: a delimiter kept in the value shows a
      *> quote or N in QD/AP/HX/DQ, a length above 2 in LQ/LA/LHX or
      *> above 3 in LDQ, and EQ=N when the three spellings do not decode
      *> to one value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NAT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N4  PIC N(4).
       01 W-V   PIC N(4) VALUE N'AB'.
       01 W-Q   PIC N(3).
       01 W-H   PIC N(4).
       01 W-L   PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE N"AB" TO W-N4.
           DISPLAY "QD=[" W-N4 "]".
           DISPLAY "AP=[" W-V "]".
           MOVE FUNCTION LENGTH(N"AB") TO W-L.
           DISPLAY "LQ=" W-L.
           MOVE FUNCTION LENGTH(N'AB') TO W-L.
           DISPLAY "LA=" W-L.
           MOVE N"A""B" TO W-Q.
           DISPLAY "DQ=[" W-Q "]".
           MOVE FUNCTION LENGTH(N"A""B") TO W-L.
           DISPLAY "LDQ=" W-L.
           MOVE NX"00410042" TO W-H.
           DISPLAY "HX=[" W-H "]".
           MOVE FUNCTION LENGTH(NX"00410042") TO W-L.
           DISPLAY "LHX=" W-L.
           MOVE N"AB" TO W-N4.
           IF W-N4 = N'AB' AND W-N4 = NX"00410042"
              DISPLAY "EQ=Y"
           ELSE
              DISPLAY "EQ=N"
           END-IF.
           STOP RUN.
