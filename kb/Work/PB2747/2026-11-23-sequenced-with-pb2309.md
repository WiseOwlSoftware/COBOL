## Sequenced with PB2309 and PB2349 (wave 20262147, group A)

Not started by wave 20262147 group A. The note's target says to "Sequence it inside the DESIGN-architecture-review §8.3 (7) group
(PB2309 split + PB2349 facade deletion) so the emitted call text changes once under the one reviewed re-baseline", and both
PB2309 and PB2349 are still `open`. Done alone, it would re-baseline every gated statement's emitted
`ExceptionState.{flag} = true` text, and PB2349 (the static `ExceptionState` forwarders' deletion) would re-baseline the same text
again. `blocked_by` now names PB2309 and PB2349 so the planner groups the three.
