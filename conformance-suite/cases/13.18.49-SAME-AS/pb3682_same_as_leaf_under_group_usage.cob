*>--- conformance-case
*> id: pb3682_same_as_leaf_under_group_usage
*> rules: [SR-13.18.49.3-7, GR-13.18.49.4-3, GR-13.18.60.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - 13.18.49.3 SR7 (cite.py: OK  13.18.49.3 7)): data-name-1 "shall reference an elementary item or a level 1
      *> group item", and E, with no subordinate entries, is an elementary item. Its USAGE comes from the group G
      *> (13.18.60.4 GR1), so the description SAME AS copies has no PICTURE of its own at the time the clause is expanded: the
      *> check read the PICTURE and refused E as "not elementary". 13.18.49.4 GR3 (cite.py: OK  13.18.49.4 3)): the USAGE clause of
      *> the alphanumeric group G to which data-name-1 is subordinate takes effect as though specified for the subject, so S is a
      *> BINARY-LONG item too. Each leg can fail: the clause is refused, S is a picture-less item with no usage, or S aliases E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE BINARY-LONG.
          05 E.
       01 S SAME AS E.
       01 N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 5 TO E
           MOVE E TO S
           ADD 2 TO S
           MOVE S TO N
           DISPLAY N
           MOVE E TO N
           DISPLAY N
           STOP RUN.
