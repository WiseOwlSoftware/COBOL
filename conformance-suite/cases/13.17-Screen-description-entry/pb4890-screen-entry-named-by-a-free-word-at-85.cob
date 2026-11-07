*>--- conformance-case
*> id: pb4890-screen-entry-named-by-a-free-word-at-85
*> rules: [SR-13.17.3-1, FMT-13.17.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4890, the negative below the reservation. 8.9 adds CRT to the reserved words in COBOL-2002, so at COBOL-85
*> 01 CRT LINE 1. is an entry whose name is the user-defined word CRT (13.17.2 level-number [ entry-name-clause ]).
*> COBOL-85 has no SCREEN SECTION (a COBOL-2002 introduction) and this compiler declines the facility at every edition
*> (Annex A.4.2, docs/CONFORMANCE.md section 5), so the program is refused by the section's named refusal, COBOL1560.
*> Fails if the section draws a syntax error, or the name a reserved-word refusal, where 8.9 leaves CRT free. The
*> reserved arm is conformance:cases/13.17-Screen-description-entry/pb4890-screen-entry-named-by-a-reserved-word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4890F.
       DATA DIVISION.
       SCREEN SECTION.
       01 CRT LINE 1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
