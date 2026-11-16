      *>--- conformance-case
      *> id: g85_f21_07
      *> rules: [1985-GR-X.5.2.4-10, 1985-GR-X.5.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR10 - "This correspondence is positional and not
      *> by name equivalence": the caller passes P-ONE then P-TWO; the
      *> called program G85F2157 declares the same two names but lists
      *> them as USING P-TWO P-ONE, so its P-TWO is the caller's P-ONE
      *> and its P-ONE is the caller's P-TWO. The CALLEE lines show the
      *> swapped values, and its MOVE to its P-TWO reaches the caller's
      *> P-ONE (CALLER P-ONE XXX). Matching by name would print the
      *> values unswapped and change the caller's P-TWO.
      *> §1985:X.5.2.4 GR16 - "The END-CALL phrase delimits the scope of
      *> the CALL statement": both CALLs succeed, so their ON OVERFLOW
      *> or ON EXCEPTION phrase is ignored (GR2); the DISPLAY after
      *> END-CALL in each IF is outside the CALL and runs. Without the
      *> delimiting it would belong to that imperative-statement-1 and
      *> not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2107.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P-ONE PIC X(3) VALUE "111".
       01  P-TWO PIC X(3) VALUE "222".
       01  WS-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           IF WS-FLAG = 1
               CALL "G85F2157" USING P-ONE P-TWO
                   ON OVERFLOW DISPLAY "OVERFLOW"
               END-CALL
               DISPLAY "AFTER END-CALL 1"
           END-IF.
           DISPLAY "CALLER P-ONE " P-ONE.
           DISPLAY "CALLER P-TWO " P-TWO.
           MOVE "333" TO P-TWO.
           IF WS-FLAG = 1
               CALL "G85F2157" USING P-ONE P-TWO
                   ON EXCEPTION DISPLAY "EXCEPTION"
               END-CALL
               DISPLAY "AFTER END-CALL 2"
           END-IF.
           DISPLAY "CALLER P-ONE " P-ONE.
           DISPLAY "CALLER P-TWO " P-TWO.
           STOP RUN.
       END PROGRAM G85F2107.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2157.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-ONE PIC X(3).
       01  P-TWO PIC X(3).
       PROCEDURE DIVISION USING P-TWO P-ONE.
       SUB-P.
           DISPLAY "CALLEE P-ONE " P-ONE.
           DISPLAY "CALLEE P-TWO " P-TWO.
           MOVE "XXX" TO P-TWO.
           EXIT PROGRAM.
       END PROGRAM G85F2157.
