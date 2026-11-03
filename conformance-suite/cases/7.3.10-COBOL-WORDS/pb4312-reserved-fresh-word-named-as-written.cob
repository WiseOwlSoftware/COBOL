*>--- conformance-case
*> id: pb4312-reserved-fresh-word-named-as-written
*> rules: [GR-7.3.10.4-5, SR-7.3.10.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4312. ISO 7.3.10.4 GR5: "When the RESERVE option is used, then the content of literal-6 shall not be
*> used as a user-defined word within this compilation group." literal-6 is written "café" and the data-name CAFÉ:
*> ISO 8.1.3.2 4) ("Extended letters in user-defined words are subject to the following rules") folds the two to one
*> word, so 01 CAFÉ is refused (COBOL0901). The refusal names the word the source writes, 'CAFÉ' (its basic letters
*> upper-cased), never the fold's spelling 'CAFé'. It fails if the message prints a spelling the user did not write.
>>COBOL-WORDS RESERVE "café"
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4312R.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CAFÉ PIC X.
PROCEDURE DIVISION.
MAIN-P.
    STOP RUN.
