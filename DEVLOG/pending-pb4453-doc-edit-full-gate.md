## kb/Work PB4453: a doc edit forces the full gate; no compiler test may read Markdown

Filed PB4453 at the owner's request. Any `docs/` path makes `inert_lane.py` choose the full lane: a build plus the
whole Unit, Conformance and Characterization populations, with a test-phase median of 12.7 min. That happens even
though a doc edit cannot change the compiler. The PB4452 planner fix waited on that gate because it also edited
DESIGN-architecture-review.md. The owner: "There is no compelling reason a compiler change needs to read an markdown
note". The fix is that no compiler test reads Markdown. Doc checks become audits, data a test needs lives outside
Markdown, and docs become inert to the gate.
