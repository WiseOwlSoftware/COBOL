*>--- conformance-case
*> id: pb4890-screen-entry-named-by-a-reserved-word
*> rules: [SR-13.17.3-1, FMT-13.17.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4890. ISO 13.17.2 prints the screen description entry as level-number [ entry-name-clause ], and the
*> entry-name clause is 13.18.20, the rule the data description entry reads too (13.18.20.4 GR1: "The word FILLER may
*> be used to name a data, report, or screen item"). 8.9 reserves CRT from COBOL-2002, and 8.3.2.1 1) says "Reserved
*> words shall not be used as user-defined words or system-names", so 01 CRT LINE 1. names a screen item with a
*> reserved word: COBOL0901 names 8.9, exactly as 01 CRT PIC X. does in WORKING-STORAGE. The screen facility is
*> declined (Annex A.4.2, docs/CONFORMANCE.md section 5), so the section also draws COBOL1560.
*> Fails if the name slot admits no reserved word and the section loop reports the entry as a missing CONSTANT
*> (COBOL5506 "expected CONSTANT") in place of the named reserved-word refusal. Below COBOL-2002 CRT is a user word:
*> conformance:cases/13.17-Screen-description-entry/pb4890-screen-entry-named-by-a-free-word-at-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4890S.
       DATA DIVISION.
       SCREEN SECTION.
       01 CRT LINE 1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
