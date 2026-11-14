---
order: 8
---
> ⚖ **DETERMINATION — a table(ALL) intrinsic argument that ranges over NO occurrence (§2023:15.3)** (2026-08-18; kb/Work
> PB62). §2023:15.3: "The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of
> the reference to the function-identifier is undefined." WiseOwl COBOL defines the undefined case: the enumeration
> (`CobolTable.AllArgs`) raises EC-ARGUMENT-FUNCTION (set when checking is on, so a USE procedure can take it) and
> terminates the reference by that name either way — an empty list is never handed to a body whose result over
> nothing is itself undefined. An OCCURS DEPENDING range whose data-name-1 is 0, or a dynamic-capacity table at
> capacity 0, is how a program reaches it.
