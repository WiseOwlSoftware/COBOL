*> reject-at: 85 2002 2014 2023
      *> kb/Work R38 - 14.9.22.2 Format 4: CONVERTING's operands are {identifier-6|literal-4} TO
      *> {identifier-7|literal-5}. An ALPHABET-name is neither (a SPECIAL-NAMES name is not an
      *> identifier), so GnuCOBOL's CONVERTING-alphabet extension is not ISO in any edition. The
      *> differential's run_misc:1759 exercises exactly this; the flip to WE_REJECT is the
      *> adjudicated divergence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R38NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA IS ASCII.
           ALPHABET BETA IS EBCDIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(10) VALUE "ABC".
       PROCEDURE DIVISION.
           INSPECT X CONVERTING BETA TO ALPHA.
           STOP RUN.
