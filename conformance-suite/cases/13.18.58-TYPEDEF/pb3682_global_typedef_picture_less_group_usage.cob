*>--- conformance-case
*> id: pb3682_global_typedef_picture_less_group_usage
*> rules: [GR-13.18.58.4-3, GR-13.18.60.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - 13.18.58.4 GR3: "The GLOBAL clause applies to the scope of the type-name" - a contained program copies the
      *> container's declaration, which the container's own bind has completed already. The group's picture-less BINARY-LONG
      *> members keep their usage through that second copy: both programs' subjects hold two members, each its own value.
      *> Each leg can fail: the copy is refused for a PICTURE it must not carry, or loses a member.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF GLOBAL USAGE BINARY-LONG.
          05 A.
          05 B.
       01 W TYPE TG.
       01 N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 3 TO A OF W
           MOVE 4 TO B OF W
           CALL "P3682LI"
           ADD A OF W TO B OF W GIVING N
           DISPLAY N
           STOP RUN.
           IDENTIFICATION DIVISION.
           PROGRAM-ID. P3682LI.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 X TYPE TG.
           01 M PIC 9(2).
           PROCEDURE DIVISION.
               MOVE 5 TO A OF X
               MOVE 6 TO B OF X
               ADD A OF X TO B OF X GIVING M
               DISPLAY M
               GOBACK.
           END PROGRAM P3682LI.
           END PROGRAM P3682L.
