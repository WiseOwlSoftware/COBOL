# The summary-of-elements table titles take one form (2026-11-23)

The third pass left one inconsistency open: pages 34 and 37 headed the summary-table title as `##` and `#` while the rest of
pages 31 to 38 print it plain. The sweep of every `SUMMARY OF ELEMENTS IN` line found 53 titles: 30 plain, 23 headed (`#` or
`##`). The page prints the title as the table's own title, repeated on every page of the table, never as a node of the
document's tree, so all 53 are now the plain underlined line `<u>SUMMARY OF ELEMENTS IN ... MODULE</u>`. The numbered clause
heading `## 2. <u>SUMMARY OF ELEMENTS BY MODULE</u>` is not one of them and is unchanged.

`heading_levels.check_format_titles` now checks each repeated page title against its one form (`TITLE_FORMS`: the
general-format titles of PB6095 and these). At origin/main it names the 23 headed titles; on this branch:
`=== HEADING LEVELS: GREEN (0 finding(s))`. Selftest case_07 pins the form, its four wrong forms and the clause heading it
must not touch (`test_heading_levels.py: PASS (8 cases, 46 expectations)`).

The fourth pass runs over the text with this repair in it. PB6051 stays open.
