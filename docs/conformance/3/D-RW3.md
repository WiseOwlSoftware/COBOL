- **D-RW3 — the COBOL-85, 2002 and 2014 reservation flags, and how they meet the removed-construct rows (kb/Work
  PB656, PB4297).** §2023:8.9 decides the 2023 flags. For 2002 the repository records the 2002 standard's own §2023:8.9 list,
  transcribed from its post-ballot text (`tests/version-matrix/reserved-words-2002.json`, words only), and an ISO text
  outranks GnuCOBOL (CLAUDE.md rule 1), so that list IS the 2002 flag, with one correction from the same text: ALIGNED,
  which its Annex F.1 item 6 lists among the reserved words 2002 added and its 13.16.1 defines, though its §2023:8.9 omits it.
  Those flags were derived before the 1985 and 2014 texts were held, so they are GnuCOBOL's per-standard word lists (`scripts/gen-reserved-words.ps1`,
  facts only), less ONE set at 2014 read from the 2009 committee draft (`tests/version-matrix/words-2009-cd.json`, its
  §2023:8.9 and §2023:8.10 lists): a word the draft knows (a 1985 reserved word, or a word of either list) that neither its §2023:8.9
  nor 2023's §2023:8.9 reserves was not reserved by the 2014 standard between them. GnuCOBOL's lists also reserve
  implementor words (§2023:4.2.10 3) allows "additional reserved words as nonstandard extensions" only as documented
  extensions), and the compiler refused nineteen of them at 2002 and twelve at 2014 as data-names with COBOL0901 citing
  §2023:8.9 (POSITION, PROCEED, WORDS, COBOL, the DEBUG-n registers, SEGMENT-LIMIT, COMMUNICATION at 2014, the draft's
  context-sensitive NEGATIVE-INFINITY and POSITIVE-INFINITY). A word the 2014 final added after the draft (PHYSICAL,
  the FLOAT-BINARY-n renames) keeps GnuCOBOL's flag; the draft's five names the final renamed are never imported.
  ISO Annex E.2 item 25 ("The following reserved words have been added", `cite.py --check E.2 "The following reserved
  words have been added"` → OK) speaks about one edge only, because E.1 lists "the substantive changes between the
  previous COBOL standard and this" one (`cite.py --check E.1 "substantive changes between the previous COBOL standard
  and this"` → OK): its 16 words were user-defined at 2014, which the generator ASSERTS of the 2014 list. SEND, RECEIVE
  and END-RECEIVE are on the 2002 §2002:8.9 list, like the rest of the communication facility's vocabulary (CD, DISABLE,
  ENABLE, PURGE, …), obsolete there and removed at 2014. ORDER is reserved at 85 as well (kb/Work PB3957): GnuCOBOL's 85 list holds it, the 2002 standard's Annex F.1 item 6 (printed page 835 of its
  post-ballot text, "The following reserved words have been added") does not name it among the words 2002 added although
  the same text's 8.9 list holds it, and `scripts/gen-reserved-words.ps1` asserts that for EVERY 8.9 word
  (`reserved-words-2002.json` `addedByAnnexF1`: a 2002 word the item does not name must be in the 85 source). An earlier revision cleared ORDER at 85
  on a "CCVS proof" that misread ST127A, which spells SORT's `WITH DUPLICATES IN ORDER` keyword and declares no data item ORDER.
  ORDER therefore has no free edition, so its `cobol-words.json` row carries no `keywordWhereFree` determination (the one way to free
  a word, `>>COBOL-WORDS UNDEFINE`, withdraws "any syntax requiring the use of" it, §2023:7.3.10.4 GR3, `cite.py --check 7.3.10.4
  "any syntax requiring the use of the COBOL word that is the content of literal-3 shall not be available"` → OK) and the generator refuses
  such a determination on a word reserved at every edition. A word's reservation and its construct's availability are TWO
  facts: SEGMENT-LIMIT and COBOL leave the 2002 list with their constructs (VCR row 7.8), so each is a data-name from
  2002 on, and COBOL a compiler-directive word as well at 2023 (§2023:8.3.2.1 2), §2023:8.12). GnuCOBOL's 3.2 dialect files date
  the PADDING CHARACTER clause: obsolete 2002 (COBOL0903), removed 2014 (VCR row 7.20). ⚠ DETERMINATION (owner may
  overturn; a 1985 or 2014 text overturns any flag in one generator run): pinned by the version-matrix rows
  `receive-as-user-word`, `end-receive-as-user-word`, `segment-limit-removed-2002`, `padding-character-removed-2014`,
  the negatives `user-word-receive` / `user-word-end-receive` (85 2002 2023), `pb4297-position-reserved-at-85` and
  `pb4297-communication-reserved-85-2002`, and
  `conformance:cases/8.3.2-COBOL-words/pb4297_1985_words_user_defined_from_2002` (2002 2014 2023),
  `…/pb4297_communication_user_defined_from_2014` (2014 2023), `…/pb4297_infinity_words_user_defined` (every edition), `pb3957-order-reserved-at-every-edition` (85 2002 2014 2023)
  and `conformance:cases/_unassigned/pb656_padding_character_obsolete_2002`.
