**2026-11-15: the 1985 row `1985-SR-IV.4.3.8.1-4` waits on this adjudication (registrar re-read of the open PARTIAL 1985 rows).**

The re-read's adjudicator read `1985-SR-IV.4.3.8.1-4` CONFORMS; its refuter answered DOUBTFUL
(`E:/COBOL-coord/scratch/op-20261115/open-rows-1985/out-a-refute.json`), and the doubt is this note's question, at 85:
do the SUBORDINATES of a GLOBAL entry possess the global attribute? The row stays PARTIAL, owned by this note, until it is
adjudicated. It is not a defect until then. The row is added to this note's `inventory_rows`.

**The rule.** §1985:IV.4.3.8.1 SR4: "The same data-name must not be used as the name of an item possessing the global
attribute and as the name of any other data item described in the program" (`cite.py --check` OK).

**Reading A** (the compiler's, carried from the 2023 adjudication PB5150): §1985:X.4.6.4 GR1, "All data-names subordinate
to a global name are global names" (OK), makes every subordinate of a GLOBAL entry possess the attribute, so a second item
of the same name anywhere in the program is refused (COBOL6862).

**Reading B** (the refuter's): the 1985 text keeps "global name" (GR1, a matter of scope) apart from "possessing the global
attribute", which it attaches to the entry that carries the clause:
- §1985:X.4.6.4 GR3: "it is only the subject of that REDEFINES clause which possesses the global attribute" (OK);
- §1985:X.1.3.8.3: "If a data item possessing the global attribute includes a table described with an index-name" (OK):
  the item possessing it INCLUDES the table, so it is the group, not each subordinate;
- §1985:X.4.6.3 SR2 confines the declaration bar to entries that "include the GLOBAL clause" (OK);
- §1985:X.1.3.8.2 GR1 resolves global names by "the normal rules for qualification and any other rules for uniqueness of
  reference are applied until one or more objects is identified" (OK), which presupposes same-named global subordinates
  told apart by qualifiers.
Under reading B the probes below are legal 1985 source, and COBOL6862 rejects legal source.

The words that separate the readings: "possessing the global attribute" (SR4, GR3, X.1.3.8.3) against "global names" (GR1).

**The refuter's three probes**, re-run 2026-11-15 on the build of branch `records-1985-reread` (main 2722570af0),
`--std 85` (`E:/COBOL-coord/scratch/op-20261115/registrar-1985-reread/log-misc.txt`):
- `E:/COBOL-coord/scratch/op-20261115/open-rows-1985/refute-a/s4-g-within.cob` (two same-named subordinates X inside ONE
  GLOBAL record, this note's own arm): `error COBOL6862: data-name 'X' names this GLOBAL item and also a GLOBAL item
  described in the same source element ...` (reading A refuses; reading B accepts and prints 12);
- `refute-a/s4-g-twosub.cob` (subordinate X of GLOBAL 01 G1 and subordinate X of GLOBAL 01 G2): COBOL6862, the same text;
- `refute-a/s4-g-fd.cob` (subordinate FX of a record of a GLOBAL FD, and FX under a local working-storage group):
  `error COBOL6862: data-name 'FX' names this item and also a GLOBAL item described in the same source element ...`.

The adjudicator's `work-a/r3-glob-sub.cob` (a GLOBAL 01's subordinate SUBN reused under a local 01) is the same question
and is refused COBOL6862 too. The EXTERNAL half of SR4 holds on every probe (COBOL7282 for `r3-ext-bad`, `s4-x-same`,
`s4-x-fd`; `r3-ext-ok`, `s4-x-sib`, `s4-x-local` compile and run), so the adjudication decides the row.
