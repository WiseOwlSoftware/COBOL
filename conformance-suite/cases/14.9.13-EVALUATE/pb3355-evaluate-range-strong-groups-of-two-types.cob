*>--- conformance-case
*> id: pb3355-evaluate-range-strong-groups-of-two-types
*> rules: [SR-14.9.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - ISO 14.9.13.3 SR4: "The two operands in a range-expression shall be of the same class"
      *> (cite.py --check 14.9.13.3 "The two operands in a range-expression shall be of the same class" -> OK). 8.5.2.1:
      *> "Both the class and the category of a strongly-typed group item are the type-name specified in the TYPE clause"
      *> (cite.py --check 8.5.2.1 -> OK), so LO (class TYPE-A) and HI (class TYPE-B) are two classes though both
      *> types hold one PIC X(2) leaf, and the range LO THRU HI is refused by SR4 itself (COBOL2107).
      *> Why this can fail: the range-class screen compared the lattice's two answers (alphanumeric = alphanumeric).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355EVN2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TYPE-A IS TYPEDEF STRONG.
          05 F1 PIC X(2).
       01 TYPE-B IS TYPEDEF STRONG.
          05 F2 PIC X(2).
       01 LO TYPE TYPE-A.
       01 HI TYPE TYPE-B.
       01 S TYPE TYPE-A.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "BB" TO F1 OF S
           EVALUATE S
             WHEN LO THRU HI DISPLAY "IN"
             WHEN OTHER DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
