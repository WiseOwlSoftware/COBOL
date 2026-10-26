*>--- conformance-case
*> id: pb3682_same_as_after_length_constant
*> rules: [GR-13.18.49.4-1, GR-13.18.60.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - A COPY OF A GROUP THE COMPILER MEASURED FIRST IS THE GROUP AS WRITTEN.
      *>   13.10.4 GR6 (cite.py: OK  13.10.4 6)) gives LENGTH OF its value "as specified in the LENGTH intrinsic function", which
      *>   measures the item as described - so the compiler completes X's description (its group usage reaching its members,
      *>   13.18.60.4 GR1, cite.py: OK  13.18.60.4 1)) when the constant entry C names it, BEFORE the SAME AS entry copies X.
      *>   13.18.49.4 GR1 copies the description "as though ... coded in place" (cite.py: OK  13.18.49.4 1)), so V is the same
      *>   group as X whether or not C measured X first: two members, and a length equal to X's.
      *>   Before the fix the copy took the header's profile without its provenance and read it as a WRITTEN PICTURE on a group
      *>   (COBOL2191), and its members had no profile to take (COBOL0881).
      *>   Each leg can fail: the program is refused; V has no members; V is not the length of X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X USAGE BINARY-LONG.
          05 XA.
          05 XB.
       01 C CONSTANT AS LENGTH OF X.
       01 V SAME AS X.
       01 N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 5 TO XA OF V
           MOVE 6 TO XB OF V
           ADD XA OF V TO XB OF V GIVING N
           DISPLAY N
           IF C = FUNCTION LENGTH(V)
              DISPLAY "SAME"
           END-IF
           STOP RUN.
