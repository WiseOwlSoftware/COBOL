## File the PB3533 leads as kb/Work notes (PB3576 to PB3579)

The PB3533 implementer's report and its branch review left five leads. They are now four notes, one per mechanism. PB3576
holds the lock table keyed by path alone (`PhysicalFileTable`) and review lead R1-1, because the kept state of a renamed-away
file is ended, not retired, only while that table stays path-keyed. PB3577 holds the `record struct` regex in
`scripts/spec/impacted_tests.py`. PB3578 holds the unverified lead that an ended kept state leaves the registry's unpublished
record locks. PB3579 holds the two OPEN paths PB3533 reaches without a witness (R1-2). No code changed; PB3580 stays unused.
