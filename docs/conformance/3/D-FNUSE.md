- **D-FNUSE — a numeric, integer, index or boolean function-identifier is not "described … as usage display or
  national", so it is no STRING character operand, no INSPECT identifier-n and no numeric integer operand opposite a
  character operand of a relation** (owner 2026-10-24 and 2026-10-25; kb/Work PB3665, PB3691,
  rules `SR-14.9.43.3-1`, `SR-14.9.22.3-2`, `GR-8.8.4.2.1-6`). §2023:14.9.43.3 SR1 requires every identifier of STRING but the POINTER to be
  *"described implicitly or explicitly as usage display or national"*, and §2023:14.9.22.3 SR2 requires *"an elementary item
  described implicitly or explicitly as usage display or national"* of INSPECT's identifier-3 … identifier-n. A
  function-identifier references *"the unique data item that results from the evaluation of a function"* (§2023:8.4.3.2.1),
  and §2023:15.2 describes that item by the function's type: an alphanumeric function has *"an implicit usage display"*
  (1)), a national function *"an implicit usage national"* (3)), a boolean function *"an implicit usage bit"* (2)); the
  numeric (4)), integer (5)) and index (6)) functions are given a class, a category and (for the first two) an
  operational sign, and **no usage**. §2023:15.2's opening sentence (a function *"may be used anywhere a sending data item
  of that class and category may be specified"*) and §2023:15.4.1 (the returned value's characteristics are the
  implementor's) leave the usage of a numeric value open, and the choice is the owner's: **WiseOwl COBOL reads the
  silence as "not described as usage display or national", so only the alphanumeric and national functions are
  admitted** — `STRING FUNCTION UPPER-CASE(S) …`, `INSPECT … FOR ALL FUNCTION LOWER-CASE(P)` — and `STRING FUNCTION
  INTEGER(N) …`, `STRING … DELIMITED BY FUNCTION ORD(C)`, `STRING FUNCTION BOOLEAN-OF-INTEGER(N 4) …` and the INSPECT
  forms are refused (COBOL1626, COBOL0847). The same refusal reaches `FUNCTION LENGTH(item)`, which the binder
  FOLDS to a number: it is the function it folds, never "a numeric literal", and the diagnostic says so (a real numeric
  literal keeps the literal wording, §2023:14.9.43.3 SR1). UNSTRING needed no new rule: SR2 (category alphanumeric or
  national) already refused the numeric and boolean functions, and its delimiter now names the folded LENGTH as
  category numeric. **GnuCOBOL 3.2 is no precedent**: `cb_emit_string` checks usage only for a data item (a literal
  carries a `TODO`, a function call falls to `default: break;`), and stores the raw four bytes of
  `cob_intr_integer`'s binary field (`FUNCTION INTEGER(N)` with N = 7 gives `\a\0\0\0`), so the precedence stops at the
  standard. Implemented once: `IntrinsicSig.ImplicitUsageOf` (the §2023:15.2 usage by type) behind
  `IntrinsicArgumentRules.FunctionIsNotDisplayOrNational`, asked by `StringUnstringBinder.Sr1Offence` and
  `StatementValidation.CheckInspectFunctionUsage`; pinned by `DisplayOrNationalUsagePredicateDriftTests`; witnessed by
  `conformance:cases/14.9.43-STRING/pb3665_string_function_usage_controls` and
  `conformance:cases/14.9.22-INSPECT/pb3665_inspect_function_usage_controls` (the admitted functions) and the
  `pb3665-*` refusals.
  **The relation condition (owner 2026-10-25, "Refuse it per the spec"; kb/Work PB3691).** §2023:8.8.4.2.5 (`cite.py
  --check` OK) reads *"The numeric integer operand shall be an integer literal or an integer numeric data item of usage
  display or national."* A numeric, integer or index function-identifier is neither: it is not a literal (§2023:8.4.3.2.1:
  it references a data item, and the folded `FUNCTION LENGTH(item)` is that function, never "an integer literal"), and
  §2023:15.2 4), 5) and 6) describe the item it returns without a usage. So `IF FUNCTION INTEGER(N) = "0007"`, `IF "0007" =
  FUNCTION ABS(N)`, `IF FUNCTION LENGTH(N) = "4"`, the same pairs as EVALUATE subject/object, SEARCH WHEN, PERFORM UNTIL
  and abbreviated combined relations, and the national forms, are refused with **COBOL2532** exactly as a COMP item is
  (no new code), through the SAME reader STRING and INSPECT ask (`IntrinsicArgumentRules.FunctionIsNotDisplayOrNational`,
  called by `RelationComparability.IsNumericIntegerOperand`); an index function (`FUNCTION MAX` over index items) opposite
  a character operand was already refused by §2023:8.8.4.2.13 (COBOL2533). The measured reason it mattered: §2023:8.8.4.2.5
  treats the integer operand "as though it were moved, according to the rules of the MOVE statement, to an elementary
  data item of the same length in terms of character positions as the number of digits in the integer", which needs the operand's digit count — a display item has one (its PICTURE),
  a function result has none the standard states — so `IF FUNCTION INTEGER(N) = "0007"` (N PIC 9(4) VALUE 7) compared
  the function's literal-form text (A.1 item 92) and answered not equal, where `IF N = "0007"` answers equal.
  **GnuCOBOL 3.2** also answers not equal there (and accepts a COMP item, which WiseOwl refuses, COBOL2532), so it is
  no precedent for the admission. Admitted still: a numeric or integer function against a NUMERIC operand (`IF FUNCTION
  INTEGER(N) = 7`, row 1), an alphanumeric or national function against a character operand (`FUNCTION UPPER-CASE(S) =
  "ABCD"`, §2023:15.2 1) and 3)), and an integer display item or literal against a character operand. `--permissive` keeps the
  refused pair as COBOL2532's warning, the character comparison of the function's text, as for the other operands.
  **`SendingValueTemp.FunctionValuePic` is not a usage claim and no admission rule reads it.** The returned-value item's
  USAGE DISPLAY there is the implementor's representation (§2023:15.4.1: *"the characteristics and representation of the
  returned value are defined by the implementor"*) that gives the nominal item its digit capacity for IN-ARITHMETIC-RANGE
  (A.1 item 92; `FUNCTION LENGTH` measures the temporary DISPLAY shows, kb/Work PB3951); the question "described … as usage display or national" is asked of the operand as written, through
  `IntrinsicSig.ImplicitUsageOf` (§2023:15.2), and `DisplayOrNationalUsagePredicateDriftTests` pins that `RelationComparability`
  asks it and never reads a returned-value `PicInfo`. Witnessed by `conformance:cases/8.8.4.2-Simple-relation-conditions/pb3691_relation_function_operands`
  (the admitted pairs) and the `pb3691-*` refusals.
