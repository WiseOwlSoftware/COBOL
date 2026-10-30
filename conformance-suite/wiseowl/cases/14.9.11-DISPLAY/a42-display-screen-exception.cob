*>--- conformance-case
*> id: a42-display-screen-exception
*> rules: [FMT-14.9.11.2, GR-14.5.3.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.11.2 Format 2 (screen) - the UNPOSITIONED shape with the exception phrases (item 9). The printed
      *> format brackets the AT phrase and the [| ON EXCEPTION | NOT ON EXCEPTION |] group independently, so
      *> DISPLAY screen-name-1 ON EXCEPTION ... is Format 2 with no AT, LINE or COLUMN phrase.
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed, so the statement
      *> draws the named COBOL1707. Why it can fail: the exception phrases were bound to the positioning
      *> phrase, so this shape drew "unexpected 'ON'" (COBOL0307, then COBOL2269) and COBOL0901 for END-DISPLAY.
      *> ISO 14.5.3.3: no enclosing statement may take the phrases here, so they are this DISPLAY's.
      *> Witness for kb/Work PB1528.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42DSP3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X FROM WS-X.
       PROCEDURE DIVISION.
           DISPLAY SG
               ON EXCEPTION DISPLAY "E"
               NOT ON EXCEPTION DISPLAY "N"
           END-DISPLAY.
           STOP RUN.
