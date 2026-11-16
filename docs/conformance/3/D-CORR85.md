- **D-CORR85 — when a CORRESPONDING statement's group-operand subscripts are evaluated (kb/Work PB5185, corrected for ADD
  and SUBTRACT by kb/Work PB5814; the redefined-subscript arm of MOVE by kb/Work PB5767).** MOVE CORRESPONDING at
  `--std 85` is "the same as if the user had referred to each pair of corresponding identifiers in separate MOVE
  statements" (`cite.py --check "§1985:VI.6.19.4 GR1" "The results are the same as if the user had referred to each pair
  of corresponding identifiers in separate MOVE statements."` → OK), and each separate MOVE evaluates "Any length
  evaluation or subscripting associated with identifier-2 ... immediately before the data is moved to the respective data
  item" (`§1985:VI.6.19.4 GR2`, OK), so each pair evaluates the group operands' subscripts afresh and a pair that stores
  into one moves the later pairs' element. That holds for every subscript, including one whose data-name lies in a
  redefined record, which the compiler reads through a statement-level temporary: at 85 the MOVE's identification steps
  are re-run before each pair (`BoundCorresponding.PairIdentification`). ADD and SUBTRACT CORRESPONDING state no
  exception (§1985:VI.6.6.4 GR3 gives only "data items in identifier-1 are added to and stored in corresponding data
  items in identifier-2"), so the general sentence of §1985:IV.4.3.8 as amended by X3.23a-1989 governs them: "any
  subscripting and reference modification are evaluated only once as the first operation of the execution of that
  statement" (`cite.py --check "§1985:IV.4.3.8"` → OK). The standard controls, so there is no implementor latitude for
  CLAUDE.md rule 1 to hand to GnuCOBOL (this determination said otherwise until 2026-11-16 and took GnuCOBOL's per-pair
  behaviour; its premise that 1985 prints no identification time for the two verbs overlooked the amended §1985:IV.4.3.8).
  From 2002 the MOVE rule carries the exception that freezes the subscripts at the start of the statement (`cite.py
  --check "§2023:14.9.25.4 GR11" "the subscript value used is that resulting from the evaluation of the subscript at the
  start of the execution of the statement"` → OK), and every verb is identified once (§2023:14.7.6); §2002:F.1 item 16
  records the 2002 freeze as a change that the earlier text only "implied". So: per pair for MOVE below 2002, once at the
  start for ADD and SUBTRACT at every edition and for MOVE from 2002. Implemented at `CorrespondingBinder.cs#Bind`
  (`perPair`, `pairIdentification`) and `#CorrAccess`, and `CorrespondingEmitter.Emit`; pinned by
  `conformance:cases/VI.6.19@1985-MOVE-STATEMENT/pb5185_move_corresponding_subscript_per_pair`,
  `conformance:cases/VI.6.19@1985-MOVE-STATEMENT/pb5767_move_corresponding_redefined_subscript_per_pair_85`,
  `conformance:cases/14.9.25-MOVE/pb5185_move_corresponding_subscript_frozen`,
  `conformance:cases/VI.6.6@1985-ADD-STATEMENT/pb5814_add_corresponding_subscript_once_85`,
  `conformance:cases/VI.6.26@1985-SUBTRACT-STATEMENT/pb5814_subtract_corresponding_subscript_once_85` and
  `conformance:cases/14.7.6-CORRESPONDING/pb5185_add_corresponding_subscript_frozen`.
