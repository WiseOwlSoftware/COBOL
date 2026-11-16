- **D-85-GROUP-CATEGORY — a group item is of the category alphanumeric: the FILE STATUS item at COBOL-85 (kb/Work PB5870).**
  This entry speaks for COBOL-85. A group item is of the category alphanumeric. Row `1985-SR-VII.2.5.3-2`: the FILE STATUS
  item "must be defined in the Data Division as a two-character data item of the category alphanumeric" (`§1985:VII.2.5.3
  SR2`). The 1985 text gives a group item the class alphanumeric and names no category for it. This compiler accepts a FILE
  STATUS item that is a group, a group of two numeric items included, as GnuCOBOL does (`cobc/tree.c#cb_tree_category` gives
  every field that has subordinate items the category alphanumeric; `cobc/typeck.c#validate_file_status` accepts a
  two-character item of that category; 3.3-dev, source read, not run). Decided by the precedence of CLAUDE.md rule 1 (the
  text leaves the choice, GnuCOBOL decides). Source: `kb/Work/PB5870.md`, item 1. Witnesses:
  `conformance:cases/VII.2.5@1985-FILE-STATUS-CLAUSE/g85_y94` is the row's own case; none tracked pins the group item.
