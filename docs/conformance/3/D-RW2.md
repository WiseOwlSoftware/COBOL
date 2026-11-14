- **D-RW2 — METHOD is reserved at COBOL-2002 and COBOL-2014 and is a user-defined word at COBOL-2023 (§2023:4.2.8, §2023:8.9;
  kb/Work PB2623).** §2023:4.2.8: "An implementation shall recognize as reserved words all the COBOL reserved words
  specified in 8.9, Reserved words" (`cite.py --check 4.2.8 "shall recognize as reserved words all the COBOL reserved
  words specified in 8.9"` → OK). The printed 2023 §2023:8.9 list runs MERGE, MESSAGE-TAG, METHOD-ID, MINUS, MODE (PDF page
  236, printed page 206; `cite.py --check 8.9 "METHOD-ID"` → OK) and has no METHOD, and §2023:8.10 lists none either. An
  earlier generator revision added METHOD to the 2023 set as an "OCR omission"; rendering the page refutes that, so
  the line is gone and METHOD follows every other 2002/2014 word the 2023 list dropped (POSITION, COBOL, …): reserved
  at 2002 and 2014, a user-defined word at 85 (no OO) and at 2023. The standard still SPELLS METHOD in the END METHOD
  marker (§2023:10.7.2; `cite.py --check 10.7.2 "METHOD, and INTERFACE are underlined"` → OK) and in EXIT METHOD (`cite.py --check E.2 "EXIT METHOD statement"` → OK; the 2002 and 2014 statement,
  removed at 2023); those keyword positions read the word whether or not the program also declares a data item of that
  name (`methodKeyword`, generated into `CobolWords.g4` from the row's `keywordWhereFree` determination in `cobol-words.json`, the rule MESSAGE and MULTIPLE share; kb/Work PB3794). ⚠ DETERMINATION (owner may overturn): the standard's own text decides — a
  word its list does not reserve is not reserved (§2023:4.2.8, §2023:8.3.2.1 rule 1) — so the alternative, treating the
  omission as a typo and keeping METHOD reserved at 2023, would be an added reserved word that §2023:4.2.10 3) obliges this
  document to list, and would refuse a program the standard permits. Pinned by `conformance:cases/_unassigned/pb2623_method_is_a_user_word_at_2023`,
  `negative/pb2623-method-reserved-at-2002-2014` and `MethodUserWordTests`.
