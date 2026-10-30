*>--- conformance-case
*> id: pb1314-set-false-operand-not-condition-name
*> rules: [SR-14.9.39.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1314 - ISO 14.9.39.3 SR6: "Condition-name-1 shall be associated with a conditional variable."
*> U is a plain numeric item, so the operand of the SECOND group is refused. The refusal has always been
*> right; what this case pins is that the message quotes the phrase THAT GROUP WROTE. ISO 14.9.39.2 Format 4
*> prints `{ condition-name-1 } ... TO { TRUE | FALSE }` as one repeatable unit, so a statement may write TRUE in
*> one group and FALSE in the next (GR8 reads it as a separate SET per condition-name). The diagnostic used to
*> hard-code `TO TRUE`, so `U TO FALSE` was reported as "SET 'U' TO TRUE", a statement nobody wrote.
*> The first group is a conforming TRUE group, so a message that took its phrase from the first group, or from
*> the statement, would still misquote U.
*> cite.py: OK  14.9.39.3 6)
*> Refused COBOL1639 at every edition; at 85 the FALSE phrase draws its introduction gate as well.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1314NF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 V PIC 9.
   88 V-ON VALUE 1.
01 U PIC 9.
PROCEDURE DIVISION.
MAIN-P.
    SET V-ON TO TRUE U TO FALSE.
    STOP RUN.
