      *>--- conformance-case
      *> id: g2cd06_directive_lines_not_replaced
      *> rules: [2002-GR-7.2.3-1]
      *> accept: [2002]
      *> files: [G2CDW.cpy, G2CDL.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.3 GR1 - "A compiler directive line is not affected by
      *>   the replacing action of a COPY statement or a REPLACE
      *>   statement." Exercised for the 2002 COPY REPLACING operand
      *>   forms word-1 and literal-3 (§2002:7.1.2.1; "Each matched
      *>   occurrence of pseudo-text-1, text-1, word-1, literal-3, or
      *>   partial-word-1 in the library text is replaced",
      *>   §2002:7.1.2.3 GR8), for pseudo-text against the 2002
      *>   FLAG-85 and FLAG-NATIVE-ARITHMETIC directive lines, and for
      *>   REPLACE against a DEFINE line.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   W1-TEXT-1      word-1 CVW BY word-2 DNW rewrites the source
      *>                  line MOVE 1 TO CVW in G2CDW (CVW is no data
      *>                  item: an unreplaced MOVE fails the compile).
      *>   W1-DIR-KEPT    G2CDW's >>DEFINE CVW AS 1 is not rewritten, so
      *>                  CVW is defined (§2002:7.2.7.3.3 GR1); a
      *>                  rewritten line defines DNW instead and prints
      *>                  W1-DIR-REPLACED.
      *>   W1-DNW-UNDEF   and DNW is not a compilation variable.
      *>                  (==ALL ON== BY ==BOGUS== leaves G2CDW's
      *>                  >>FLAG-85 ALL ON and its FLAG-NATIVE-ARITHMETIC
      *>                  ON line alone: a rewritten >>FLAG-85 BOGUS is
      *>                  malformed and fails the compile.)
      *>   L3-TEXT-NEW    literal-3 "OLD" BY literal-4 "NEW" rewrites
      *>                  the DISPLAY in G2CDL.
      *>   L3-DIR-KEPT    G2CDL's >>DEFINE CVL AS "OLD" is not rewritten,
      *>                  so CVL references "OLD" (§2002:7.2.9.3 GR7).
      *>   R-TEXT-3       REPLACE ==1== BY ==3== rewrites DISPLAY 1.
      *>   R-DIR-KEPT     but not the >>DEFINE RV AS 1 line, so RV = 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DNW PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           MOVE 0 TO DNW
           COPY G2CDW REPLACING CVW BY DNW
                                ==ALL ON== BY ==BOGUS==
                                ==ON== BY ==BOGUS==.
           DISPLAY "W1-TEXT-" DNW
       >>IF CVW IS DEFINED
           DISPLAY "W1-DIR-KEPT"
       >>ELSE
           DISPLAY "W1-DIR-REPLACED"
       >>END-IF
       >>IF DNW IS DEFINED
           DISPLAY "W1-DNW-DEFINED"
       >>ELSE
           DISPLAY "W1-DNW-UNDEF"
       >>END-IF
           COPY G2CDL REPLACING "OLD" BY "NEW".
       >>IF CVL = "OLD"
           DISPLAY "L3-DIR-KEPT"
       >>ELSE
           DISPLAY "L3-DIR-REPLACED"
       >>END-IF
           REPLACE ==1== BY ==3==.
       >>DEFINE RV AS 1
           DISPLAY "R-TEXT-" 1
           REPLACE OFF.
       >>IF RV = 1
           DISPLAY "R-DIR-KEPT"
       >>ELSE
           DISPLAY "R-DIR-REPLACED"
       >>END-IF
           STOP RUN.
