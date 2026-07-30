*> reject-at: 2002 2014 2023
*> ISO 14.9.4.3 SR22 - "If identifier-4 or its corresponding formal parameter is specified with a BY VALUE
*> phrase, identifier-4 shall be of class numeric, object, or pointer." GnuCOBOL accepts an alphanumeric
*> operand as an extension and silently assumes BY CONTENT - a DIFFERENT passing mode from the one written.
*> Surfaced by the pre-merge differential: this was already rejected, but by DA6's 8.8.1.1 ARITHMETIC screen,
*> because the grammar production is named arithmeticExpression. Right verdict, wrong rule quoted.
*> (BY VALUE is a COBOL-2002 introduction, so 85 rejects this earlier for a different reason.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB6BYVALUE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(4).
PROCEDURE DIVISION.
MAIN.
    CALL "PROG2" USING BY VALUE X END-CALL.
    STOP RUN.
