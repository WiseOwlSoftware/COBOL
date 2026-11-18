## Step 4b built (branch `listing-step-4b`, w20262104e)

Step 4b of `docs/rearchitecture/DESIGN-listing.md` (2002 and 2014) is built. No product code changed: step 4a's rules are
edition-neutral and the 2002 and 2014 listing rules are the 2023 sentences, each read in its own edition's text.

- Tests: `ListingHookCases` (one body per hook, parameterized by edition), `Listing2002Tests` and `Listing2014Tests` (seven
  facts each, citing their own edition), `Listing2023Tests` sharing the bodies and gaining three cases. Each new case shown
  able to fail by a mutation of the code it observes.
- Records: ten 2002 listing rows CONFORMS (2002 GAP 1183 -> 1173); `2002-GR-7.2.15.2-1` restated; two 2023 witnesses added.
- The two 2002 DOC rows (`2002-DOC-B.1-90`, `-165`) are held by kb/Work PB5930: a 2002 DOC row cannot have its own
  register anchor while section 7 is the 2023 A.1 register. Their determinations are documented in DOC-A.1-117 and
  DOC-A.1-200 (B.1 items 90 and 165, 2014 A.1 items 106 and 182).

Step 5 (options, program map, statistics and verbs) is next, after this step lands.
