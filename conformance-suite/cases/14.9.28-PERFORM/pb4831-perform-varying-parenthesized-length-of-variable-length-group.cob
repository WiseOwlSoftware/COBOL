*>--- conformance-case
*> id: pb4831-perform-varying-parenthesized-length-of-variable-length-group
*> rules: [SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> PB4831 - the parenthesized twin of pb4831_perform_varying_length_of_variable_length_group. ISO §14.9.28.2's FROM phrase
      *> prints { identifier-3 | index-name-2 | literal-1 }; "(FUNCTION LENGTH (VG))" is "an arithmetic expression enclosed in
      *> parentheses" (§8.8.1.1), no identifier, so it is refused although the bare function is admitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPVPARENVGLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K     PIC 9 VALUE 3.
       01 VG.
          05 T  PIC X OCCURS 1 TO 9 DEPENDING ON K.
       01 I     PIC 99.
       PROCEDURE DIVISION.
           PERFORM VARYING I FROM (FUNCTION LENGTH (VG)) BY 1 UNTIL I > 5
               DISPLAY I
           END-PERFORM
           STOP RUN.
