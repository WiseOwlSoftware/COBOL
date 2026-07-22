*> reject-at: 2023
*> ISO 1989:2023 7.3.10.4 GR5 - a >>COBOL-WORDS RESERVE'd word shall not be used as a
*> user-defined word: FOO is reserved for this group, so 01 FOO is rejected (COBOLNET0901).
       >>COBOL-WORDS RESERVE "FOO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWRSV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FOO PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FOO.
           STOP RUN.
