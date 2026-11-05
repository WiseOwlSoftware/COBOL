*>--- conformance-case
*> id: message-tag-move-corresponding
*> rules: [GR-14.7.6-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 14.7.6 GR4: "Neither data item contains an OCCURS, REDEFINES, or RENAMES clause or is of
      *> class index, message-tag, object, or pointer." (cite.py --check 14.7.6 OK, rule 4.) A pair of subordinate items one
      *> of which is a message tag is therefore no corresponding pair, and MOVE CORRESPONDING leaves the tag alone while it
      *> moves the numeric pair. G1 and G2 are strongly typed groups of one type holding a tag (the placement 13.18.60.3 SR14
      *> permits). X OF G2 receives 5; the tag stays NULL.
      *> Derived: 5, then TAG NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTCORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 X PIC 9.
          05 T USAGE MESSAGE-TAG.
       01 G1 TYPE TG.
       01 G2 TYPE TG.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO X OF G1
           MOVE CORRESPONDING G1 TO G2
           DISPLAY X OF G2
           IF T OF G2 = NULL
              DISPLAY "TAG NULL"
           END-IF
           STOP RUN.
