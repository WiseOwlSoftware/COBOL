# PB5498 discharged: the lead is PB5302, landed

Re-probe, 2026-11-14, `cobol --std 85 --run` on main 66ff5cbc77 (Debug build 0/0), reader's `p1.cob`
(`E:/COBOL-coord/scratch/coderead-85/out-read4/work-rd-04/c1/p1.cob`):

```
A9 REACHED 1
B0 START IX=-1
B0 AT END IX=+0
```

The WHEN is evaluated at -1 (false), the incremented index 0 is outside the permissible range, and the search
ends with AT END, as §1985:VI.6.22.4 GR2b requires. The reader's "evaluated at -1, 0 and 1" described a tree before
`56417bbd82` (PB5302: the 85 advance test judges both bounds). A start index of 0 (A9) still advances to occurrence 1,
the PB5121 edition arm. Pinned by golden `14.9.37-SEARCH/pb5302_search_advance_below_table_85` (start index -1).
No source change.
