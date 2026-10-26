*>--- conformance-case
*> id: pb2282_perform_varying_by_omitted
*> rules: [GR-14.9.28.4-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2282 - a PERFORM VARYING phrase and an AFTER phrase with their BY phrases omitted, at the
      *> introducing edition COBOL-2002 (VCR row 7.42; constructs.json perform-varying-by-omitted-2002; below
      *> 2002 it is COBOL0900, negative pb2282-perform-varying-by-omitted-below-2002).
      *>   cite.py --check 14.9.28.4 "For any BY phrase that is omitted, the augment value is 1" -> OK §14.9.28.4 12)
      *>   cite.py --check 14.9.28.4 "the induction variable is incremented by the augment value, and condition-1
      *>     is evaluated again" -> OK §14.9.28.4 13) d)
      *>   cite.py --check 14.9.28.4 "the induction variable associated with the current condition is set to its
      *>     initialization value" -> OK §14.9.28.4 13)
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   N=1 N=2 N=3   the inline VARYING has no BY, so N is augmented by 1 (GR12) until N > 3.
      *>   END1 N=4      13) d): condition-1 is tested after each augment; N = 4 makes it true and ends the loop.
      *>   1 7 / 1 8 / 3 7 / 3 8
      *>                 the out-of-line PERFORM: N BY 2, the AFTER phrase M FROM 7 with no BY (augment 1).
      *>   END2 N=5 M=7  13) e): when M > 8 is true, M is set to its initialization value 7 and N is augmented
      *>                 to 5, which makes N > 3 true and ends the PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2282S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       01 M PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING N FROM 1 UNTIL N > 3
               DISPLAY "N=" N
           END-PERFORM
           DISPLAY "END1 N=" N
           PERFORM SHOW-PARA VARYING N FROM 1 BY 2 UNTIL N > 3
               AFTER M FROM 7 UNTIL M > 8
           DISPLAY "END2 N=" N " M=" M
           STOP RUN.
       SHOW-PARA.
           DISPLAY N " " M.
