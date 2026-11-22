      *>--- conformance-case
      *> id: pb6025_relative_next_group_past_page_limit_85
      *> rules: [1985-SR-XIII.3.16.3-2, 1985-GR-XIII.3.10.8-6,
      *>    1985-GR-XIII.3.10.8-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6025. ANSI X3.23-1985 bounds a relative NEXT GROUP
      *> integer only by 1985:XIII.3.16.3 SR2, "Integer-1 and integer-2
      *> must not exceed three significant digits in length"; the page
      *> limit bounds the ABSOLUTE integer of a body group
      *> (1985:XIII.3.10.8 GR5), and a relative one is absorbed by the
      *> final LINE-COUNTER setting rule 1985:XIII.3.10.8 GR6b: "If the
      *> sum is equal to or greater than the line number specified by
      *> the FOOTING phrase of the PAGE clause, then the RWCS places the
      *> line number specified by the FOOTING phrase of the PAGE clause
      *> into LINE-COUNTER". COBOL-2002 adds the page-limit bound (2002
      *> 13.16.35.2 SR1), so NEXT GROUP PLUS 50 under PAGE LIMIT 20 is
      *> legal at 85 only.
      *> DERIVATION. LAST DETAIL and FOOTING omitted: both 20
      *> (1985:XIII.3.8.4 GR2c). GENERATE 1: nothing yet on the page,
      *> LINE-COUNTER 0 < FIRST DETAIL 2, so the line is 2 (GR4b);
      *> 2 + 50 = 52 >= 20, so LINE-COUNTER = 20 (GR6b). GENERATE 2:
      *> 20 + 1 = 21 > 20 (GR3b), page advance, PAGE-COUNTER 2, the
      *> line is 2 again and LINE-COUNTER 20. Why it can fail: a
      *> compiler that applies the 2002 bound refuses it (COBOL2284);
      *> an engine that adds the 50 prints 052 or keeps page 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6025A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb6025a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W-LC PIC 9(3).
       01  W-PC PIC 9(3).
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES FIRST DETAIL 2.
       01  D1 TYPE DETAIL LINE PLUS 1 NEXT GROUP PLUS 50.
           05  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE D1.
           MOVE LINE-COUNTER TO W-LC.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY "LC1 " W-LC " PC1 " W-PC.
           GENERATE D1.
           MOVE LINE-COUNTER TO W-LC.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY "LC2 " W-LC " PC2 " W-PC.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
