*>--- conformance-case
*> id: pb6228_bare_condition_object_subscript_not_hoisted
*> rules: [GR-14.9.13.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6228 - an EVALUATE selection object is evaluated only when its WHEN phrase is analyzed
      *> (14.9.13.4 GR4: the pairs of a WHEN phrase are analyzed left to right; cite.py --check "14.9.13.4 GR4" OK).
      *> X = 1 selects the FIRST WHEN, so the second WHEN's bare level-88 object T-OK, whose subscript holds
      *> FUNCTION RANDOM, is never analyzed and RANDOM must not run. The generator is seeded with 7, EVALUATE runs,
      *> RANDOM is drawn once (R1); it is re-seeded with 7 and drawn once (R2). Without a hidden draw in the EVALUATE
      *> the first draw after the seed is the same number both times, so the program prints UNTOUCHED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6228A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       01 T-TAB.
          05 T-FLAG PIC X OCCURS 3 VALUE "Y".
             88 T-OK VALUE "Y".
       01 R1 PIC 9V9(6).
       01 R2 PIC 9V9(6).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R1 = FUNCTION RANDOM (7).
           EVALUATE TRUE
              WHEN X = 1
                 DISPLAY "ONE"
              WHEN T-OK (FUNCTION INTEGER (FUNCTION RANDOM * 3) + 1)
                 DISPLAY "OK"
           END-EVALUATE.
           COMPUTE R1 = FUNCTION RANDOM.
           COMPUTE R2 = FUNCTION RANDOM (7).
           COMPUTE R2 = FUNCTION RANDOM.
           IF R1 = R2 DISPLAY "UNTOUCHED" ELSE DISPLAY "HOISTED".
           STOP RUN.
