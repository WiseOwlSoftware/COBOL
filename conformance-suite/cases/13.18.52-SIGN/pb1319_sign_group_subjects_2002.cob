*>--- conformance-case
*> id: pb1319_sign_group_subjects_2002
*> rules: [SR-13.18.52.3-1, SR-13.18.52.3-2, GR-13.18.52.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1319 - the two group SIGN subjects COBOL-85 refused and the 2023 text admits (the derived 2002 edge,
      *> VCR row 7.40; the 85 refusals are conformance:negative/pb1319-sign-group-unsigned-85 and
      *> conformance:negative/pb1319-sign-group-comp-85).
      *> SR1: "The SIGN clause may be specified only for: ... an alphanumeric group item, national group item, or
      *>   strongly-typed group item."   cite.py: OK  13.18.52.3 1)  (Syntax rules)
      *> SR2: "The usage of an elementary item for which the SIGN clause is specified shall be display or national."
      *>   cite.py: OK  13.18.52.3 2)  (Syntax rules)
      *> GR1: "The SIGN clause applies only to numeric items whose picture character-string contains the symbol 'S'."
      *>   cite.py: OK  13.18.52.4 1)  (General rules)
      *> G1 (SIGN LEADING SEPARATE) holds no signed numeric item - legal by SR1 - so the clause applies to nothing:
      *>   X1 "ABC", N1 PIC 9(3) 5 -> "005" (GR1: no 'S', no sign position).
      *> G2 (SIGN LEADING SEPARATE) holds a binary signed item C2 and a display signed item D2: SR2 speaks of the entry
      *>   that WRITES the clause, so both are legal; D2 = -8 -> "-008" (GR6 a/b); C2 keeps its binary form
      *>   (CONFORMANCE.md D-SIGN1), so its byte length is 2 and DISPLAY shows its value "-007" through the display
      *>   conversion of a signed binary item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1319G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 SIGN IS LEADING SEPARATE.
          05 X1 PIC X(3) VALUE "ABC".
          05 N1 PIC 9(3) VALUE 5.
       01 G2 SIGN IS LEADING SEPARATE.
          05 C2 PIC S9(3) COMP VALUE -7.
          05 D2 PIC S9(3) VALUE -8.
       PROCEDURE DIVISION.
           DISPLAY "G1=" G1
           DISPLAY "D2=" D2 " LEN-D2=" FUNCTION LENGTH(D2)
           DISPLAY "C2=" C2 " BYTES-C2=" FUNCTION BYTE-LENGTH(C2)
           STOP RUN.
