- **D-RANGE-KNOWN — when "the runtime collating sequence is known" for a level-88 THROUGH range** (2026-10-07;
  kb/Work PB552, rows `SR-13.18.63.3-26`, `SR-13.18.63.3-27`). §2023:13.18.63.3 SR26 b) and SR27 b) apply to an
  alphanumeric or national range only "when … the runtime collating sequence is known", and SR26's NOTE 5 leaves
  knowing it to the implementor ("this is not a requirement of an implementor"); the one sentence the standard does
  fix is "The runtime collating sequence is unknown when the collating sequence is defined by a locale or the
  collating sequence is otherwise determined at runtime" (cite.py OK). **Determination: every sequence this
  processor fixes at compile time is known** — the `IN alphabet-name-1` phrase's alphabet, else the PROGRAM COLLATING
  SEQUENCE, else the native order — and only a LOCALE sequence is not. So an inverted or equal character range in a
  known sequence is refused at compile time (COBOL2961), as a numeric one always is (SR26 a)), and §2023:14.7.8's
  run-time EC-RANGE-INVALID arm is reached by a level-88 range only through a LOCALE alphabet (and by an EVALUATE
  WHEN range, which no syntax rule orders). Survey under the owner's precedence (CLAUDE.md rule 1; the 2026-08-30
  protocol): GnuCOBOL 3.2 has nothing — it accepts `88 C VALUE 5 THRU 1` and `VALUE "Z" THRU "A"` silently at
  `-std=default`, `cobol2014` and `ibm-strict` (measured, wave 1031 C), enforcing neither arm; IBM Enterprise COBOL
  6.4 states the rule for every class with no condition ("When the THROUGH phrase is used, literal-1 must be less than
  literal-2", Language Reference, VALUE clause format 2), i.e. treats its fixed sequences as known. SR27 b)'s screen
  (COBOL2048) already read the same predicate (`DataBinder.ConditionValueOrder.Known`); SR26 now shares it.
  Witnesses: `conformance:cases/13.18.63-VALUE/pb552-value-through-not-ascending`, `conformance:cases/13.18.63-VALUE/ec_range_invalid` (the
  LOCALE arm at run time), `conformance:cases/13.18.63-VALUE/pb502_value_range_in_alphabet` (an ascending range in a reversed alphabet).
