*>--- conformance-case
*> id: pb6356-set-condition-false-without-phrase-gate-only
*> rules: [SR-14.9.39.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6356. W-ON writes no WHEN SET TO FALSE phrase and the statement says TO FALSE. SET condition-name TO FALSE is a
*> COBOL-2002 construct (§2002:14.8.35.2 SR7), so at --std 85 the edition gate COBOL0900 is the one refusal: the SR7 screen
*> (COBOL2049) belongs to the editions that have the phrase and says nothing at 85. From 2002 on COBOL2049 is the refusal, citing
*> that edition's own SR7 (§2002:14.8.35.2, §2014:14.9.35.2, §2023:14.9.39.3; the per-edition text is pinned in
*> SetFormat4OperandRefusalTests, which also asserts the absence of COBOL2049 at 85).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6356NG.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9.
   88 W-ON VALUE 1.
PROCEDURE DIVISION.
MAIN-PARA.
    SET W-ON TO FALSE.
    STOP RUN.
