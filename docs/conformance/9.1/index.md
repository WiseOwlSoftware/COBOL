---
heading: "### 9.1 The rules the COBOL-85 claim does not support, each with its ground"
columns: ["Rule", "Citation", "Subject", "Ground"]
---
One row per rule of the 1985 catalog whose own verdict is DOCUMENTED-NON-SUPPORT, or that is KEPT into a rule of ISO/IEC
1989:2023 with that verdict. The set is the rows', not this table's: `python scripts/spec/conformance_doc.py --check` fails
on a rule of the set with no row here, on a row whose rule has left the set, and on a row whose citation or subject is not
its rule's. A ground is a qualification of §1985:I.1.5.2.5 (the claim supports the subset "except as qualified in paragraph
1.5.2.5"), implementor latitude the rule itself grants, or a departure this statement states as one.
