*>--- conformance-case
*> id: pb5988-picture-no-digit-position-cites-1985
*> rules: [SR-13.18.40.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5988 - THE 1985 CITATION OF THE "NO DIGIT POSITION" REFUSAL. ANSI X3.23-1985 5.9.6 (Precedence
      *> Rules) says: "At least one of the symbols 'A', 'X', 'Z', '9', or '*' or at least two occurrences of one of
      *> the symbols '+', '-', or 'cs' must be present in a PICTURE character-string." A lone '$' is one
      *> occurrence of 'cs' and no other listed symbol, so the string is illegal at --std 85, and the refusal
      *> names 5.9.6, the paragraph that states the rule, not 5.9.4 GR3a (which defines a numeric item's symbols
      *> and its 1 to 18 digit positions). The refusal itself is pb529-picture-no-digit-position's; this test
      *> pins the 1985 text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5988NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC $.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
