- **D-INTEGER-ARGUMENT-SCREEN — an intrinsic function's integer argument: what the compiler refuses, accepts, and checks at run time
  (kb/Work PB5889, the ruling of 2026-11-21).** This entry speaks for every edition. The rule is on the program: "An arithmetic
  expression that will always result in an integer value or an integer data item shall be specified" (`§2023:15.3 AR6`; at
  COBOL-85 `§1989:A.2.2 AR4`, "An arithmetic expression which will always result in an integer value must be specified"). A form
  that always results in an integer is conforming source, so the compiler must accept it (`§2023:4.2.2` "An implementation shall
  accept the syntax and provide the functionality for all standard language elements"; `§1985:I.1.5.2` "must fully support all the
  language elements of that subset"); whether a breach is detected at compile time is the implementor's (`§2023:4.2.2` "it is left
  to the implementor's discretion to determine what is syntactically distinguishable"; at 1985 `§1985:I.1.6` "The results of
  violating the formats or rules of Standard COBOL are undefined"). **Determination:** the screen (COBOL1627) has three outcomes.
  (1) PROVEN: the argument's value, as a polynomial in its terms with every intermediate exact in the arithmetic in force, is an
  integer for every value of its terms; accepted. (2) REFUTED: the compiler names a value of the operands (the expression's data
  items, each free to hold that value) at which the exact value is not an integer, or the value is a non-integer constant plus a
  part that is an integer whatever value its integer terms take; refused, the message naming the value (`at N = 1 it is 1/3`). A
  numeric function written as the integer argument itself is refused by its type (`§2023:8.4.3.2.3 SR11`, `§1989:A.2.3 GR2`), and
  so is a data item or literal that is not an integer. (3) UNDECIDED: neither; accepted, and the value is checked when the argument
  is evaluated. A value of a FUNCTION is never taken as a counterexample, because the compiler does not model a function's range
  (`FUNCTION FACTORIAL (N + 2) / 2` is never odd), so `S2 - FUNCTION MAX (S2)`, `FUNCTION MEAN (N, N) + 1`, `FUNCTION ORD (A) / 2`
  and a quotient by a function are undecided. **The run-time check:** from COBOL-2002 a value that is not an integer is an incorrect
  argument, EC-ARGUMENT-FUNCTION (`§2023:15.3 AR14`, "the EC-ARGUMENT-FUNCTION exception condition is set to exist"), under the
  edition's checking rules; with checking off the truncated integer is the result (A.1 item 124). COBOL-85 has no exception
  conditions; the Correction Amendment's clarification of the intrinsic function module "allows an implementation of amended third
  Standard COBOL to terminate processing" when an argument's value does not comply (`§1993:p.B-46`), so at `--std 85` the run unit
  is terminated with a message naming the function, the argument and the value. GnuCOBOL 3.2 accepts all four probe forms and
  checks nothing (`MOD (N / 3, 2)` prints 1.3333 even under `-debug`), so it gives no guidance past the text. Witnesses:
  `conformance:cases/15.3-Arguments/pb5889_function_meaning_identities`, `conformance:cases/15.3-Arguments/pb5889_run_time_check_85`,
  `conformance:cases/15.3-Arguments/pb5889_run_time_check`, `conformance:cases/15.3-Arguments/pb5889_run_time_check_unchecked`,
  `unit:IntrinsicTypeSixPowerShapesDriftTests.Shapes_AreRefusedOrAdmittedAsRecorded`.
