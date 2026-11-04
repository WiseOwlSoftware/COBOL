*>--- conformance-case
*> id: pb4610-value-oversize-national-edited
*> rules: [SR-13.18.63.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR5 sentence 2: "National literals in the VALUE clause of an elementary item shall not
      *> exceed the size indicated by an explicit PICTURE clause."  A numeric-edited item of usage NATIONAL is
      *> of class national (8.5.2.1 Table 2), so SR7 admits a national literal for it and this sentence measures
      *> that literal against the EDITED picture: ZZ9 is three national positions and N"ABCDEF" is six.  The
      *> compiler measured only the alphanumeric twin (SR4, pb4610-value-oversize-numeric-edited); this program
      *> compiled clean at every edition and DISPLAY showed [ABC], the literal silently truncated
      *> (kb/Work PB4610, branch review round 1).  National items need 2002, so it is refused from 2002 on.
      *> The conforming boundary is pinned by pb4610_value_size_national_edited.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4610N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC ZZ9 USAGE NATIONAL VALUE N"ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY NE
           STOP RUN.
