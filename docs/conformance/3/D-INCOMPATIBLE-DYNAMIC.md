- **D-INCOMPATIBLE-DYNAMIC — Incompatible dynamic-length and dynamic-capacity data (§2023:14.6.13.2 rules 5 and 6; kb/Work PB1118).** A
  dynamic-length item IS a managed string, so its internal format — a length and that many characters — is always
  correctly formed, and the one way its content can disagree with "the corresponding DYNAMIC LENGTH clause" is to be
  longer than THAT clause's maximum size (§2023:8.5.1.10.1; the minimum length is zero). Content stored through a
  description always agrees with it, because every store truncates to the maximum (§2023:8.5.1.10.4), so a disagreement
  arises only when the item is READ through a description other than the one that stored it: a LINKAGE formal
  (§2023:14.2.3 GR8 — "as if the formal parameter occupies the same storage area as the argument") and a member of a shared
  cell-backed area such as an EXTERNAL record described in two programs. **Those reads, and only those, ask the
  question** (`CobolDynString.Agree`): under EC-DATA-INCOMPATIBLE checking content past the maximum is the fatal
  condition at the sending reference (a USE declarative may RESUME, abandoning the statement); with checking off the
  content is read as it is, whole. A receiving-only reference is not a reference to the content (rule 5's closing
  paragraph, NOTE 2) and asks nothing. **Rule 6 is unreachable by construction:** the implementor's internal format of
  a dynamic-capacity table is one `CobolDynTable` object, and every one a program reads was constructed from that
  program's own OCCURS clause or recreated into it by a variable-length group transfer at the receiving description
  (§2023:14.6.9.2); no storage form shares one across descriptions (an EXTERNAL record holding one is COBOL0899, a file
  record cannot hold one, §2023:8.5.1.9.1 3), and a LINKAGE one crosses through that same recreation).
  `DynamicCapacityAgreementDriftTests` pins those construction paths. Pinned by `2014/pb1118_dyn_length_agreement`.
