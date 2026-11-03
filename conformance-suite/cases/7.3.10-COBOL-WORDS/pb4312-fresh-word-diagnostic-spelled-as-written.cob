*>--- conformance-case
*> id: pb4312-fresh-word-diagnostic-spelled-as-written
*> rules: [SR-7.3.10.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4312. ISO 7.3.10.3 SR4: "The content of each of these literals shall be a COBOL word" (literal-6 among
*> them), and 8.3.2.1 puts no hyphen last: "CAFÉ-" is refused (COBOL1623). The refusal names the word as written,
*> 'CAFÉ-', never the Annex C fold's 'CAFé-'. It fails if the message prints a spelling the user did not write.
>>COBOL-WORDS RESERVE "CAFÉ-"
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4312S.
PROCEDURE DIVISION.
MAIN-P.
    STOP RUN.
