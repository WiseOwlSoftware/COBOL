*>--- conformance-case
*> id: pb4304_picture_string_50_2002
*> rules: [SR-13.18.40.3-4]
*> constructs: [picture-string-50-2002]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4304 - ISO 13.18.40.3 SR4: "The maximum number of characters allowed in
      *> character-string-1 is 63." (the 2023 text; python scripts/spec/cite.py --check 13.18.40.3
      *> "The maximum number of characters allowed in character-string-1 is 63" -> OK).  The
      *> number is the EDITION's: 30 at COBOL-85, 50 in the 2002 text's own SR4 (Annex F.2 item 94,
      *> "increased from 30 to 50"), 63 from COBOL-2014 (VCR row 7.46, constructs row picture-string-50-2002).
      *> Refused at COBOL-85 (bound 30): pb4304-picture-string-31-at-85.
      *> The item below is 50 written characters: the boundary of the edition that introduced
      *> the bound, so the whole picture is 50 character positions (13.18.40.4 GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4304PICTURESTRING502002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
           MOVE ALL "Q" TO B.
           DISPLAY "SIZE=" FUNCTION LENGTH(B).
           STOP RUN.
