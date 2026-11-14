- **D-SRCH-DYN — which enclosing occurrence bounds a SEARCH of a dynamic-capacity table nested under other tables**
  (kb/Work PB3365). The scan of a SEARCH or SEARCH ALL ends at the table's "highest permissible occurrence number"
  (§2023:14.9.37.4 GR4, GR9), which for a dynamic-capacity table is its current capacity (§2023:8.5.1.9.1), and each occurrence of an
  enclosing table holds a capacity of its own (§2023:8.5.1.9.2). The standard names the enclosing occurrence only by §2023:14.9.37.4
  GR1 ("The subscript that is used to determine the occurrence of each superordinate table to search is specified by the
  user in the WHEN phrases") and by §2023:14.9.37.3 SR2, which lets identifier-1 write the enclosing subscripts. **WiseOwl COBOL
  bounds the scan by the occurrence identifier-1 names when it writes every enclosing subscript, otherwise by the
  occurrence the FIRST WHEN-phrase reference to the table (or to an item within it), in source order, subscripts; a
  nested dynamic-capacity table that neither names is refused (COBOL2075).** The enclosing subscripts are evaluated
  once, when the statement begins; the standard leaves a table referenced at two different enclosing occurrences in its
  WHEN phrases undefined. A fixed or occurs-depending table needs no such choice: its bound is the same for every
  occurrence of its enclosing tables. **The enclosing tables' subscripts on identifier-1 are a COBOL-2023 permission
  (kb/Work PB4609, PB4577; owner decision of 2026-11-04 on PB4547, "Revert: 2023-only").** §2023:14.9.37.3 SR2 and SR3 permit them
  (`cite.py --check 14.9.37.3 "Identifier-1 may be contained within one or more other tables, for which the subscripting is
  still required"` → OK); the 2002 text, the 2009 committee draft and the 2014 FCD 1.0 (14.9.34.2 ALL FORMATS rule 1) say "Identifier-1 shall not
  be subscripted or reference modified" (outside the tracked tree, so not `cite.py`-checkable), so the relaxation is the
  2023 text's alone. At `--std 85`, `--std 2002` and `--std 2014` a subscripted identifier-1 is COBOL0900
  (`search-identifier-1-enclosing-subscript-2023`, VCR row 7.49), refused on recognition by the parse arm of
  `VersionConformancePass` for both SEARCH formats; the bare spelling is legal at every edition, and a subscript beyond the
  enclosing count is COBOL2075 at every edition (below 2023 it also draws the COBOL0900). Witnesses: `conformance:cases/14.9.37-SEARCH/pb3365_search_nested_dynamic_table`,
  `conformance:cases/14.9.37-SEARCH/pb3365_search_nested_dynamic_property`,
  `conformance:cases/14.9.37-SEARCH/pb3365-search-nested-dynamic-no-occurrence`.
