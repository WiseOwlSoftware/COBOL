*>--- conformance-case
*> id: pb4361-goback-returning-index-item
*> rules: [GR-14.9.18.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4361. The GOBACK RETURNING extension moves its operand (14.9.18.4 GR2), so the operand is held to
      *> 14.9.25.3 SR1: "The class of identifier-1 or identifier-2 shall not be index, message-tag, object, or
      *> pointer." An index data item is refused as MOVE-class COBOL0809, not as the STATUS phrase COBOL1704 the
      *> program did not write. (Positive control: pb4361_goback_returning_scaled_operand.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4361.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           GOBACK RETURNING IX.
