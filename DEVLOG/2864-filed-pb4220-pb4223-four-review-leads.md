## Entry 2864 — 2026-10-31 11:23 PDT — Filed PB4220-PB4223, four review leads

Filed from the PB4162 and PB107 branch reports. PB4220 (binder, MINOR): the CAPACITY register is built by hand in both `DataBinder.CreateCompilerTempTable` and `DataBinder.DynamicResolve` (PB4162 review lead R1-2). PB4221 (binder, MINOR): the `BoundHoldTableAll` summary and `Count` doc omit the dynamic-capacity arm that leaves `Count` null (R1-3); a separate mechanism from PB4220, so two notes. PB4222 (decision): implement CLDR hiraganaQ or decline it as deprecated (PB107's remainder). PB4223 (decision): numericOrdering past 254 digits, ICU split versus full numeric value (PB107 review lead, code on PR #894). No spare id; none duplicated an existing note.

