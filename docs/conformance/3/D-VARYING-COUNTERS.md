- **D-VARYING-COUNTERS — VARYING counters of one clause (§2023:13.18.64.3 SR3, §2023:13.18.64.4 GR3 b); kb/Work PB1306)**: SR3 lets arithmetic-expression-2
  name data-name-1 of the same VARYING clause, and the clause may define several counters (§2023:13.18.64.2). The standard
  does not order the additions of GR3 b) among the counters of one entry. Determination: for each occurrence after the
  first the counters of the entry are advanced IN CLAUSE ORDER, each adding its BY evaluated against the counters as
  the previous addition left them (so a BY naming an earlier counter of the clause sees that counter's new value, and
  one naming a later counter or itself sees the old one). Only a BY that names a counter of its own entry needs
  this; every other counter is FROM + n × BY. The choice is the implementor's (CLAUDE.md rule 1); no GnuCOBOL behavior
  was measured for it.
