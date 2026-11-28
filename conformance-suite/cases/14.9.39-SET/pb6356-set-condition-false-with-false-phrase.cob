*>--- conformance-case
*> id: pb6356-set-condition-false-with-false-phrase
*> rules: [SR-14.9.39.3-7]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6356. SET condition-name TO FALSE is a COBOL-2002 construct. §2002:14.8.35.2 SR7: "If the FALSE phrase is
*> specified, the FALSE phrase shall be specified in the VALUE clause of the data description entry for condition-name-1."
*> (the same rule is §2014:14.9.35.2 SR7 and §2023:14.9.39.3 SR7; each checked with cite.py --check.) W-ON writes the
*> phrase, so SR7 is met at every edition from 2002 on.
*> §2002:14.8.35.3 GR5: TRUE places "the literal in the VALUE clause associated with condition-name-1" in the conditional
*> variable; GR6: FALSE places "the literal in the FALSE phrase of the VALUE clause associated with condition-name-1".
*>   SET W-ON TO TRUE   -> W = 1
*>   SET W-ON TO FALSE  -> W = 0
*> Fails if the statement is refused despite the phrase, or if FALSE places anything but its own literal.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6356OK.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 5.
   88 W-ON VALUE 1 WHEN SET TO FALSE 0.
PROCEDURE DIVISION.
MAIN-PARA.
    SET W-ON TO TRUE.
    DISPLAY "TRUE=" W.
    SET W-ON TO FALSE.
    DISPLAY "FALSE=" W.
    STOP RUN.
