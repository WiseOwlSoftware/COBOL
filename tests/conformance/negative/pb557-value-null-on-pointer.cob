*> reject-at: 2002 2014 2023
*> ISO 1989:2023 - the SIBLING one level out from 13.18.63.3 SR9, and it needs its own derivation because SR9
*> does NOT name USAGE POINTER. 13.18.63.2 format 1 takes literal-1; 8.4.3.10.1 makes NULL "a predefined
*> address of class pointer or a predefined content of class message-tag" - an identifier under 8.4.3, not a
*> literal - and 8.3.3.6.2 does not list it among the figurative constants either. No syntax rule of
*> 13.18.63.3 types a literal for a subject of class pointer: SR2 types one for numeric, SR4 for alphabetic /
*> alphanumeric / alphanumeric-edited, SR5 for national, SR6/SR7 for numeric-edited, SR10 for boolean.
*> Nothing is lost by the prohibition: 13.18.63.4 GR4 - "data items of class message-tag, class object, and
*> class pointer are initialized to null" - already gives the item this value with no clause written, which
*> tests/conformance/2002/pb557_pointer_class_initial_value_is_null pins. (COBOLNET2168)
*> MEASURED BEFORE: rc=70 with no COBOL diagnostic at all - the raw literal text reached the record-struct
*> initializer and Roslyn reported `CS0029: Cannot implicitly convert type 'string' to
*> 'CobolNet.Runtime.ManagedPointer'`, naming a generated .g.cs file.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB557PTRNULL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P USAGE POINTER VALUE NULL.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "OK"
    STOP RUN.
