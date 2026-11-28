*>--- conformance-case
*> id: pb6259_options_initialize_index_in_redefined_group
*> rules: [GR-14.6.2.3.2-1, GR-11.9.10.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6259 - OPTIONS INITIALIZE reaches a USAGE INDEX item that is a member of a REDEFINES group exactly as it
*> reaches a standalone one. ISO 1989:2023 14.6.2.3.2 GR1 (cite.py OK) - "the storage allocated for the implied or
*> associated sections is set to the specified-fill-character"; 11.9.10.4 GR5 - the character following 'TO' creates the
*> specified-fill-character. A USAGE INDEX item occupies eight storage bytes, so each of the eight takes the fill
*> character, and the byte after it (A) takes it too.
*> OPTIONS INITIALIZE WORKING-STORAGE SECTION TO X"5A" - the fill byte 0x5A is the character 'Z'.
*>   R  PIC X(9), the whole group G (8 index bytes + the 1-byte A) -> [ZZZZZZZZZ]
*>   A  PIC X(1)                                                   -> [Z]
*> BEFORE: the member was seeded with the decimal text of the 64-bit pattern 0x5A5A5A5A5A5A5A5A, so R read 651061555
*> and A read 5.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6259OIIRG.
OPTIONS.
    INITIALIZE WORKING-STORAGE SECTION TO X"5A".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 IX USAGE INDEX.
   05 A PIC X.
01 R REDEFINES G PIC X(9).
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "R=[" R "]"
    DISPLAY "A=[" A "]"
    STOP RUN.
