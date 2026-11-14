- **D-DEBUG-ITEM-REFERENCE — DEBUG-ITEM is referenced only from within a debugging section** (kb/Work PB4858). X3.23-1985 USE FOR DEBUGGING
  syntax rule 11 (§1985:XV.3.2.3): "References to the special register DEBUG-ITEM are restricted to references from within a debugging section."
  The text decides (no latitude): a statement outside every debugging section that references DEBUG-ITEM or a member, whole, reference-modified or
  as a subscript or window position, is refused COBOL6442, asked at the statement funnel (`StatementBinder.BindStatement`, `ReferencesDebugItem`)
  of each statement's own references. A reference inside the section, including from a nested statement, is legal. Witness:
  `conformance:wiseowl/diagnostics/pb4858-debug-item-outside-section`.
