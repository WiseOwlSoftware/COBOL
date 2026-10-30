*>--- conformance-case
*> id: pb3245-define-literal-unterminated
*> rules: [FMT-8.3.3.2.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3245 - a compiler-directive operand is lexed apart from the
*> compilation group (kb/Work PB1616), and its literal is held to the same
*> rule: ISO 8.3.3.2.2 prints an alphanumeric literal between an opening
*> and a closing quotation symbol, and 6.4.2 lets one stay open at the end
*> of a line only when "terminated by a floating continuation indicator".
*> The >>DEFINE value below never closes; it is refused with the code the
*> same literal draws in the program text, COBOL5563 (it was the generic
*> COBOL1619 "malformed compile-time expression").
>>DEFINE PB3245-GREETING AS "HELLO
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245DEF.
PROCEDURE DIVISION.
    STOP RUN.
