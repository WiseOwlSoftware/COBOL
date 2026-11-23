## 2026-11-23 resume unit: one of two PB2725 branches is abandoned

Two SPLIT branches carry the same work: the predecessor's PB2723 GR3 unify (the four commits of
worktree-wf_46d3a156-23e-10) and a PB2725 fix (one `quotedLiteral` grammar rule, one `QuotedLiteral` reader).

- worktree-wf_6f9eca3a-cc1-19 (report w20262143a-PB2723, head cbef125376) is kept. It is cut from c3eb4a6a05 and
  gated GREEN at its tip (run 000003-20261123T082332Z). Its own commits are only the GR3 unify and PB2725.
- worktree-wf_780ee655-772-1 ABANDONED: it duplicates cc1-19. Its own commits since 7eff8ed8d8 (9c41384828,
  e7642d84ef, 50f207f81f) are PB2725 alone, done independently of cc1-19. Everything else it carries lands another
  way: the GR3 unify through cc1-19, PB6088 is on main (worktree-wf_6f9eca3a-cc1-4), and PB5721/PB5720/PB5613/PB6039
  through worktree-wf_af497aee-26c-14, which is WAITING TO LAND. It is cut from the older ae1136f781.

How the duplicate arose (timestamps, not yet a diagnosis): 772-1 (wave 20262145) wrote its SPLIT report at
2026-11-22 22:48 PST with PB2725 already committed (9c41384828, 22:18). cc1-19 is group A of the earlier-planned wave
20262143, and its first own commit is b45c8e769e at 23:59, so group A started after 772-1 had finished, still naming
23e-10 as its predecessor. Whether a queued group re-checks its predecessor before it starts is a planner question for
the next wave unit to check.
