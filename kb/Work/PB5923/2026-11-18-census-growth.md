## Lead: four tolerated architecture rows grew on main between two census records (found by listing step 1)

Listing step 1 had to record a new architecture census (`docs/rearchitecture/evidence/arch-census/ac83984ddba9...json`)
because `ArchCensusDriftTests.TheNewestRecord_CoversEveryProductProject_AndItsWholeCompiledPopulation` requires the newest
record to cover every `src/` project, and `WiseOwl.Cobol.Listing` is new. The previous record was `2a581b75c6`. The new
census measured four tolerated rows of `docs/rearchitecture/architecture-edges.json` LARGER than the old one:

| Row | uses at 2a581b75c6 | at ac83984ddba9 |
|---|---|---|
| `WiseOwl.Cobol.Binding.Bound -> WiseOwl.Cobol.Binding` | 47 | 48 |
| `WiseOwl.Cobol.Binding.Bound -> WiseOwl.Cobol.Binding.Procedure` | 40 | 43 |
| `WiseOwl.Cobol.Binding.Bound -> WiseOwl.Cobol.Frontend.Generated` | 20 | 26 |
| `WiseOwl.Cobol.CodeGen -> WiseOwl.Cobol.Binding` | 66 | 68 |

No listing type is in any of them (the listing's own pairs are allowed edges of the new layers `listing` and
`compiler.listing`). The growth landed on main between the two records, unseen because no census was recorded in between:
the drift test's own walk (`walkUses`) passed throughout. The branch admitted the measured counts by hand, each row's plan
naming this file; the removers (PB2347, PB2419, PB2420, PB2399, PB2405, PB2418) are unchanged and still take the rows to
zero. Whoever next works a remover should read which pairs grew (diff the two census records' namespace edges) and
confirm each is one the remover's move already covers.
