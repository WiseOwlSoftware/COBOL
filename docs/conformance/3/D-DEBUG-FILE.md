- **D-DEBUG-FILE — when a file-name USE FOR DEBUGGING section runs** (kb/Work PB4811, PB4808). The text is X3.23-1985's Debug module (the
  owner's copy, `specs-private/ANSI_COBOL_1985.pdf`, PDF pages 693-698), USE FOR DEBUGGING general rules 2 and 23 and syntax rule 6; the design is
  `docs/CONTROL_FLOW_DESIGN.md` D20. Rule 2: the section runs after an OPEN or CLOSE that references the file, after a READ "not resulting in the
  execution of an associated AT END or INVALID KEY imperative statement", and after a DELETE or START that references it; rule 23: DEBUG-NAME is
  the file-name, DEBUG-CONTENTS the entire record read for a READ and spaces for every other reference. **Determinations** where the text leaves the
  choice: (1) a DELETE or START section runs whatever the outcome of the I-O, because rule 2 c states no condition, and AFTER the whole statement: its
  USE procedure (§1985:VII.4.6.4 GR6: control returns to the statement afterwards) and its INVALID KEY and NOT INVALID KEY statements are part of its execution, "after the execution of any
  DELETE or START statement" (rule 2 c; kb/Work PB5295). This departs from GnuCOBOL 3.3-dev (`typeck.c:9243`, `9279`, `14423`: "after delete but
  before exception test"), where the 1985 text decides. (2) A READ section runs after the INTO move, any USE procedure and the NOT AT END and NOT INVALID KEY
  statements (kb/Work PB5295), on the path that delivered a record, with the record read in DEBUG-CONTENTS (rule 23 c); a READ that delivered none and ran no AT END or INVALID
  KEY statement (status 10 with no AT END phrase, 46, 47, a keyed 23 with no INVALID KEY phrase) runs it too, after the USE procedure (rule 2 b), with
  DEBUG-CONTENTS spaces because no record was read (rule 11: a member no rule updates stays spaces), and runs nothing else held for the statement: an
  INTO receiver's ALL REFERENCES section is not executed when the INTO move did not run (rule 5, last sentence). (3) DELETE FILE, which the 1985 text does not have, is a DELETE
  statement that references the file-name and runs the section as GnuCOBOL's `cb_emit_delete_file` does. (4) WRITE, REWRITE, SORT, MERGE, RELEASE and
  RETURN are not in rule 2 and run no file-name section (GnuCOBOL agrees). (5) DEBUG-CONTENTS is the file's record area, as wide as the largest
  record of the file. (6) A sort-merge file may be named, and no statement references it in rule 2's sense, so its section never runs.
  Witnesses: `conformance:cases/_unassigned/pb4811-debug-file-name-triggers`, `conformance:cases/_unassigned/pb4811-debug-file-name-repeated`
  (COBOL6335), `conformance:cases/_unassigned/pb4811-debug-file-name-failures` (the failing OPEN, CLOSE, READ, START and DELETE under a USE procedure
  and without one, DEBUG-CONTENTS, an INTO receiver), `conformance:cases/_unassigned/pb4811-debug-file-name-removed-2002` (COBOL0902).
