*>--- conformance-case
*> id: pb4309-cobol-words-undefine-intrinsic-name
*> rules: [GR-7.3.10.4-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 7.3.10.4 GR3 (kb/Work PB4309): when UNDEFINE is specified, "any syntax requiring the use of the COBOL word that is
*> the content of literal-3 shall not be available for use in this compilation group". LENGTH is an intrinsic function
*> name (15.50), so FUNCTION LENGTH is refused here - and the refusal names the UNDEFINE that withdrew the word (COBOL5776),
*> never the sentence a misspelling draws ("not an intrinsic function of ISO/IEC 1989"), which is false of LENGTH.
       >>COBOL-WORDS UNDEFINE "LENGTH"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4309UND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION LENGTH(S).
           STOP RUN.
