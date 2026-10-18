*>--- conformance-case
*> id: pb2533-sd-reserved-word-clause
*> rules: [FMT-13.4.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2533 - a RESERVED word written in the CLOSED ISO 13.4.6.2 general format is refused BY
      *> NAME, exactly as an ordinary word run is (pb829-sd-unrecognized-clause).  The format prints
      *>   SD file-name-1 [ record-clause ] .
      *>   (cite.py --check 13.4.6.2 "[ record-clause ] ." -> OK, General format)
      *> and the GLOBAL clause may be specified only in the entries 13.18.27.3 SR1 lists, one of which is
      *> "A file description entry" (cite.py --check 13.18.27.3 "A file description entry" -> OK, 1) d)) -
      *> an SD is not one.  The compiler always refused this program, but with a bare parse error
      *> ("COBOL0307: unexpected 'GLOBAL'. A period may be missing ..."): the error production that names a
      *> closed format's residue begins at an IDENTIFIER, so a reserved word never entered it.  The parser's
      *> error strategy now reads the same ClosedFormats row the post-parse refusal reads and reports
      *> COBOLNET1970 naming the format.  Refused at every edition: GLOBAL is reserved from COBOL-85 on and
      *> the format is closed in every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2533SD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb2533sd.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SF GLOBAL.
       01 SREC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
