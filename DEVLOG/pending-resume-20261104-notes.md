## kb/Work PB4681 and PB4682: two process defects wave 20261868 left unfiled

The resume unit of 2026-11-04 filed the two defects the wave's handoff named as unfiled. PB4681: generate.py timed out at
120 s on a fully busy host and redded ten Unit classes and the Conformance population count in group D's first gate; the
re-gate was GREEN with no code change. PB4682: a stray `caret.log` in the main checkout made `next_unit.py` choose this
resume unit; the unit read the file and deleted it, and its writer is still unknown. The third item, the prose report that
Workflow treated as SPLIT, was already filed as PB3361's fourth witness. No compiler change.
