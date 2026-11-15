- **D-INS1 — an INSPECT TALLYING counter in the storage of identifier-1, a pattern or a delimiter (§2023:14.9.22.4 GR13,
  Annex A.2 item 21 d)).** `cite.py --check 14.9.22.4 "If identifier-1, identifier-3, or identifier-4 occupies the same
  storage area as identifier-2, the result of the execution of this statement is undefined"` → OK 13); `cite.py --check
  A.2 "occupies the same storage area as the TALLYING identifier"` → OK 21) d). **Determination:** the operands of
  one TALLYING phrase list are taken in source order, and each operand's count is computed from the state of the
  statement AFTER the operands before it have stored theirs (GR11 still ADDS each count to its counter). The counter's
  new value is therefore visible to identifier-1's image, to a later operand's pattern and to its BEFORE/AFTER
  delimiter: `INSPECT G TALLYING T FOR ALL "A" T FOR ALL "2"` with T inside G counts the "2" the first operand
  stored (D2 = `31A1A1`, not `21A1A1`). A single-operand statement is unaffected (the counter is stored after the
  scan). The mechanism is one rule in `InspectEmitter.EmitBody`: before each operand after the first, when the
  previous counter MAY share storage with identifier-1 or any operand the cycle reads, the data items it may share
  are read again and the one comparison cycle re-run (`StorageOverlap.MayShareStorage` is a conservative
  declaration-level proof of disjointness: containment, REDEFINES, LINKAGE and BASED storage answer "may"; a false
  "may" costs one re-run and changes no result). **A function-identifier is outside this choice and is evaluated
  ONCE, when the tallying begins (kb/Work PB4115):** it *"references a temporary data item whose value is determined
  when the function is referenced at runtime"* (`cite.py --check "§2023:8.4.3.2.4 GR1" "A function-identifier references
  a temporary data item whose value is determined when the function is referenced at runtime"` → OK 1)), INSPECT
  references each identifier once (§2023:14.9.22.4 GR6), and no counter can occupy a temporary item, so GR13's overlap
  never arises for one whatever its arguments: `INSPECT FUNCTION UPPER-CASE(G) TALLYING T FOR ALL "A" T FOR ALL "3"`
  with T inside G over `0A1A1A` gives `3A1A1A`, and `INSPECT FUNCTION CURRENT-DATE` with two counters reads the
  clock once. GnuCOBOL 3.2 agrees for identifier-1 but evaluates a pattern's or delimiter's function when its phrase
  runs, after the counters before it stored (`2X` and `3B` where this compiler gives `1X` and `4B`); the standard
  controls, and this compiler does not follow it. Pinned by
  `conformance:cases/14.9.22-INSPECT/pb4115_inspect_function_evaluated_once` and held by
  `unit:InspectFunctionOnceDriftTests`. **A statement whose counters overlap nothing it reads is the single shared comparison cycle of
  GR8, unchanged** — so two non-overlapping phrases still compete for characters in source order at each position
  (`ALL "BC"` then `ALL "AB"` over "ABCABC" counts BC=0, AB=2, §2023:14.9.22.4 GR8 a), b)); GnuCOBOL runs every phrase as
  its own pass and counts BC=2, AB=0 there, a divergence from the standard on a case with no overlap, which this
  compiler does not follow because the standard controls). Measured against GnuCOBOL 3.2: D2, D3, D5 and the
  reference-modified case agree. Pinned by `conformance:cases/14.9.22-INSPECT/pb1907_inspect_tally_overlap` (D1-D5, RM, and controls C1
  and C2: C1 a statement that overlaps nothing, C2 one the compiler cannot prove disjoint although it is).
