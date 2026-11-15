      *>--- conformance-case
      *> id: pb5601_define_boolean_expression
      *> rules: [SR-7.3.11.3-4, GR-7.3.11.4-7]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5601 - a boolean EXPRESSION as the DEFINE operand.
      *> §2023:7.3.11.2 prints boolean-expression-1 beside arithmetic-
      *> expression-1 and literal-1 in the DEFINE format; §2002:7.2.9.1
      *> and §2014:7.3.9.1 print only arithmetic-expression-1, literal-1,
      *> PARAMETER and OFF, so below 2023 the directive is refused
      *> (COBOL0900, wiseowl/diagnostics/pb5601_define_boolean_expression.json).
      *>   cite.py: OK  §2023:7.3.11.4 GR7  "If boolean-expression-1 is
      *>   specified, boolean-expression-1 is evaluated according to 7.3.7"
      *> B-OR sets a bit when either operand has it: B"1100" B-OR B"0011"
      *> is B"1111". B-NOT inverts each bit: B-NOT B"0101" is B"1010".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5601OK.
       PROCEDURE DIVISION.
       >>DEFINE V AS B"1100" B-OR B"0011"
       >>DEFINE W AS B-NOT B"0101"
       >>IF V = B"1111"
           DISPLAY "V".
       >>END-IF
       >>IF W = B"1010"
           DISPLAY "W".
       >>END-IF
           STOP RUN.
