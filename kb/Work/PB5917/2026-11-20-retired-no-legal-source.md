# PB5917 - RETIRED (wave 20262114 group A): no legal 85 program reaches the rule; the one extra line is a cascade

**Re-probe (Debug build, `p1.cob` = the pb445 key program in fixed form, `--std 85`).** The refusal reproduces, together with three others:

```
p1.cob(6,11): error COBOL2787: OCCURS ASCENDING KEY IS 'G' on 'E': the key is a variable-length group ...
p1.cob(14,12): error COBOL1965: SEARCH ALL 'E': 'G (IX)' is not referenced in the KEY phrase ...
p1.cob(14,12): error COBOL2492: the second operand is not a group item: a variable-length group may be compared only with a compatible GROUP
p1.cob(10,29): error COBOL0900: DYNAMIC LENGTH elementary item requires COBOL-2014 (targeting COBOL-85)
```

**Why the note's harm does not hold.** `VariableLengthCompatibility.IsVariableLength` is true only for a group with a dynamic-length
item or a dynamic-capacity table as a subordinate (§2023:8.5.1.12.1, `DataItem.HasVariableLengthSubordinate`); both are declarable from
2014 only (`COBOL0900` above for DYNAMIC LENGTH). So at 85 and 2002 the group the rule refuses cannot be written in a program the
edition accepts: the program is already refused for its declaration, and the SEARCH ALL line is one of four consequences of that
refusal, as COBOL2787 and COBOL2492 are. No legal source is rejected (`rejects_legal_source: false`), and 2002 stays frozen. The rule's
own wording in the line cites §2014:14.9.34.2 SR10 at 85 (`SearchAllFormat2Rules.Rule`), so it does not claim an 85 rule it lacks.

`pb445-search-all-key-variable-length` and `-sending-variable-length` keep `refuse-at: ["2014", "2023"]`, the editions at which the program
exists. No code change.
