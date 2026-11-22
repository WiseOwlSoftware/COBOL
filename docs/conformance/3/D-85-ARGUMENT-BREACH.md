- **D-85-ARGUMENT-BREACH — an intrinsic function argument whose value breaks the function's argument rules at COBOL-85 (kb/Work
  PB6019).** This entry speaks for COBOL-85. The intrinsic function module places constraints on argument values (a FACTORIAL
  argument "must be an integer greater than or equal to zero", `§1989:A.2.14.3 AR1`; a CHAR argument "must be greater than zero
  and less than or equal to the number of positions in the collating sequence", `§1989:A.2.9.3 AR2`; a zero divisor, a date out
  of range, a text NUMVAL cannot read), and when the arguments "do not have values within the permissible range, then the result of
  such a reference is undefined" (`§1989:A.2.2 p3`; the same in `§1989:A.1.2.2`, "do not have values that comply with the
  specified constraints, then the result of such a reference is undefined"). COBOL-85 has no exception
  conditions, and the Correction Amendment's clarification of that rule "allows an implementation of amended third Standard COBOL
  to terminate processing" (`§1993:p.B-46`). **Determination:** at `--std 85` every such breach detected at run time terminates
  the run unit with a message naming the function, the argument and its value and the rule it breaks; no reference returns a
  value the implementation made up. It is the same path an integer argument that is not an integer takes
  (D-INTEGER-ARGUMENT-SCREEN), and the one place it is decided is the runtime's answer to every broken argument rule
  (`ExceptionEngine.ArgumentError` and its two text twins), keyed by the edition of the element executing, so a COBOL-85 program
  CALLed from a later-edition one terminates and a later-edition program CALLed from a COBOL-85 one raises EC-ARGUMENT-FUNCTION.
  From COBOL-2002 the text names the condition ("the EC-ARGUMENT-FUNCTION exception condition is set to exist", `§2002:15.2 AR13`,
  `§2014:15.3 AR13`, `§2023:15.3 AR14`) and A.1 item 90 is the result with checking off; nothing changes there. Before kb/Work
  PB6019 the 1985 breach returned A.1 item 90's 0 or spaces and the run went on, while a non-integer integer argument terminated it:
  one rule, two answers. Witnesses: `conformance:cases/A.2.14@1985-FACTORIAL-FUNCTION/pb6019_argument_breach_85`,
  `conformance:cases/A.2.9@1985-CHAR-FUNCTION/pb6019_argument_breach_text_85`,
  `unit:ArgumentBreachEditionTests.AtCobol85_EveryBreachTerminatesTheRunUnit_WithItsMessage`,
  `unit:ArgumentBreachEditionTests.EveryBreach_TakesTheOnePath`.
