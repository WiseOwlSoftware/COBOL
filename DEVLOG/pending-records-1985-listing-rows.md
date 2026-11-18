## The 1985 inventory counts the missing listing: three rows leave CONFORMS

§1985:IV.7.1 says each compiler "produces an output listing of the source program in reference format", and the compiler
produces none. The 1985 inventory had said 0 GAP because the listing requirement is prose with no numbered rule and so
was not in the catalog, and because three rows leaned on a determination 1985 does not have. kb/Work PB5923 holds the
ruling; this change records the rows it reaches.

- 1985-GR-VI.3.4.4-1 (DATE-COMPILED, §1985:VI.3.4.4 GR1) went from CONFORMS to NOT-IMPLEMENTED: the date is meant for the
  listing, and there is none. Its derivation (DRV-1985-GR-VI.3.4.4-1, unpopulatable-antecedent) cited A.1 item 117, a
  2023 implementor-defined determination that 1985 lacks; the derivation is withdrawn and its conformance entry deleted.
- 1985-GR-VI.3.3.4-1 and 1985-GR-X.3.1.4-1 (the program-name identifies "all listings") went from CONFORMS to PARTIAL:
  the source and object program half holds, the listing half has nothing to identify.
- The derivation behind 1989-RV-A.2.46.3-3 kept its verdict, but its text no longer says no listing conforms at 1985; it
  names the 1985 absence as the PB5923 nonconformance.
- Conformance entry DOC-A.1-117 now says that at `--std 85` the missing listing is a known nonconformance (PB5923), so
  "in any edition" describes what the compiler does, not what conforms.
- PB5923 claims the three rows in `inventory_rows` while it is open.

1985 inventory line before: 2275 rules, 0 GAP; after: 2275 rules, 3 GAP (CONFORMS 894 to 891, NOT-IMPLEMENTED 1,
PARTIAL 2). The state counts a non-resolving verdict as GAP, so the three rows are three GAP, not one GAP and two other.
