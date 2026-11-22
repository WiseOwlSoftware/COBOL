      *>--- conformance-case
      *> id: pb4978_debug_all_subscript_85
      *> rules: [1985-GR-XV.3.2.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4978. At COBOL-85 an elementary item under a table is
      *> referenced as a whole table by a function argument subscripted
      *> ALL (§1985:IV.4.3.8.2.3 SR7: "The subscript ALL may be used only
      *> when the subscripted identifier is used as a function argument";
      *> §1989:p.A-28: "the effect is as if each table element associated
      *> with that subscript position were specified"), so SEARCH
      *> identifier-1 is not the only whole-table reference there.
      *> §1985:XV.3.2.4 GR24c: "DEBUG-CONTENTS contains the content of the
      *> data item referenced by identifier-1", every occurrence of F in
      *> storage order, and GR15 makes DEBUG-CONTENTS "large enough to
      *> contain the data required": 50 positions here (10 occurrences of
      *> PIC X(5)). The second reference, F (2), is one occurrence.
      *> Expected: the first run shows occurrences 1, 2 and 10 of the
      *> whole table (AAAAA, BBBBB at 6, JJJJJ at 46); the second shows
      *> BBBBB and spaces after it.
      *> Why it can fail: a DEBUG-CONTENTS sized to one occurrence of F
      *> (the narrowing kb/Work PB4978 proposed) truncates the first run
      *> at position 5, and position 46 shows spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4978AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E OCCURS 10.
             10 F PIC X(5).
       01 X PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF F.
       DS-1.
           DISPLAY "DS [" DEBUG-CONTENTS (1:10) "] ["
               DEBUG-CONTENTS (46:5) "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           MOVE "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHHIIIIIJJJJJ"
               TO TB.
           MOVE FUNCTION MAX (F (ALL)) TO X.
           DISPLAY X.
           MOVE F (2) TO X.
           DISPLAY X.
           STOP RUN.
