      *>--- conformance-case
      *> id: g2-any-length-zero-length-by-content
      *> rules: [2002-GR-13.16.2.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.2.3 GR1 - the ANY LENGTH subject "shall be: a) a
      *> zero-length item when the corresponding argument ... is a
      *> zero-length item, or b) treated as though there were n
      *> repetitions of the picture symbol ... where n is the length of
      *> the corresponding argument".
      *> The argument is a 2002 zero-length item: G contains only the
      *> variable-occurrence item T, and N = 0 (§2002:8.5.4 item 1).
      *> It is passed BY CONTENT through a format 3 CALL ... AS NESTED
      *> (§2002:14.8.4.3 GR6), so the allocated record is "a data item
      *> of the same category, usage, and length as the argument"
      *> (§2002:14.1.3 GR9), and "For a variable-occurrence data item
      *> passed by content, the length of the argument is determined by
      *> the rules of the OCCURS clause for a sending data item"
      *> (§2002:14.7.1.1): length 0 for the first call (leg a), 3 for
      *> the second (leg b).  A DISPLAY of a zero-length item transfers
      *> no data (§2002:14.8.10.3 GR1), hence "[]".
      *> Fails if the callee sees the maximum length 5 (the BY
      *> REFERENCE rule) or any length other than the current one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ANY01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 G.
          05 T PIC X OCCURS 0 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G2ANY01S" AS NESTED USING BY CONTENT G
           MOVE 3 TO N
           MOVE "A" TO T(1)
           MOVE "B" TO T(2)
           MOVE "C" TO T(3)
           CALL "G2ANY01S" AS NESTED USING BY CONTENT G
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ANY01S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L2 PIC 99.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
           MOVE FUNCTION LENGTH(AL) TO L2
           DISPLAY "ARGLEN=" L2 " [" AL "]".
           GOBACK.
       END PROGRAM G2ANY01S.
       END PROGRAM G2ANY01.
