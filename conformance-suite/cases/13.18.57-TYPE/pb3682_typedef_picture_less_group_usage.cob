*>--- conformance-case
*> id: pb3682_typedef_picture_less_group_usage
*> rules: [GR-13.18.57.4-1, GR-13.18.60.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - 13.18.60.4 GR1: "If the USAGE clause is specified or implied at a group level, it applies only to each elementary
      *> item in the group" (cite.py: OK  13.18.60.4 1)), and 13.18.57.4 GR1: the effect of a TYPE clause is "as though the data
      *> description identified by type-name-1 had been coded in place" (cite.py: OK  13.18.57.4 1)). A group whose members are
      *> picture-less BINARY-LONG items is one such description, so TG and the TYPE subject W both have the two members A and B, each
      *> an integer item (13.18.60.4 GR12) holding what is moved into it. The TYPEDEF used to be refused (COBOLNET2191, COBOLNET0881),
      *> and the subject, were it copied before the group's profile was shed, held no members at all (A OF W was undefined).
      *> Each leg can fail: the declaration is refused; W has no member A or B; the two members share one value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE BINARY-LONG.
          05 A.
          05 B.
       01 W TYPE TG.
       01 N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 3 TO A OF W
           MOVE 4 TO B OF W
           ADD A OF W TO B OF W GIVING N
           DISPLAY N
           STOP RUN.
