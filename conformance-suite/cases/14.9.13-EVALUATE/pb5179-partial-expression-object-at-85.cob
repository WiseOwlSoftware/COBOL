      *>--- conformance-case
      *> id: pb5179-partial-expression-object-at-85
      *> rules: [SR-14.9.13.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5179 - the edition half of pb5179_partial_expression_object_2002. The COBOL-85 selection object
      *> (§1985:VI.6.13.2) is ANY, a condition, TRUE, FALSE or [NOT] an identifier, literal or arithmetic expression;
      *> "> 3" is none of them (2002 Annex F.2 item 48 added it). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5179NEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       P1.
           EVALUATE A
               WHEN > 3 DISPLAY "GT"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
