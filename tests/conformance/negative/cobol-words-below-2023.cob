*> reject-at: 85 2002 2014
*> ISO 1989:2023 7.3.10 - the >>COBOL-WORDS directive is a COBOL-2023 addition (Annex E.3.3
*> item 12): below --std 2023 the directive word is rejected by the introduction gate.
       >>COBOL-WORDS RESERVE "FOO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWNEG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           STOP RUN.
