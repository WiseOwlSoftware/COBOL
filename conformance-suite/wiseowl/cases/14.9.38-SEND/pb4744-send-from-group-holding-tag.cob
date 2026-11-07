      *>--- conformance-case
      *> id: pb4744-send-from-group-holding-tag
      *> rules: [GR-14.9.38.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND FROM a strongly-typed group that holds a message tag. Expected: COBOL6460. 14.9.38.4 GR4: "The message contains
      *> the content of data-name-1" (identifier-1 in the Q2 reading, DESIGN-message-control.md): the content of a group
      *> includes the tag it holds, but a tag lives in a managed slot of its group and is no part of the group's storage
      *> image (A.1 item 212), which is what travels. The slots are not carried beside the image yet, so the statement is
      *> refused rather than lose the tag silently (kb/Work PB4744's lead).
      *>   cite.py: OK  14.9.38.4 4)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(3).
          05 TGT USAGE MESSAGE-TAG.
       01 G1 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM G1 RETURNING MT
           END-SEND.
           STOP RUN.
