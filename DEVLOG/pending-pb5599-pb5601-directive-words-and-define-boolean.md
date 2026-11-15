## PB5599, PB5601: the compiler-directive words are each edition's own, and a boolean expression in a DEFINE is a 2023 format

Two defects of the conditional-compilation stage, both found by the 2002 read, both reproduced on a Debug build at the
base before any change.

**PB5599.** `CompilerDirectiveWords` held one table, the §2023:8.12 one, and answered `IsReserved(word)` at every edition.
At `--std 2002` that refused `>>DEFINE PUSH AS 1` (COBOL2650) and `>>TURN EC-I-O PUSH CHECKING ON` (COBOL0718), words
§2002:8.12 does not print, and accepted `>>DEFINE FLAG-85 AS 1`, which §2002:8.12 does. The three printed tables differ:
§2014:8.12 adds `<>`, FLAG-02 and MOVE-TO-SAME-NAME to §2002:8.12's 70 words; §2023:8.12 adds 27 more and drops
FLAG-85 and FLAG-NATIVE-ARITHMETIC. The fix is one table whose every word carries the window of editions that print it
(`IsReserved(word, edition)`, `Of(edition)`), the three callers passing their edition, nothing reserved below 2002 (no
directives). Exception-names are reserved from the edition that introduced them (`EcInfo.IntroducedIn`).
`CompilerDirectiveWordsDriftTests` now reads all three printed tables and holds each equal to the code, and holds every
catalog directive's word reserved exactly where its construct row exists (the hand-kept `removedIn2023` list is gone).
Goldens: `pb5599_define_name_push`, `pb5599_define_name_flag_85`, `pb5599_turn_file_name_push`,
`pb5599_turn_file_name_flag_85`, each with a refusal at the other edition. Rows: `2002-SR-7.2.19.2-1` CONFORMS;
`2002-SR-7.2.2-6` PARTIAL (GAP 2258 to 2257), the remainder being the 2002 exception-name set (`EcInfo.IntroducedIn`
holds names §2002:14.5.12.1 lacks), so the note stays open.

**PB5601.** `>>DEFINE V AS B"1100" B-OR B"0011"` compiled at 2002 and 2014. §2002:7.2.9.1 and §2014:7.3.9.1 print
arithmetic-expression-1, literal-1, PARAMETER and OFF; boolean-expression-1 is §2023:7.3.11.2's addition. The existing
boolean-operator gate asks when B-OR was introduced (2002), not whether the DEFINE format admits an expression. New
construct row `define-boolean-expression-2023`, asked by `DirectiveDiag.GateDefineBooleanExpression` from the DEFINE value
arm only (EVALUATE, WHEN and constant conditional expressions admit a boolean expression from 2002, §2002:7.2.6); the
refused DEFINE defines nothing. Golden `pb5601_define_boolean_expression` (2023) refused at 85, 2002 and 2014. Two older
`accept: [2002]` cases had written the 2023 form (`pb1367_define_general_format_alternatives`, `directive_expressions`);
both now write the boolean literal B"1111", which is literal-1.

Self-review: architecture (one table, the window beside the word, no second list; the DEFINE gate is a construct row, not a
hand edition test), full code (each quoted rule checked with `cite.py --check`; the COBOL2650 and COBOL0718 message text
still cites §2023 at every edition, left for a note), performance (the table is a dictionary built once), duplication
(the catalog drift test replaced a hand list). No diagnostic code was allocated or used.
