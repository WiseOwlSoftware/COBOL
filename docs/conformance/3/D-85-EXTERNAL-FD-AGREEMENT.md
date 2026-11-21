- **D-85-EXTERNAL-FD-AGREEMENT — the agreement of file description entries for one external file connector across
  separately compiled programs at COBOL-85 (kb/Work PB5912).** This entry speaks for COBOL-85. The 1985 general rules that
  tie the descriptions of one external file connector together state what the run unit must have, for instance "all file
  description entries in the run unit which are associated with this file connector must have" a LINAGE clause and its values
  in common (`§1985:VII.3.7.4 GR11`), and none states a consequence; `§1985:I.1.6` gives the result of a violation: "The
  results of violating the formats or rules of Standard COBOL are undefined unless otherwise specified in this standard".
  WiseOwl COBOL checks these rules where both descriptions are in one compilation group. It does not compare the descriptions
  of two separately compiled programs at `--std 85`, at compile time or at run time; a run unit that violates them has
  undefined results, as the text says. Each declaring program's own description must still name external data items where
  the text requires one (the LINAGE data-names, kb/Work PB5982; the PADDING CHARACTER data item, `§1985:VII.2.7.4 GR6`).
  Decided by the Mythos session on 2026-11-20 (the documented limit, option (a) of the PB5912 ruling). Source:
  `kb/Work/PB5912.md` and its 2026-11-20 ruling addendum.
