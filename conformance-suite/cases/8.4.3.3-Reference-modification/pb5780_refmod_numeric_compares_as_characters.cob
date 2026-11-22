      *>--- conformance-case
      *> id: pb5780_refmod_numeric_compares_as_characters
      *> rules: [GR-8.4.3.3.4-6, GR-8.4.3.3.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5780 - the 2023 twin: §2023:8.4.3.3.4 GR6a (the
      *> unique data item "is considered class and category
      *> alphanumeric"). The 1985 text, which the rest quotes:
      *> a reference-modified numeric item is class
      *> and category alphanumeric. §1985:IV.4.3.8.4.4 GR5: "the
      *> categories numeric, numeric edited, and alphanumeric edited
      *> are considered class and category alphanumeric"; GR2: the
      *> evaluation is "as if it were redefined" as an alphanumeric
      *> item. A relation with a numeric operand is then the
      *> nonnumeric comparison (§1985:VI.6.3.1.1.2: "For nonnumeric
      *> operands, or one numeric and one nonnumeric operand, a
      *> comparison is made with respect to a specified collating
      *> sequence of characters"): the integer is compared as its
      *> characters, the shorter operand padded with spaces.
      *> Why each leg can fail (the defect compared the inner item's
      *> numeric value):
      *>   RM=5    "005" with "5  " - unequal: NO (was YES).
      *>   RM2=5   "05" with "5 " - unequal: NO (was YES).
      *>   RM>5    "123" with "5  " - "1" precedes "5": NO (was YES).
      *>   RM=X3   "005" with "5  " (an X(3) item): NO.
      *>   RM=ZERO "005" with "000": NO.
      *>   RM=LIT  "005" with "005": YES.
      *>   RM=N3   "005" with N3 as characters "005": YES.
      *>   EVALUATE N3 (1:3) WHEN 5: "005" with "5": the OTHER arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5780RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N3 PIC 9(3) VALUE 5.
       01 N5 PIC 9(3) VALUE 123.
       01 X3 PIC X(3) VALUE "5".
       PROCEDURE DIVISION.
       P1.
           IF N3 (1:3) = 5 DISPLAY "RM=5 YES" ELSE DISPLAY "RM=5 NO".
           IF N3 (2:2) = 5 DISPLAY "RM2=5 YES"
               ELSE DISPLAY "RM2=5 NO".
           IF N5 (1:3) > 5 DISPLAY "RM>5 YES" ELSE DISPLAY "RM>5 NO".
           IF N3 (1:3) = X3 DISPLAY "RM=X3 YES"
               ELSE DISPLAY "RM=X3 NO".
           IF N3 (1:3) = ZERO DISPLAY "RM=ZERO YES"
               ELSE DISPLAY "RM=ZERO NO".
           IF N3 (1:3) = "005" DISPLAY "RM=LIT YES"
               ELSE DISPLAY "RM=LIT NO".
           IF N3 (1:3) = N3 DISPLAY "RM=N3 YES"
               ELSE DISPLAY "RM=N3 NO".
           EVALUATE N3 (1:3)
               WHEN 5 DISPLAY "EV 5"
               WHEN OTHER DISPLAY "EV OTHER"
           END-EVALUATE.
           STOP RUN.
