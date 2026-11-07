      *>--- conformance-case
      *> id: pb4744-send-from-group-holding-tag
      *> rules: [GR-14.9.38.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND FROM a strongly-typed VARIABLE-LENGTH group (an OCCURS DYNAMIC table) that holds a message tag. Expected: COBOL6460.
      *> 14.9.38.4 GR4: "The message contains the content of" identifier-1, and a fixed group's tags travel beside its image as slots (send-group-holding-tag);
      *> a variable-length group is sent as its contiguous current-extent image, whose receiving half (FromContiguousImage) has no tag-skipping form yet, so the
      *> statement is refused rather than lose the tag silently (kb/Work PB4744's lead).
      *>   cite.py: OK  14.9.38.4 4)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 TG IS TYPEDEF STRONG.
          05 TGT USAGE MESSAGE-TAG.
          05 TGI PIC X OCCURS DYNAMIC.
       01 G1 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM G1 RETURNING MT
           END-SEND.
           STOP RUN.
