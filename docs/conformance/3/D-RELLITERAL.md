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
