*>--- conformance-case
*> id: pb4709-abs-parenthesized-fraction-literal
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB4709 review round 1, R1-2 (the sibling of pb4709_integer_row_scale_zero) - a parenthesized sole literal is read as the bare
      *> literal is, by its FORM: §2023:8.3.3.3.2 (cite.py --check 8.3.3.3.2 "An integer literal is a fixed-point numeric literal that
      *> contains no decimal point" OK) makes 1.0 no integer literal, though its value is integral. So ABS((1.0)) and ABS(1.0) are
      *> both the Numeric row (the per-function result tables of §2023:15.7.1 and its siblings, cite.py --check 15.7.1 OK), so each is a
      *> numeric function (§2023:15.2 item 4) whose value is a Noninteger sender, and §2023:14.9.25.3 SR10 (Table 16, cite.py --check 14.9.25.3
      *> OK) does not move it to an alphanumeric receiver - COBOL0819. The branch under review had typed the parenthesized spelling
      *> Integer by VALUE and so accepted it where the bare spelling was refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB4709PARENLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A10  PIC X(10).
       PROCEDURE DIVISION.
           MOVE FUNCTION ABS((1.0)) TO A10
           STOP RUN.
