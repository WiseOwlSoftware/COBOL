## PB3370-PB3372: wave 1117's leads filed (and PB616, PB2672 extended)

Notes only; no code changed.

**PB3370.** A reserved word after a SOURCE-COMPUTER or OBJECT-COMPUTER computer-name (`SOURCE-COMPUTER. XYZ GLOBAL.`) is refused with a generic COBOL0307 instead of a named diagnostic: the optional group's exit alternative on error recovery reads XYZ through unrecognizedClause. Message quality only; the fix is a grammar restructure of the optional group. Family of PB2533, different mechanism. Both repro programs are copied into the note.

**PB3371.** `prune_worktrees.py` lifecycle() advises deleting a FINISHED branch as superseded when its lead note is landed, even when the same line reports UNLANDED work; a branch amending a landed note (notes-pb3149-owner-20261018) was so advised. The note records the repro and the fix shape. PB2672 gains a paragraph naming PB3371 as its sibling arm.

**PB3372.** `FileRegistry.OpenCore` treats any SHARING or RETRY phrase as a record-locking participant even with `FileLockMode.None`. Reported as a design smell with harm unmeasured; the governing ISO rules are not yet derived, and the note says so.

**PB616.** The citation-qualification question (861 bare `§X.Y.Z GRn` sites in src/, 2,336 tree-wide, accepted deliberately by `audit_code_citations.py`) is recorded as an open owner decision.
