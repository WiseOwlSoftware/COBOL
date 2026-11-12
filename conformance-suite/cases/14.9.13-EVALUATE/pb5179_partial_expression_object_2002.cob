      *>--- conformance-case
      *> id: pb5179_partial_expression_object_2002
      *> rules: [SR-14.9.13.3-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5179 - a partial expression as an EVALUATE selection object. §2023:14.9.13.3 SR5: "A selection
      *> object is a partial-expression if the leftmost portion of the selection object is a relational operator, a
      *> class condition without the identifier" ...; ISO/IEC 1989:2002 Annex F.2 item 48 added it ("A partial
      *> expression may now be used as a selection object in an EVALUATE statement"). A is 5: "> 3" with the
      *> subject forms A > 3 (true, GT); NUMERIC forms A IS NUMERIC (true, NUM); "< 3" is false, so WHEN OTHER (GE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5179EP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       P1.
           EVALUATE A
               WHEN > 3 DISPLAY "GT"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE A
               WHEN NUMERIC DISPLAY "NUM"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE A
               WHEN < 3 DISPLAY "LT"
               WHEN OTHER DISPLAY "GE"
           END-EVALUATE.
           STOP RUN.
