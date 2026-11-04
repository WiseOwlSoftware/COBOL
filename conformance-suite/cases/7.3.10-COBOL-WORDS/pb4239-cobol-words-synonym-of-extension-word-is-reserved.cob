*>--- conformance-case
*> id: pb4239-cobol-words-synonym-of-extension-word-is-reserved
*> rules: [GR-7.3.10.4-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.4 GR2 (cite.py --check 7.3.10.4 "shall be treated as a synonym for" -> OK 2)): JS is a synonym
*> for JSON, a reserved word of this implementation (4.2.10 3); 8.3.2.1 1)), so JS is reserved as JSON is and cannot be a
*> data-name. The directive itself is legal (JSON is a reserved word, SR3; JS is no reserved word, SR4); the refusal is at
*> the use, and its sentence names the reservation and the synonym as written (kb/Work PB4239, PB4573).
       >>COBOL-WORDS EQUATE "JSON" WITH "JS"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4239E7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 JS PIC X VALUE "C".
       PROCEDURE DIVISION.
           DISPLAY JS.
           STOP RUN.
