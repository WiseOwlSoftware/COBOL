- **D-EXPECT — a syntax error names what the parse expects at the token (kb/Work PB3428, PB3861).** §2023:4.2.2 requires a
  compile-time indication of "violations of the general formats" and leaves its wording to the implementor. Where the
  token written is of a kind the parse does not take at its position, **COBOL5506** names the element written (a
  word, a literal by its §2023:8.3.3.1 category — "alphanumeric, boolean, national, or numeric", the integer literal of
  §2023:8.3.3.3.2 among the numeric ones — a figurative constant, or the separator period) and what is expected there:
  every keyword, context word and literal category, where all of it can be named (`OBJECT-COMPUTER. XYZ MEMORY SIZE
  12 FOO.` expects CHARACTERS, MODULES or WORDS; `SEGMENT-LIMIT IS.` an integer literal; `OPEN F` EXTEND, I-O, INPUT
  or OUTPUT), and otherwise the literal categories expected (`SET IX TO "A"`: of the literals, only a numeric
  literal). Context words that are no lexer token are named by running the predicate that recognizes them. The
  figurative constant ZERO is named only where a ZERO written at the token would parse: the parse takes the
  arithmetic ZERO that the token rewriter makes of a ZERO beside an arithmetic operator (`COMPUTE W-N = 1 + "A"`
  names it), and naming it after `SET IX TO`, where the parse refuses a sole ZERO, would direct the user to a rewrite
  the compiler refuses. **What is expected is the compiler's parse, a superset of the
  general format** wherever a later check names the departure better (INSPECT's tallying phrase is parsed with FIRST
  and TRAILING so COBOL2269 refuses them by name), so the message says "expected", never "the general format
  admits". **Except for the keywords the compile edition does not have (kb/Work PB4174):** the parse also takes every
  later edition's words, and §2023:8.3.2.1 1) ("Reserved words shall not be used as user-defined words or system-names")
  makes a word §2023:8.9 has not reserved yet a user-defined word, so a keyword is named unless a table shows a LATER
  edition introduced it: the §2023:8.9 table (`ReservedWords.AdmitsAsKeyword`) or, for a §2023:8.10 context-sensitive word, the
  row's first-listing edition (`ContextSensitiveWordEntry.Introduced`). `COMPUTE W-N = (W-N + 1.` expects B-AND,
  B-OR and B-XOR from COBOL-2002, the shift operators B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R and B-SHIFT-RC from COBOL-2023
  (Annex E.2 3), and none of the seven at COBOL-85; `OPTIONS. ARITHMETIC IS FOO.` expects NATIVE or STANDARD at 2002
  and also STANDARD-BINARY and STANDARD-DECIMAL from 2014; `ADD 1 TO W ROUNDED MODE IS FOO.` names the eight mode
  names from 2014 and, below it, has no keyword to name (the bare COBOL0001). A word an older edition reserved and
  this one dropped stays named (the migration mode's removed clause; `COBOL` and `PHYSICAL`, which 2023 formats
  underline although its §2023:8.9 does not list them), as does a word a format spells where §2023:8.9 leaves it free
  (`keywordWhereFree`, `CobolLexer.IsKeywordWhereFree`). **A §2023:8.10 word's first edition** is derived, never hand-set
  (`scripts/gen-reserved-words.ps1` step 7c, `ContextSensitiveWordsDriftTests.Introduced_IsDerivedFromThe2002ListAndAnnexE33`):
  2002 for a word in the 2002 standard's §2002:8.10 list, which the repository records as
  `tests/version-matrix/context-sensitive-words-2002.json` (transcribed from the 2002 post-ballot text, kb/Work PB3602
  source P2); 2023 for a word Annex E.3.3 item 13 of the 2023 standard says it added and that list lacks; 2014 for every
  other word of the 2023 table (E.1: the annex lists the changes since the previous standard, so a word it does not call
  new was in 2014, and it is not in the 2002 list). A word an older edition reserved outright (`MULTIPLE`, a 1985
  reserved word) is the §2023:8.9 table's. The 2009 CD 1.2 of the 2014 line (PB3602 source P3) lists 14 of the 22 words
  dated 2014 and none dated 2023; the GnuCOBOL per-standard lists were not used (they omit ISO's `NEAREST-EVEN` at
  2014). It is not given where a statement may begin (the
  imperative-statement and statement-shape diagnostics own those positions), where a forgotten separator period
  would let the next sentence or entry follow (COBOL0307), or at a closed format's residue (COBOL1941 / 1970 / 1971).
  Witnesses: `conformance:cases/12.3.6-OBJECT-COMPUTER/pb3428-memory-size-unit-word`,
  `conformance:cases/12.3.6-OBJECT-COMPUTER/pb3428-segment-limit-integer-omitted`,
  `conformance:cases/12.3.6-OBJECT-COMPUTER/pb3428_memory_size_modules_85`,
  `conformance:cases/14.9.39-SET/pb3861-set-index-to-alphanumeric-literal`,
  `conformance:cases/14.9.39-SET/pb3861_set_index_to_integer_literal`,
  `conformance:cases/8.3.2-COBOL-words/pb4174-expected-words-are-the-editions-85`,
  `conformance:cases/8.3.2-COBOL-words/pb4174-expected-words-are-the-editions-2002`,
  `conformance:cases/8.3.2-COBOL-words/pb4174-expected-words-are-the-editions-2023`, `unit:WrittenKindRefusalTests`,
  `unit:CobolWordsDriftTests`.
