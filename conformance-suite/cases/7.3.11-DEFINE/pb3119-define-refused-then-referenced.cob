*>--- conformance-case
*> id: pb3119-define-refused-then-referenced
*> rules: [FMT-7.3.11.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.11.2 general format (cite.py --check 7.3.11.2 "OVERRIDE" -> OK): the operand of a >>DEFINE is an
*> arithmetic-expression, a boolean-expression or a literal; `1 +` is none of them, so the directive is refused (kb/Work PB3119).
*> The refusal is reported ONCE, at the >>DEFINE. The >>IF lines that then name FOO, in a comparison, in an arithmetic
*> operand and in a boolean expression, add nothing: FOO is the name of a refused definition, not a name nobody wrote.
*> (That the later lines report nothing is held by ConditionalCompilationDefineTests; a harness expectation names only
*> the diagnostic that must appear.)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3119N01.
>>DEFINE FOO AS 1 +
>>IF FOO = 2
>>END-IF
>>IF FOO + 1 = 2
>>END-IF
>>IF B"1" AND FOO
>>END-IF
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
