*>--- conformance-case
*> id: pb3355_evaluate_range_same_type_strong_groups
*> rules: [SR-14.9.13.3-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - ISO 14.9.13.3 SR4's LEGAL side for strongly-typed groups. The rule: "The two operands in a
      *> range-expression shall be of the same class and shall not be of class boolean, message-tag, object, or
      *> pointer" (cite.py --check 14.9.13.3 "The two operands in a range-expression shall be of the same class" -> OK).
      *> 8.5.2.1 makes the class of a strongly-typed group its type-name (cite.py --check 8.5.2.1 "Both the class and
      *> the category of a strongly-typed group item are the type-name specified in the TYPE clause" -> OK), so two
      *> groups of the SAME type are of the same class and the range is legal; the selection subject is a group of
      *> that type too (8.8.4.2.12 compares two strongly-typed groups of one type element by element).
      *> Why this can fail: the refusal pair pb3355-evaluate-range-strong-group-and-alphanumeric and
      *> pb3355-evaluate-range-strong-groups-of-two-types holds the other side of the boundary, so a screen that
      *> refused EVERY strongly-typed range end (a type-name never "the same class" as anything) would pass those two
      *> and fail here, where the ends are one type. A screen that took a strong group for plain alphanumeric would
      *> pass this program and fail the pair.
      *> Expected: AA <= BB <= CC is IN; ZZ is OUT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355EVOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 F1 PIC X(2).
       01 LO TYPE REC-T.
       01 HI TYPE REC-T.
       01 V TYPE REC-T.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AA" TO F1 OF LO
           MOVE "CC" TO F1 OF HI
           MOVE "BB" TO F1 OF V
           EVALUATE V
             WHEN LO THRU HI DISPLAY "BB=IN"
             WHEN OTHER DISPLAY "BB=OUT"
           END-EVALUATE
           MOVE "ZZ" TO F1 OF V
           EVALUATE V
             WHEN LO THRU HI DISPLAY "ZZ=IN"
             WHEN OTHER DISPLAY "ZZ=OUT"
           END-EVALUATE
           STOP RUN.
