- **D-85-INDEX-IN-ANOTHER-DIMENSION — an index-name of the table's hierarchy used in another dimension's subscript position
  at COBOL-85 (kb/Work PB5817).** This entry speaks for COBOL-85. In `C3 (CX, RX)`, where CX is the index of the inner table
  and RX of the outer, each index-name is specified by an INDEXED BY phrase of an entry in the hierarchy of the table
  referenced, which is what `§1985:IV.4.3.8.2.3 SR3` requires: "Index-name-1 must correspond to a data description entry in
  the hierarchy of the table being referenced which contains an INDEXED BY phrase specifying that index-name". What the value
  of an index means is the implementor's: "The value of the index referenced by index-name-1 corresponds to the occurrence
  number of an element in the associated table. This correspondence is defined by the implementor" (`§1985:IV.4.3.8.2.4
  GR2`). WiseOwl COBOL holds an index as the occurrence number itself, so an index used in another dimension's position
  selects that occurrence number of that dimension. The reference compiles and runs; GnuCOBOL 3.2 (`cobc -std=cobol85`, run
  under WSL by the PB5817 adjudicator) compiles the same program with no diagnostic and prints the same result. Decided by
  the precedence of CLAUDE.md rule 1 (the correspondence is implementor-defined, GnuCOBOL decides), ruled by the Mythos
  session on 2026-11-20 against an adjudicator's reading that SR3 is positional. Source: `kb/Work/PB5817.md` and its
  2026-11-20 ruling addendum.
