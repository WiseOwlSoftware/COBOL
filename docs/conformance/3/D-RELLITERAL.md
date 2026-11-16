- **D-RELLITERAL — a relation condition of two literals is a WARNING (kb/Work PB1470).** §2023:8.8.4.2.1 closes its
  general rules with "A relation condition shall contain at least one reference to an operand that is not a literal"
  (`cite.py --check 8.8.4.2.1` stamps the unnumbered sentence "13)", PB222). It is neither a general format nor a
  syntax rule, so §2023:4.2.2 settles only that the warning mechanism indicates it ("This warning mechanism shall indicate
  violations of such rules"); the severity is implementor latitude, and the owner's precedence (ISO, then GnuCOBOL,
  then IBM or Micro Focus) decides it. GnuCOBOL 3.2, measured under WSL, compiles `IF 1 = 1`, `IF "A" = "A"` and
  `PERFORM UNTIL 1 = 1` and warns under `-Wall` (`-Wconstant-numlit-expression`, `-Wconstant-expression`). So each such
  relation is **WARNING COBOL3146** at every edition and strictness, and is evaluated as written. A figurative
  constant is a literal (§2023:8.3.3.6 is a clause of §2023:8.3.3 Literals), a symbolic-character is "a user-defined figurative
  constant" (§2023:8.3.2.2.29), and a constant-name's effect "is as if literal-1 … were written where constant-name-1 is
  written" (§2023:13.10.4 GR1), so `IF "A" = SPACE`, `IF ZERO = ZERO`, `IF SB = "B"` and `IF KT = "A"` draw it too; a data
  item, an arithmetic expression (`IF 1 + 1 = 2`) and a function-identifier (`FUNCTION LENGTH(X)`, folded at bind
  time or not, §2023:8.4.3.2.1) do not. It is asked where a relation condition is WRITTEN: IF, PERFORM UNTIL, SEARCH WHEN, a
  boolean relation, an abbreviated relation's inserted subject (`IF 1 = N OR 2` holds `1 = 2`, §2023:8.8.4.12) and an
  EVALUATE partial-expression (§2023:14.9.13.3 SR8: "treated as though it were specified as condition-2"). An EVALUATE
  subject/object pair or range is governed by §2023:14.9.13.3 SR10's Table 15, which refuses a literal object against a
  literal subject and permits a range-expression against one, so it is not asked again. Implemented in
  `ConditionBinder.WrittenRelational` → `StatementValidation.CheckRelationHasNonLiteralOperand`; pinned by
  `conformance:RelationLiteralOperandTests` and `conformance:cases/8.8.4.2-Simple-relation-conditions/pb1470_relation_of_two_literals_runs`.
  **1985 text (kb/Work PB5826).** At `--std 85` the text is `§1985:VI.6.3.1.1`, "The relation condition must contain at least one reference to a variable", and for the sign condition `§1985:VI.6.3.1.5`, "Arithmetic-expression-1 must contain at least one reference to a variable" (cite.py OK on both). The 2002 relation sentence is `§2002:8.8.4.1.1` (`§2014:8.8.4.1.1`), the 2023 one `§2023:8.8.4.2.1`; the 2002 and 2023 sign-condition texts have no such sentence. An arithmetic expression of literals references no variable (`IF 2 * 2 = 4`), so at 85 it is flagged where at 2002 and later it is not. Same determination as above: a WARNING at 85 too, COBOL7609 for the relation and COBOL7610 for the sign condition (COBOL3146 is not raised at 85), the condition evaluated as written. The EVALUATE selection-subject sign condition (§2023:14.9.13.3 SR5) is the EVALUATE rule, so it is not asked. Implemented in `StatementValidation.CheckRelationHasNonLiteralOperand` (its `DialectLevel < 2002` arm) and `CheckSignConditionHasVariable`; pinned by `conformance:RelationLiteralOperandTests` and `conformance:cases/VI.6.22@1985-SEARCH-STATEMENT/pb5826_condition_without_variable_runs`.
