*>--- conformance-case
*> id: pb4589-cobol-words-substituted-receive-synonym-is-no-user-word
*> rules: [GR-7.3.10.4-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4589): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional."
*>   cite.py: OK  §7.3.10.4 4)  (General rules)
*> 8.3.2.1 rule 1: "Reserved words shall not be used as user-defined words or system-names."
*>   cite.py: OK  §8.3.2.1 1)  (General)
*> RCV stands where the reserved word RECEIVE stood (8.9 reserves it at COBOL-2023), so it is not a user-defined word:
*> 01 RCV names no data item and the program is refused (COBOL0901). The source was accepted, the data item
*> named RECEIVE, because the directive had withdrawn RECEIVE from the reservation the synonym was asked of.
>>COBOL-WORDS SUBSTITUTE "RECEIVE" BY "RCV"
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4589RV.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 RCV PIC X.
PROCEDURE DIVISION.
    MOVE "A" TO RCV
    STOP RUN.
