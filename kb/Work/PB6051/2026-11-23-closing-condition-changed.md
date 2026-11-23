# The closing condition is PB6135's, not another full pass (2026-11-23)

The owner approved closing PB6051 on mechanical checks instead of a pass that finds nothing: every error class found has a
whole-document check GREEN in the gate, and a PDF text-layer diff reports no unexplained difference (kb/Work PB6135). No
sixth full pass runs. Pass 5's repair (branch md85-pass5) is the last pass-driven change.
