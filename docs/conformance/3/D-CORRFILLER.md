- **D-CORRFILLER — an item under a FILLER group corresponds (§2023:14.7.6 rules 1 and 5; kb/Work PB229).** Rule 1
  excludes the data item that is itself "implicitly or explicitly described with the keyword FILLER" and pairs items
  with "the same data-name and the same qualifiers, if any, up to, but not including, D1 and D2" (`cite.py --check
  14.7.6` → OK, rule 1). The qualifiers are names: "Qualification is the specification of superordinate names from
  the hierarchy to which a user-defined name belongs. The superordinate names are called qualifiers" (`--check
  8.4.2.2.1` → OK), and a FILLER group has none to give (`--check 13.18.20.4 "Under no circumstances shall a FILLER
  item be referred to explicitly."` → OK, rule 1). So an item subordinate to a FILLER group has the qualifiers it
  would have beside that group, and **WiseOwl COBOL pairs it with a namesake standing there on the other side**, for
  MOVE, ADD and SUBTRACT CORRESPONDING alike. Rule 5 is where the standard names the intermediate groups that cut
  their subordinates off, "when the group item contains an OCCURS or REDEFINES clause" (`--check 14.7.6` → OK,
  rule 5), so a FILLER group carrying either clause still excludes its whole subtree. The standard controls here,
  so the owner's precedence (ISO, then GnuCOBOL, then IBM or Micro Focus) never reaches the model implementations;
  GnuCOBOL, which matches CORRESPONDING members by name one level at a time, is understood to skip a FILLER group's
  subtree, which this reading departs from (not measured: the implementer host has no GnuCOBOL install). The rejected
  reading, which this compiler held until PB229 on "the legacy/NIST-proven" behavior and no clause, cut off every
  FILLER group's subtree and silently skipped the pair. The same reading already governed rule 6's uniqueness count,
  which stands a FILLER group's subordinates at the group's level (kb/Work PB1111). Implemented at
  `CorrespondingBinder.cs#CorrMembers`, pinned by `conformance:cases/14.7.6-CORRESPONDING/pb229_corresponding_filler_group_rule4`.
