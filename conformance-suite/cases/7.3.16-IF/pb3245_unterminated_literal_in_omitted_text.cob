*>--- conformance-case
*> id: pb3245_unterminated_literal_in_omitted_text
*> rules: [GR-7.3.16.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3245 - ISO 7.3.16.4 3): "If constant-conditional-expression-1
*> evaluates to FALSE, all lines of text-2 are included in the resultant
*> text and all lines of text-1 are omitted from the resultant text". The
*> omitted text-1 holds a literal its line never closes. It is not program
*> text, so the unterminated-literal rule (COBOL5563) does not reach it, and
*> the included text-2 runs: the output is OK. Fails if a stage before
*> conditional compilation refuses the open literal, or if the omitted line
*> reaches the lexer.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245OMIT.
PROCEDURE DIVISION.
>>DEFINE PB3245-FLAG AS 0
>>IF PB3245-FLAG = 1
    DISPLAY "NEVER CLOSED
>>ELSE
    DISPLAY "OK".
>>END-IF
    STOP RUN.
