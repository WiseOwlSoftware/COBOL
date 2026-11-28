*>--- conformance-case
*> id: pb6355-set-format4-operand-wrong-category
*> rules: [SR-14.9.39.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6355. SET Format 4's condition-name-1 "shall be associated with a conditional variable" (§2023:14.9.39.3 SR6;
*> §2014:14.9.35.2 SR6; §2002:14.8.35.2 SR6; §1985:VI.6.23.3 SR6 "must be", each checked with cite.py --check). W is a data item:
*> the name identifies a resource, so the refusal says what W IS and that the format wants a condition-name (COBOL1757, the
*> statement-operand-rule code SET's Format 3 and Format 4 switch arms raise), never COBOL1639's "identifies no resource". COBOL1639
*> stays for a name nothing declares.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6355WC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9.
PROCEDURE DIVISION.
MAIN-PARA.
    SET W TO TRUE.
    STOP RUN.
