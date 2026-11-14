- **D-CORR85 — when a CORRESPONDING statement's group-operand subscripts are evaluated (kb/Work PB5185).** MOVE
  CORRESPONDING at `--std 85` is "the same as if the user had referred to each pair of corresponding identifiers in
  separate MOVE statements" (`cite.py --check "§1985:VI.6.19.4 GR1" "The results are the same as if the user had
  referred to each pair of corresponding identifiers in separate MOVE statements."` → OK) with no exception, so each pair
  evaluates the group operands' subscripts afresh and a pair that stores into one moves the later pairs' element. From
  2002 the same rule carries the exception that freezes them at the start of the statement (`cite.py --check
  "§2023:14.9.25.4 GR11" "the subscript value used is that resulting from the evaluation of the subscript at the start
  of the execution of the statement"` → OK), which the compiler keeps at 2002 and later. ADD and SUBTRACT CORRESPONDING
  at 1985 print no statement about identification time (§1985:VI.6.6.4 GR3 gives only "data items in
  identifier-1 are added to and stored in corresponding data items in identifier-2"), so the standard leaves the choice to the
  implementor and, by the precedence in CLAUDE.md rule 1, GnuCOBOL decides it: it identifies afresh per pair for ADD and
  SUBTRACT as for MOVE. WiseOwl COBOL therefore applies the one edition rule to all three verbs (per pair below 2002,
  one start-of-statement anchor from 2002, §2023:14.7.6 GR6). §2002:F.1 item 16 records the 2002 freeze as a change that the
  earlier text only "implied". Implemented at `CorrespondingBinder.cs#Bind` (`perPair`) and `#CorrAccess`, pinned by
  `conformance:cases/VI.6.19@1985-MOVE-STATEMENT/pb5185_move_corresponding_subscript_per_pair` and
  `conformance:cases/14.9.25-MOVE/pb5185_move_corresponding_subscript_frozen`, `conformance:cases/VI.6.6@1985-ADD-STATEMENT/pb5185_add_corresponding_subscript_per_pair`, `conformance:cases/VI.6.26@1985-SUBTRACT-STATEMENT/pb5185_subtract_corresponding_subscript_per_pair` and `conformance:cases/14.7.6-CORRESPONDING/pb5185_add_corresponding_subscript_frozen`.
