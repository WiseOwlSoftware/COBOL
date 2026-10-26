*>--- conformance-case
*> id: pb3682_same_as_picture_less_group_usage
*> rules: [GR-13.18.49.4-1, GR-13.18.60.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - 13.18.49.4 GR1: the SAME AS clause describes the subject as though the description of data-name-1 "had been coded
      *> in place" - the group X of two picture-less BINARY-LONG members (13.18.60.4 GR1; cite.py: OK  13.18.60.4 1)) has subordinate
      *> items, so its copy V has them too, under the same names (13.18.49.4 GR2 a)). The copy was taken before the usage walk shed
      *> X's profile, so V was an ELEMENTARY item of the group's usage with no members (and a quarter of X's size).
      *> Each leg can fail: the members are missing, or they alias one another.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X USAGE BINARY-LONG.
          05 XA.
          05 XB.
       01 V SAME AS X.
       01 N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 5 TO XA OF V
           MOVE 6 TO XB OF V
           ADD XA OF V TO XB OF V GIVING N
           DISPLAY N
           STOP RUN.
