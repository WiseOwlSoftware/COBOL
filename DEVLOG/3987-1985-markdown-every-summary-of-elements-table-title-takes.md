## Entry 3987 — 2026-11-23 08:14 PST — 1985 Markdown: every summary-of-elements table title takes one form, and the heading lint holds it

The third transcription pass (kb/Work PB6051) left the summary-of-elements table titles in three forms: 30 plain underlined
lines, and 23 promoted to `#` or `##` headings (pages 34 and 37 among them), while the printed pages 31 to 38 set the title
the same way on every page as the table's own title. All 53 are now the plain line `<u>SUMMARY OF ELEMENTS IN ... MODULE</u>`.

`scripts/spec/heading_levels.py` generalizes PB6095's format-title check into `TITLE_FORMS`, one (detector, form) pair per
repeated page title, so the general-format titles and the summary-table titles are checked by one loop. At origin/main the
lint names the 23 headed titles; on the branch it is GREEN. Selftest case_07 pins the form.
The reconciliation ledger records it (23 `summary-title-form(PB6051)` sweeps; the 21 `summary-titles(p31-1)` sweeps that
recorded a headed title now record the plain form). TRANSCRIPTION-STATE.md states the rule. The fourth pass over the repaired text is still owed; PB6051 stays open.

