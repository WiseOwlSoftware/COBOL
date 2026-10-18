*>--- conformance-case
*> id: pb2533-identification-reserved-word-paragraph
*> rules: [FMT-11.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2533 - a RESERVED word written as a paragraph header of the CLOSED ISO 11.2.1 general
      *> format is refused BY NAME, as an ordinary word is (pb829-identification-unrecognized-paragraph).  The
      *> list is a list of PARAGRAPHS, so the code is COBOLNET1971 and the noun "paragraph".  It used to be a
      *> bare "COBOL0001: unexpected 'GLOBAL'": the error production that names a closed format's residue
      *> begins at an IDENTIFIER, so a reserved word never entered it.  Refused at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2533ID.
       GLOBAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
