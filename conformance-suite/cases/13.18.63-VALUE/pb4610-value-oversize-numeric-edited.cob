*>--- conformance-case
*> id: pb4610-value-oversize-numeric-edited
*> rules: [SR-13.18.63.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR4: "Alphanumeric literals in the VALUE clause of an elementary item shall not exceed
      *> the size indicated by an explicit PICTURE clause."  A numeric-edited item of usage DISPLAY is of
      *> class alphanumeric (8.5.2.1), so SR7 admits an alphanumeric literal for it and this sentence measures
      *> that literal against the EDITED picture: ZZ9 is three character positions and "ABCDEF" is six.
      *> THE SENTENCE IS NOT A COBOL-2023 RULE.  It is the same in ISO/IEC 1989:2002 (SR4), the 2014 FCD (SR4)
      *> and 2023; Annex E.2 item 27 describes the content of a literal that fits, not this length rule.
      *> kb/Work PB4610: the compiler refused this program at 2023 only (COBOL1570) and compiled it clean at
      *> 2002 and 2014, where the literal was silently truncated.  1985: no text is held, the 2002 Annex F.1
      *> list of 1985-to-2002 changes names no VALUE-literal size change, and the alphanumeric arm of the same
      *> sentence (COBOL1740) is already refused at 85, so this is refused at every edition too.
      *> The conforming boundary - a literal of exactly the edited size - is pinned by
      *> pb4610_value_size_numeric_edited.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4610N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC ZZ9 VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY NE
           STOP RUN.
