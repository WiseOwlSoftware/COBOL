      *>--- conformance-case
      *> id: pb5901_using_by_reference
      *> rules: [FMT-14.2.1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of pb5901-using-by-reference-at-85 (kb/Work
      *> PB5901). From 2002 the using-phrase prints [BY REFERENCE]
      *> (§2023:14.2.1, "where using-phrase is", cite.py --check OK),
      *> and a BY REFERENCE formal shares its argument's storage:
      *> §2023:14.2.3 GR4, "Both the BY REFERENCE and the BY VALUE
      *> phrases are transitive across the parameters that follow
      *> them" (cite.py --check OK). The callee stores XYZ into L1,
      *> so the caller's W-ITEM prints XYZ. Why it can fail: a
      *> compiler that refuses the phrase, or passes a copy, prints
      *> nothing or ABC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ITEM PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           CALL "PB5901Q" USING W-ITEM.
           DISPLAY W-ITEM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(3).
       PROCEDURE DIVISION USING BY REFERENCE L1.
       P1.
           MOVE "XYZ" TO L1.
           EXIT PROGRAM.
       END PROGRAM PB5901Q.
       END PROGRAM PB5901P.
