## Step 9b built (branch `listing-step-9b`, w20262104s): the close-out; PB5925 landed

Step 9b of `docs/rearchitecture/DESIGN-listing.md` section 11, on step 7b's branch (the two land as one pull request).

- **The HTML page's name links** were built with step 7b (their model, `ListingLine.Names` and `ListingCrossReference`, came with it). Step 9b holds
  them: `HtmlListingRendererTests.EveryKindOfName_LinksToItsDefinition_AndItsRowListsItsReferencesAsLinks` reads `ListingSymbolKind` off the enum, so
  a kind added later has its links checked the day it exists; every expected link is computed from the document's own references. Shown red by
  mutation (the link removed); the colour spans, the sticky-nav padding, the exception-region rows and the streaming writer are untouched and their
  tests green.
- **PB5947 and PB5941 are `landed`** (`work.py` shows both); not rebuilt.
- **The `All` budget (PB5953, landed; PB5962 open):** the cross-reference's share cut at its roots, figures in the benchmark README's step-9b table.
  The classification of the 215,230 occurrences of the 374 NIST programs is byte-identical before and after the change (`cmp` of two dumps); the
  pairing oracle is red 369 of 374 with the place link broken and green restored.
- **Every section of the design checked** against section 11's built list and its "What is not built" list, section by section, and against the
  tests of section 10; no section is unbuilt. This was a reading of the design's own account against the code and the records, not a line-by-line
  re-audit of each section's rules.
- **The residue, each a filed item** (the 7b-2 note's leads, by number): lead 1 and 4 and 5 built by 7b-2's review round; lead 2 PB5956; lead 3
  PB5957; lead 6 PB5959; lead 7 PB5960; lead 8 PB5961 (analysis); the `All` budget's rest PB5962. The decision that PB5925 is landed with them:
  kb/Work PB5954.
- Documents: DESIGN-listing.md (banner, 6.7.1 "classification asks the places the visitor asks", 7.2, 9, 11), docs/DESIGN.md section 0.5,
  DOC-A.1-117, the user guide (links).
