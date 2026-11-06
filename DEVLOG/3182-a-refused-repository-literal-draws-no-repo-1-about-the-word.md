## Entry 3182 — 2026-11-05 18:55 PST — A refused REPOSITORY literal draws no REPO-1 about the word

PB4773. A specifier whose AS literal SR2 refuses (for example the zero-length literal) fell back to the declared word, so the resolution rules (SR6 class, SR9 interface, SR16 property, SR10 function) drew a COBOL5962 whose sentence said the name sought was a word, or an AS "WORD", the source never wrote. The re-probe showed all four kinds, not just the function twin the review named. The binder now registers no function specifier, defers no property check and skips the class and interface searches when the literal was refused; the word stays declared. Test: ExternalRepositoryResolutionTests. Closes no inventory row. A call of the refused function still draws COBOL1505.

