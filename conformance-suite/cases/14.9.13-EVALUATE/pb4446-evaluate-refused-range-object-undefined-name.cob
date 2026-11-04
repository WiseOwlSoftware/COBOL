*>--- conformance-case
*> id: pb4446-evaluate-refused-range-object-undefined-name
*> rules: [SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4446. ISO 14.9.13.3 SR10 and Table 15 leave a range-expression object blank against the TRUE selection
*> subject, so the pair is refused (COBOL1634). The range's own operands are not the pair's: 8.4.2.1, "In order to use a
*> resource, a statement shall contain a reference that uniquely identifies that resource", is broken by UNDEFX, which
*> no declaration gives, and 4.2.2 requires the warning mechanism "to indicate violations of the general formats and
*> the explicit syntax rules". Fails if the refusal hides the COBOL1639 about UNDEFX (the refusal used to return before
*> the range was bound), or if the program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4446R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN UNDEFX THRU 3
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
