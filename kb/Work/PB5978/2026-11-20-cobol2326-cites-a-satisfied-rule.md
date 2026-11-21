# PB5978 addendum, 2026-11-20: COBOL2326 cites a rule the program satisfies

Found by the refuter of the 1985 re-read (workflow wf_fcfc774f-471, slice 1985-f01); program
`E:/COBOL-coord/scratch/reread-1985-fixed/out/refute-1985-f01/src/g-own-data-vs-global-index-set.cob`, re-run by the Mythos
session on the PB5975 branch head at `--std 85`:

```
(21,12): error COBOL2326: SET 'K' TO '3' violates ISO §1985:VI.6.23.3 SR2 — "Identifier-1 and identifier-2 must each reference
an index data item or an elementary item described as an integer", and the format sends only index-name-2, identifier-2 or integer-1
```

K is an elementary integer item, so it satisfies SR2. The refusal is right for another reason: §1985:VI.6.23.4 GR3c, "If
identifier-1 does not reference an index data item, it may be set only to an occurrence number that corresponds to the value
of index-name-2. Neither identifier-2 nor integer-1 can be used in this case." The message should cite GR3c. Same family as
this note (a diagnostic citing the wrong rule), a different site: the SET format-1 operand check
(`DiagnosticCatalog` entry `set-index-assignment-operand`).
