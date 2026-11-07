## PB4813: verify_publishable reads the 2002 stamp, so the 2002 canary can be collected

**Mechanism.** `scripts/spec/verify_publishable.py` keys its per-copy stamp shapes (`FORBIDDEN`, `TREE_FORBIDDEN`) by edition,
and both tables had rows for 1985 and 2023 only. `transcribe_pdf.py collect --edition 2002` therefore stopped with "no
per-copy stamp shapes ... for the 2002 edition" and wrote none of the five pages of the ended canary batch.

**Re-probe.** The canary's `collect.log` in the private state directory ends on that FATAL. The 2002 PDF's text layer prints
the reseller's per-copy stamp as one line, the same on all 882 pages.

**Fix.** The 2002 rows are written from that line as shapes, with the variable parts as character classes. `FORBIDDEN[2002]`
has seven: the opening sentence, `Licensed to`, `order #`, an order number of six or more digits, the download date and the
two licence-terms sentences. `TREE_FORBIDDEN[2002]` has three: the order number, the date and a purchaser's name. 2023's
order-number shape stops at six digits and a word boundary, so it misses the 2002 number. That is why the 2002 row is the
edition's own and not borrowed. Every shape matches the stamp line on all 882 pages and no other line of the PDF.
`transcribe_pdf.clean_page` on the text layer drops one line from each canary page and 882 lines in all. No stamp is left.

**Self-test.** Four new checks run on an invented stamp of the 2002 shape, assembled so the tree check never reads it whole.
Every piece is refused once. 2002's order-number shape catches the longer number and 2023's does not. The tree shapes catch
the number, the date and a name. 2002 still exits at its missing acknowledgment row.

**Not done.** The canary pages are not collected. The key file at hand holds one key whose label names neither account, and
`api_key.read_run_key` correctly refuses to bill a run recorded as account2 with it.

**Self-review.** One mechanism, one table row per edition, no second copy of a shape list. No literal stamp text in any
tracked file: the planted name and number are assembled from pieces.
