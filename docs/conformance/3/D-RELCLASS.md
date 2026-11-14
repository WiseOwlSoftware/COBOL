- **D-RELCLASS — a relation outside §2023:8.8.4.2.1's "Comparisons are defined for the following:" list is refused
  (kb/Work PB1468).** The list is closed, and two of its limits are class/usage/form facts of the source, so §2023:4.2.2
  ("This warning mechanism shall indicate violations of such rules") reaches them; the severity is ours. **§2023:8.8.4.2.5**
  ("The numeric integer operand shall be an integer literal or an integer numeric data item of usage display or
  national"): a non-integer, COMPUTATIONAL, packed-decimal or floating-point item, a non-integer literal, an arithmetic
  expression or a numeric, integer or index intrinsic function-identifier against an operand of class alphanumeric,
  alphabetic or national is an **ERROR, COBOL2532**, and `--permissive` keeps it as a warning evaluated as the
  character comparison of the value's text — the DA6 disposition every §2023:8.8.1.1 / §2023:15.3 class screen follows. **An
  INTEGER function-identifier is no longer admitted as the integer operand (owner 2026-10-25, kb/Work PB3691; the first
  wave of this determination admitted it as the Table-16 Integer row of the A.1 item 92 determination, which moves such
  a function to an alphanumeric receiver in its literal text form): see D-FNUSE's relation-condition paragraph.**
  **§2023:8.8.4.2.13** ("Relation tests
  may be made only between" two index-names; an index-name and a numeric data item or numeric literal; an index data
  item and an index-name or another index data item) is an **unconditional ERROR, COBOL2533** — the pairs outside
  it have no comparison to keep under `--permissive` (an index-name against an alphanumeric operand aborted the run
  unit). The figurative ZERO opposite an index-name is row 2's numeric literal (§2023:8.3.3.6.4 GR4, "the numeric value
  '0'"); an arithmetic expression opposite an index-name is not "a numeric data item or numeric literal" and is
  refused. **A numeric or integer FUNCTION-IDENTIFIER is row 2's numeric data item, folded or not (kb/Work PB1662):**
  §2023:8.4.3.2.1 — "A function-identifier references the unique data item that results from the evaluation of a function"
  — and §2023:15.2 items 4 and 5 (class and category numeric) make it one, so `IF I1 = FUNCTION LENGTH(X)` (a value the
  binder folds), `IF I1 = FUNCTION INTEGER(3.7)` and `IF FUNCTION SQRT(9) = I1` are all defined; the first wave of this
  determination refused the unfolded function and admitted the folded one, so one rule drew two answers by what the
  optimizer could compute (the fold now keeps its origin, `BoundNumericLiteral.FunctionValue`, and BOTH are the item the
  function references). A COUNTER REGISTER is a numeric data item likewise: LINAGE-COUNTER "references a temporary unsigned integer data item
  of class and category numeric" (§2023:8.4.3.14.4 GR1), and PAGE-COUNTER and LINE-COUNTER "reference temporary unsigned
  integer data items of class and category numeric" (§2023:8.4.3.15.4 GR1). Implemented in `RelationComparability` at the one relation checkpoint; pinned by
  `conformance:RelationComparabilityTests`, `conformance:wiseowl/cases/8.8.4.2-Simple-relation-conditions/pb1468_relation_defined_pairs`,
  `conformance:cases/_unassigned/pb1662_index_name_against_function`,
  `conformance:cases/8.8.4.2-Simple-relation-conditions/pb1468-relation-numeric-not-integer` and `conformance:cases/8.8.4.2-Simple-relation-conditions/pb1468-relation-index-pair`.
