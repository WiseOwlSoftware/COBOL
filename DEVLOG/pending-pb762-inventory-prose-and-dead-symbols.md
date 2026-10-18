## PB762: the inventory gate resolved symbols against comments and never read a row's prose

**What the note said.** Seven traceability-inventory rows narrate `ReadLockGovern` and `ApplyReadLockDiscipline`, which
no longer exist, and nothing measures it because `SpecTraceabilityInventoryDriftTests` reads `code-location` and not
`notes`. Five of the seven already carried a dated PB338 erratum stapled beneath prose that still described the dead
method in the present tense; `GR-14.9.30.4-22` and `FMT-14.9.30.2` carried none.

**What the re-probe showed, on this tree.** The premise "code-location resolves on all seven" was true for the wrong
reason. The resolver ran a word search over the WHOLE file body, so `FileRegistry.cs#ReadLockGovern` was listed in the
`code-location` of three CONFORMS rows (`GR-14.9.30.4-8`, `-9`, `-12`) and resolved on a comment that narrates the
method's removal. A comment-blind resolver found **45 dead witnesses on 38 rows** (one, `Clock.cs#Clock`, never named
a declaration: the file holds `IClock` and `SystemClock`), and a scan of the prose for `path#Symbol` spellings found
**16 more rows** quoting a location that has moved or gone, four of them as `docs/CONFORMANCE.md#DOC-A.1-n (absent)`.

**The fix, in the two places the architecture assigns.**
- One predicate, `SymbolResolves`, for `code-location` and for every `path#Symbol` spelling in a row's prose
  (`EveryNotesLocation_ResolvesInTheTree`, new). It reads a C# file's tokens, so a comment and a doc comment are not
  code and a string literal is; a grammar file without its comments; any other file whole. A dotted C# symbol is a
  qualified name, each segment an identifier of the file. Prose is `notes` up to the first `retired-witness:` mark,
  whose spelling moved into the schema (`notes.retired-witness-mark`) so both engines read one.
- `record_verdicts.validate` refuses a record that retires a witness while the row's prose still names its symbol
  and the row no longer cites it anywhere else. The history of a rename goes in `retire-reason`, behind the mark; the
  prose describes the code as it is. `audit_witness_loss.py --self-test` plants the PB338 shape (an erratum appended
  beside the stale sentence) and the moved-symbol exemption on a PLANTED row (a fixed symbol and a second witness), never a
  row borrowed from the live inventory, whose first witness may be a bare path or be re-sited (branch review R1-1).
- The gate was shown to fail: the fabricated-inventory test drives a scratch tree (line, doc and block comments, a
  grammar comment, an absent member, a stale prose spelling, a retired mark hiding history); on the real tree it was
  RED with 45 + 16 before the batch.

**The data.** `adjudication/pb762-notes-and-dead-sites/batch.json`: 63 records, GAP 56 -> 56, 45 witnesses retired with a
reason each and 20 successors added (`ItemLength.Positions`, `DataItem.ByteWidth`, `CurrencyOperandOf`, ...). The seven
rows were rewritten in the present tense: `ReadShared` and `ReadKeyedShared` ask `ConflictOnLockedRecord` before the
physical step, `ApplyPostReadLockActions` decides retention through the retention bracket alone, and the erratum is
folded away. `GR-14.9.30.4-12` also carried a second false sentence (the 2002 gate "in GateStatement"): the gate is the
parse arm's `VisitReadIgnoringLock`, and its IGNORING LOCK code-path sentences were stale since PB331 and are rewritten.

**What self-review found.** `COBOLNET_FILES_DESIGN.md` described the same dead symbols in the present tense in three
places (fixed); `DataBinder.Switches.cs` carried a `<see cref>` to the dead `CurrencyTextLiteral` (fixed). The ISO
citations in the touched prose were each re-run through `cite.py --check`; no behaviour changed, so no golden.

**Not fixed, measured.** A BARE identifier in old prose is invisible to any resolver (prose also narrates history). On
this tree 89 rows keep a retired symbol in their prose with no live citation of it; the writer's rule stops the next
one and the residue needs each sentence read. This landing touches no SORT or file I/O behaviour, so the owner's
success-and-exception test requirement has nothing to add here.
