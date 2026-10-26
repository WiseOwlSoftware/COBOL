*>--- conformance-case
*> id: pb3355-evaluate-range-strong-group-and-alphanumeric
*> rules: [SR-14.9.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - ISO 14.9.13.3 SR4: "The two operands in a range-expression shall be of the same class"
      *> (cite.py --check 14.9.13.3 "The two operands in a range-expression shall be of the same class" -> OK). 8.5.2.1:
      *> "Both the class and the category of a strongly-typed group item are the type-name specified in the TYPE clause"
      *> (cite.py --check 8.5.2.1 -> OK), so the strongly-typed group V (class REC-T) and the alphanumeric item HI-X
      *> (class alphanumeric) are two classes, and the range V THRU HI-X is refused by SR4 itself (COBOL2107), not by
      *> the relation rule the range lowers to. The class lattice answers ALPHANUMERIC for a strong group because
      *> 15.3 item 2 says so for a function ARGUMENT, which is not a range operand.
      *> Why this can fail: the range-class screen compared the lattice's two answers (alphanumeric = alphanumeric).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355EVN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 F1 PIC X(2).
       01 V TYPE REC-T.
       01 HI-X PIC X(2).
       01 S PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "BB" TO S
           EVALUATE S
             WHEN V THRU HI-X DISPLAY "IN"
             WHEN OTHER DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
