## PB5925: the source listing, step 4b - the listing hooks witnessed at 2002 and 2014, each by its own text

Step 4b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925; the owner's order of 2026-11-18 to build the full
listing). The 2002 and 2014 texts were read in their own transcriptions, and every rule they hold for the listing is the
2023 sentence: the LISTING directive (§2002:7.2.15.2 GR1 to GR5, §2014:7.3.16.2), the PAGE directive (§2002:7.2.16.3 GR2,
GR3, §2014:7.3.17.3), COPY SUPPRESS and the state after library text (§2002:7.1.2.3 GR4, GR5, §2014:7.2.2.3), the '/' and '*'
comment lines (§2002:6.2.6.1, §2014:6.3.6.1) and WHEN-COMPILED's one instant (§2002:15.80.2 RV3, §2014:15.92.2 RV3). Neither
edition has a PUSH, POP or DISPLAY directive or an exception-checking PERFORM (0 of each in both transcriptions). Every
citation passed `cite.py --check`.

**No code changed.** Step 4a's trace and visibility rules are edition-neutral, so the listing already met both editions.
The step is tests, records and documents. A hook whose rule is the same sentence at 2002, 2014 and 2023 is ONE body,
`ListingHookCases`, parameterized by edition; `Listing2002Tests` and `Listing2014Tests` hold seven facts each, every one
citing its own edition's rule, and `Listing2023Tests` calls the same bodies for its four shared hooks and gains the three
the others have (PAGE with no listing, the '/' line, WHEN-COMPILED). 28 facts pass. Each new case was shown able to fail:
with the GR5 restore in `ConditionalCompilationProcessor.Run.RenderCopybook` disabled, with the suppression term of
`ListingVisibility.Apply` removed, and with the PageEjectComment page break removed, the matching 2002 and 2014 facts are
red (each mutation reverted).

**Records.** Ten of the twelve 2002 listing rows are CONFORMS with `Listing2002Tests` as witnesses (`2002-FMT-7.2.15.1`,
`2002-FMT-7.2.16.1`, `2002-GR-7.1.2.3-4`, `-5`, `2002-GR-7.2.15.2-2` to `-5`, `2002-GR-7.2.16.3-2`, `2002-RV-15.80.2-3`;
2002 GAP 1183 to 1173), and `2002-GR-7.2.15.2-1`'s prose, which said no listing is produced, is restated. The 2023 rows
RV-15.99.3-3 and GR-7.3.19.4-3 gain the new 2023 witnesses. The two DOC rows did not close: a 2002 DOC row's CONFORMS owes
its own anchor `docs/CONFORMANCE.md#2002-DOC-B.1-<n>`, and section 7 is the 2023 A.1 register, whose audit refuses any other
key (tried and measured on the branch). That blocks every 2002 DOC row (157 are GAP), so it is a note of its own, kb/Work
PB5930, holding the two. Their determinations are written anyway: DOC-A.1-117 and DOC-A.1-200 now name B.1 items 90 and
165 and 2014's A.1 items 106 and 182.

**Measured.** Nothing: no file under `src/` changed, so section 9's budgets cannot move.

Docs: DESIGN-listing.md (status, section 2.2, section 9, section 10 item 2, section 11, each "(step 4b)"), the user guide's
listing chapter (the 2002 and 2014 sections, WHEN-COMPILED at every edition), DOC_INDEX and DESIGN.md section 0.5.
