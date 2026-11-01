*>--- conformance-case
*> id: pb4304-picture-string-51-below-2014
*> rules: [SR-13.18.40.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4304 - ISO 13.18.40.3 SR4 (63 in the 2023 text; 50 in the 2002 text's own SR4,
      *> 30 at COBOL-85 - VCR row 7.46).  51 written characters: one over the COBOL-2002 bound of 50 (and over the COBOL-85 bound of 30).
      *> It is a LENGTH error, so also under --permissive.  The positive twin is
      *> pb4304_picture_string_63_2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4304PICTURESTRING51BELOW2014.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
