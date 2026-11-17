**2026-11-17, GR1 of §1985:XIII.3.10.7 is now the standard's text (wave 20262099 f; kb/Work PB4952's dated file of the same day).** Folio XIII-30 has been transcribed from the public GovInfo scan of X3.23-1985 (its PDF page 608), so the row `1985-GR-XIII.3.10.7-1` can be read against printed words. The printed rule:

> (1) Upper limit rule:
>
> If a report heading report group has been presented on the page on which the page heading report group is to be presented, then the first line number on which the page heading report group can be presented is one greater than the final LINE-COUNTER setting established by the report heading.
>
> Otherwise the first line number on which the page heading report group can be presented is the line number specified by the HEADING phrase of the PAGE clause.

`python scripts/spec/cite.py --check "§1985:XIII.3.10.7 GR1" "one greater than the final LINE-COUNTER setting established by the report heading"` reports OK, with no RECONSTRUCTED mark.

The note's reading 1 used the reconstruction's words ("on the page to which the report is currently positioned" and "established by the report heading report group"). The printed rule sets the same upper limit in different words. The row's store record was decided against the reconstruction, so `inventory_view.py --check` refuses it by its `rule-digest` until it is decided again against this text. Its stored code read is stale. The row is decided in the dated file `2026-11-17-landed.md`.
