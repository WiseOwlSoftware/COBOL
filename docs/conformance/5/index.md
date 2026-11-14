---
heading: "## 5. Annex A.4 optional-element disposition (§2023:4.2.7)"
columns: ["A.4 §", "Module", "Disposition", "Note"]
---
One row per A.4 optional module. **Claimed** = support is claimed (§2023:4.2.7/A.4.1); **Partial** = the supported /
not-supported split is itemized (the §2023:4.2.7 second-sentence requirement); **Not claimed** = the module's syntax
is not accepted (per A.4.1, optional-element syntax is accepted only when support is claimed — so a parse error
or a named error is the conforming posture). This section is the user-documentation face; §1's conformance
summary claims only what is Claimed/Partial here. A row's `derived-verdicts.<selector>` names the inventory rows the
decline covers; the selector in `tests/version-matrix/inventory-schema.json` IS that set, so this table carries no
row counts (they drifted from the selectors three times); `python scripts/spec/derive_verdict_batch.py --list` prints
each selector's live count.
