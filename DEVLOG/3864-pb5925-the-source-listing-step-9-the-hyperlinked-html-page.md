## Entry 3864 — 2026-11-19 16:51 PST — PB5925: the source listing, step 9 - the hyperlinked HTML page, the section visitor, and what of the design is not built

Step 9 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925) writes the listing as one HTML page: `--listing-format html`,
`BASE.lst.html`, a rendering of the same document as the text and JSON listings.

**The page.** `HtmlListingRenderer` (`src/WiseOwl.Cobol.Listing/Html`) writes one self-contained file: its style and a small script
(the filter, expand and collapse, opening the folded blocks around a link's target) inline, nothing loaded from elsewhere. Every line
of the document is a row with the text listing's number, depth, marker, file line and text. Each COPY inclusion is a folding block
under the text listing's banner, folded when the COPY statement wrote SUPPRESS. The lines the text listing does not print (not
listed, a replaced DATE-COMPILED comment-entry line, an omitted line) are on the page, greyed, in a folded block that says how many
and why; a DISPLAY directive's transfer stays outside it (§2023:7.3.12.4 GR2). Each diagnostic stands under its line with its column
marked, and the summary links to it; each statement's object code (method, IL range, instructions) folds under its line; a data
description's margin, a data map entry's facts, the program map, the object-code map and the verbs link to what they name. The markup
is well-formed XML, so the tests read the page with `XmlReader` and find each row by the renderer's id functions.

**The visitor.** Section 7's `IListingSectionVisitor<T>` is built with the second renderer that walks sections: one method per
section, one dispatch (`ListingSectionVisitor.Visit`) that a test holds total, and the text renderer visits through it too. The
sections' order and titles moved from the text renderer into `ListingSections` (`Order`, `Title`), so the page presents the sections
in the text listing's order by construction. A data map entry's facts are stated once (`DataMapText.DetailParts`), the entries they
name as parts: the text prints `#n`, the page links it.

**What the code proved about the design.** `IListingRenderer` is not built: the formats' one table already dispatches each format to
its renderer. The name links and the definitions' reference lists stand on `ListingLine.Names` and the cross-reference, which step 7b
adds; they join with it. Checking every section of the design for what is built found one part no step owned: section 6.11's table of
edition flags in the diagnostic summary (kb/Work PB5947). Section 11 now lists every part not built and the note that holds it.

**Measured** (section 9): the `all` request with the page is +53.7 to +60.0 % over no listing on a full compile (+34.4 to +40.4 %
before it); the page costs three times its size, because each format is rendered to a string before it is written (kb/Work PB5941's
dated file `2026-11-19-step-9-html-row`, with the fix: each renderer writing to the file's stream).

**PB5925 stays open.** The brief asked to flip it; step 7b, step 6b's last slices, PB5947 and PB5941 are open, and a landed PB5925
would say the listing, and with it 1985, is complete.

**Review lead carried.** Step 7b's R1-1: the `§2023:14.7.8 rule N` siblings the earlier sweep missed now name GR1 or GR2 (checked
with `cite.py --check`), in the binder, the emitter, `DATA_MODEL_DESIGN.md`, three conformance cases' comments and one inventory row.
Every other lead of the listing's review files was already fixed on main (kb/Work PB5925's dated file `2026-11-19-step-9-built`).

