      *> EC-RANGE-INVALID (ISO §14.7.8 THROUGH phrase rule 2, spec :24863; Table 13 Nonfatal): an alphanumeric or
      *> national THRU range whose starting value collates AFTER its ending value (an inverted range) — in a level-88
      *> VALUE clause or an EVALUATE WHEN range — sets the nonfatal EC-RANGE-INVALID and the range is treated as EMPTY.
      *> A numeric descending range (rule 1) sets NO exception. Observed via FUNCTION EXCEPTION-STATUS under
      *> >>TURN EC-RANGE-INVALID CHECKING ON.
      >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-RNG-IV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "M".
          88 VALID-RANGE VALUE "A" THRU "Z".
          88 INV-RANGE   VALUE "Z" THRU "A".
       01 WS-N PIC 9 VALUE 5.
          88 NUM-INV VALUE 9 THRU 1.
       PROCEDURE DIVISION.
       MAIN-P.
      *> valid alphanumeric range (M in A..Z): membership true, no EC.
           IF VALID-RANGE DISPLAY "VALID-TRUE" ELSE DISPLAY "VALID-FALSE" END-IF.
           DISPLAY "V[" FUNCTION EXCEPTION-STATUS "]".
      *> numeric inverted range (9 THRU 1): rule 1 sets no EC; membership false.
           IF NUM-INV DISPLAY "NUM-TRUE" ELSE DISPLAY "NUM-FALSE" END-IF.
           DISPLAY "N[" FUNCTION EXCEPTION-STATUS "]".
      *> level-88 inverted alphanumeric range (Z THRU A): EC-RANGE-INVALID, membership false (empty).
           IF INV-RANGE DISPLAY "INV-TRUE" ELSE DISPLAY "INV-FALSE" END-IF.
           DISPLAY "I88[" FUNCTION EXCEPTION-STATUS "]".
      *> EVALUATE WHEN inverted alphanumeric range: EC set, WHEN not taken (empty range).
           EVALUATE WS-C
               WHEN "Z" THRU "A" DISPLAY "EVAL-MATCH"
               WHEN OTHER        DISPLAY "EVAL-OTHER"
           END-EVALUATE.
           DISPLAY "IEV[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
