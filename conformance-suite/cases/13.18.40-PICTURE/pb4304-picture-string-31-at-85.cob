*>--- conformance-case
*> id: pb4304-picture-string-31-at-85
*> rules: [SR-13.18.40.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4304 - ISO 13.18.40.3 SR4 (63 in the 2023 text; 50 in the 2002 text's own SR4,
      *> 30 at COBOL-85 - VCR row 7.46).  31 written characters: one over the COBOL-85 bound of 30.
      *> It is a LENGTH error, so also under --permissive.  The positive twin is
      *> pb4304_picture_string_50_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4304PICTURESTRING31AT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
