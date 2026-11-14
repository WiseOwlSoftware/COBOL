- **D-SIGN1 — which subordinate items a group's SIGN clause reaches (§2023:13.18.52.4 GR1; kb/Work PB493).** GR1 applies
  the clause "for each numeric item subordinate to the group to which it applies", narrowed only by "The SIGN clause
  applies only to numeric items whose picture character-string contains the symbol 'S'", and names no usage. But
  GR5 and GR6 place the sign on a DIGIT or CHARACTER position, which a binary, packed-decimal or floating-point item
  does not have, and §2023:13.18.52.3 SR2 states the usage pair of the item a clause is written on: "The usage of an
  elementary item for which the SIGN clause is specified shall be display or national". **Determination:** an
  inherited clause reaches a signed numeric item of usage display or national, and a signed item of any other usage
  keeps its own representation (`01 G SIGN IS LEADING SEPARATE. 05 A PIC S9(4) COMP.` leaves A two bytes): the
  standard's own sign positions decide it, so the CLAUDE.md rule 1 precedence never reaches an implementation
  survey. (COBOL-85 instead refuses that group, D-SIGN2.) ONE rule decides it for every route by which an item acquires a clause it did not write
  (`DataBinder.WithInheritedSign`): a data division group (`InheritSignClauses`), the §2023:13.18.49.4 GR5 SAME AS
  ancestor transform, and a REPORT GROUP description entry that is a group, whose clause reaches its printable items
  through the report entry's description (`DataBinder.EnclosingReportItem`; GR2 and GR3 precedence included).
  Witnessed by `conformance:cases/13.18.52-SIGN/pb493_report_group_sign_inherited` and
  `conformance:cases/13.18.52-SIGN/pb536_code_set_group_sign`.
