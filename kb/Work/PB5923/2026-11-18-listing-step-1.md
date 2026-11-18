## Landing: listing step 1, the 1985 standard minimum (branch `listing-step-1`, w20262104a)

Step 1 of `docs/rearchitecture/DESIGN-listing.md` section 11 is built. At `--std 85` the `cobol` command writes `<out>.lst`
by default (`--listing off` turns it off); at the later editions on request. The listing lists every physical line of the
source and of each COPY library text in reading order (§1985:IV.7.1, IV.7.4), starts a page before each '/' comment line
(§1985:IV.7.2.4), prints DATE-COMPILED's replacement `DATE-COMPILED. <instant>.` (§1985:VI.3.4.4 GR1), and names in each
page heading the innermost program of the page's first line (§1985:VI.3.3.4 GR1, §1985:X.3.1.4 GR1). One instant serves
the heading, DATE-COMPILED and WHEN-COMPILED (`CompilationInputs.ReadCompilationTime` captures once).

Records: `1985-GR-VI.3.4.4-1` NOT-IMPLEMENTED -> CONFORMS, `1985-GR-VI.3.3.4-1` and `1985-GR-X.3.1.4-1` PARTIAL ->
CONFORMS, witnessed by `Listing1985Tests`; `inventory_view.py --summary` 1985 went from "2275 rules · 3 GAP (3 open)" to
"2275 rules · 0 GAP". The unnumbered IV.7 rules stay PB5924's.

Leads left: PB5927 (re-measure the cost on a quiet host; the step-1 run was too noisy to decide the time budgets), PB5926 (the KEPT row `1989-RV-A.2.46.3-3` rests on a derivation whose "no listing" premise step 1 falsified);
`2026-11-18-census-growth.md` beside this file.
