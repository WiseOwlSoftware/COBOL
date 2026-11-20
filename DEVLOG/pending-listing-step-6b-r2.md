## PB5932: the source listing, step 6b remainder slice 2: the file clauses' operands keep their written name, and the table resolves it

Step 6b moves every name the compiler resolves into the one symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1; kb/Work
PB5932). It lands by name class behind a shrink-only baseline of what has not moved. Remainder slice 1 moved the procedure division's
data-names. This slice moves the first half of part B, the data division's clause operands.

**What changed.** A clause operand such as FILE STATUS, RELATIVE KEY, RECORD KEY, ASSIGN USING, PADDING CHARACTER, a LINAGE operand,
RECORD VARYING DEPENDING ON, APPLY COMMIT, VALUE OF or OCCURS DEPENDING ON used to sit in the model as a pair of strings, the name and
its qualifier words. Each binder handed that pair to a lookup that took text. The operand is now one value,
`Binding.Model.WrittenDataName`, made only by the one capture of a data-name-n operand, and the symbol table resolves it
(`SymbolTable.IdentifyDataName(WrittenDataName, Scope)`). The record-key-names, APPLY COMMIT's file of a containing program and an
OMITTED condition's formal are now matched inside the table, read off the file or formal list that carries them. That deletes
`FileModel.RecordKeyNameCandidates` and six name comparisons outside the table. The baseline fell from 75 to 63 entries.

**A design correction.** The brief said the operands would keep a `WrittenQualifiedName`, the parser's form with tokens. The model
holds no parse tree, and an entry decoded from a repository has no tokens, so the operand keeps its written words instead. The clause's
cursor stays beside it for step 7b to record the occurrence. Sections 6.7.1 and 11 say so.

**Behaviour.** `python scripts/arch/compare_oracle.py --no-build --base 9682d2f88d83` printed `=== ARCH-ORACLE: IDENTICAL — 22651
cases ===` after the operand conversion and again after the last move.

**Review leads fixed here.** The 2023 rule against a MERGE inside another SORT or MERGE procedure now reports through its register
row's verdict, so it carries that row's edition flag and the listing counts it among the flagged constructs (lead pb5947 R1-2).
`EditionFlagDriftTests` now also refuses a hand-reported code that a removed row pins. kb/Work PB5951's lead sweep fixed the same two leads
while this branch ran; the merge of main keeps its one mechanism (the scan reads its codes from the rows; the obsolete-only rows
pin only COBOL0903 and COBOL0900, so the two scans covered the same codes) and its fragment reading, which also survives a
malformed escape. The HTML listing's script resolves the URL fragment once (lead pb5948 R1-1). A doc comment names the right test (pb5947 R1-1).

**What remains.** Part B's second half (the data-entry operands OCCURS KEY, RENAMES and SAME AS, the qualification walks moved into the
table, the procedure division's word probes and ambiguity counts), then the declarations (C) and the data-names' entry ids (D).
kb/Work PB5932's dated file `2026-11-19-step-6b-r2` lists them.
