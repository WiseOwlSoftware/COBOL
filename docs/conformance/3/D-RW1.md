- **D-RW1 — reserved words added for nonstandard extensions (§2023:4.2.10; kb/Work PB655).** §2023:4.2.10: "An
  implementation that introduces additional reserved words as nonstandard extensions conforms to this Working
  Draft International Standard, even though the additional reserved words may prevent translation of some
  conforming compilation groups", and "Documentation associated with an implementation … shall specify any
  reserved words added for nonstandard extensions" (`cite.py --check 4.2.10 "shall specify any reserved words
  added for nonstandard extensions"` → OK). This is that specification. Every OTHER keyword the lexer tokenizes is
  a legal user-defined word wherever ISO §2023:8.9 leaves it free (reservation-gated per edition) and, for a §2023:8.10
  context-sensitive word, everywhere outside its context — `CobolWordsDriftTests` proves that join empty. The
  words below are reserved at EVERY edition although ISO reserves none of them, following GnuCOBOL (the model
  implementation for implementor latitude), because each spells a vendor construct this compiler recognizes:
  **COMP-1, COMP-2, COMP-3, COMP-4, COMP-5, COMPUTATIONAL-1, COMPUTATIONAL-2, COMPUTATIONAL-3,
  COMPUTATIONAL-4, COMPUTATIONAL-5, END-JSON, END-XML, ENTRY, JSON, XML**. Each is a reserved word of GnuCOBOL 3.2
  (`cobc/reserved.c`, no context test). CHANNEL, END-INVOKE, END-MERGE, END-METHOD, END-SORT, GENERIC, PACKED and
  REMARKS were on this list until 2026-09-29 and are NOT reserved: ISO §2023:8.9 does not reserve them, GnuCOBOL 3.2
  has no entry (REMARKS: a context test normal code never meets), so they are user-defined words and the vendor
  constructs they spell are recognized by position (owner: follow ISO) — except CHANNEL, whose vendor SPECIAL-NAMES entry `CHANNEL integer IS mnemonic-name` (and its `RESERVE integer [CHANNELS]` twin) no ISO format prints and nothing implements, so it is refused as a syntax error and CHANNEL is a user word everywhere (kb/Work PB2727), and END-INVOKE, END-SORT and
  END-MERGE, which spell NO construct at all: Table 12 (§2023:14.5.1) gives INVOKE, SORT and MERGE no explicit scope
  terminator (`cite.py --check 14.5.1 "Statement names are identified in Table 12"` → OK; §2023:14.9.23.2's INVOKE format
  ends at `[ RETURNING identifier-4 ]`), so `INVOKE … END-INVOKE`, `SORT … END-SORT` and `MERGE … END-MERGE` are
  not recognized — the word is an ordinary user word in the position after the statement (a syntax error after
  INVOKE, an undefined GIVING file after SORT/MERGE) — and none of the three has a lexer token (kb/Work PB758,
  decision R58 for END-INVOKE and the same ISO text for the other two; negatives
  `pb758-invoke-end-invoke-after-returning`, `pb758-invoke-end-invoke-after-using-returning`,
  `pb758-sort-end-sort-is-not-a-terminator` and `pb758-merge-end-merge-is-not-a-terminator`). The single source is
  `tests/version-matrix/cobol-words.json` `extensionReserved`; `CobolWordsDriftTests` fails when this list, that array and the lexer disagree,
  and `NonstandardExtensionRegisterDriftTests` fails when it and the `ReservedWords` of the §3.2 register's rows disagree
  (each word is reserved on the account of one extension row).
  **The `>>COBOL-WORDS` directive treats these words as reserved words (owner decision 2026-10-31, kb/Work PB4239).**
  §2023:8.3.2.1 1) makes a §2023:4.2.10 addition a reserved word of this implementation, §2023:7.3.10.1 and D.12.1 give the directive so a
  program may change which words are reserved (D.12.1: "to conform to previously existing implementor extensions"), and D.12.3
  names this collision ("a change of compiler that includes new reserved words that conflict with existing code")
  (`cite.py --check D.12.3 "This can be useful when a change of compiler that includes new reserved words that conflict with
  existing code"` → OK). So `UNDEFINE`, `EQUATE` and `SUBSTITUTE` may name any of them as literal-1/3/4 (§2023:7.3.10.3 SR3), and
  §2023:7.3.10.4 GR3/GR4 then free or respell the word (`>>COBOL-WORDS UNDEFINE "JSON"` makes JSON a data-name; `UNDEFINE
  "COMP-3"` frees COMP-3 and leaves COMPUTATIONAL-3 the packed usage), while `RESERVE` and the new word of `EQUATE` and
  `SUBSTITUTE` may not be one (literal-2/5/6, §2023:7.3.10.3 SR4, COBOL1623 naming this reservation). GnuCOBOL 3.2's default
  dialect agrees for UNDEFINE and for EQUATE's new word; it accepts `RESERVE "JSON"` where SR4 refuses (the standard controls,
  not that latitude); its `-std=cobol2014` does not reserve the words and so refuses `UNDEFINE` of them. Pinned by
  `conformance:cases/7.3.10-COBOL-WORDS/pb4239_*`, the `pb4239-*` negatives and
  `CobolWordsDirectiveTests.Sr3_AnExtensionReservedWord_IsAnExistingWord` / `Sr4_AnExtensionReservedWord_IsNotAFreshWord`, which
  iterate the register.
  ⚠ DETERMINATION (owner may overturn): reserving them keeps the vendor forms recognizable by name; the rejected
  reading — admitting them as user words at every edition — would make each vendor construct ambiguous with a
  data reference in every operand list.
