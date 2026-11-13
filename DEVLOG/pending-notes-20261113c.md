## Notes batch 20261113c: PB5465, PB5466, PB5467

Three new kb/Work notes, filed from implementer reports and not fixed here. PB5465 (MAJOR, wrong answer): INVOKE BY CONTENT is not transitive across the following arguments, so `OfInvokeArgument` passes them BY REFERENCE. PB5466 (MINOR, test gap): the "without the debugging mode" direction of `§1985:VI.7.3` has no unit test. PB5467 (MINOR, tooling): `selftest_layout` does not require `ensure_utf8()` in a case file's main block. No compiler or tooling code changed.
