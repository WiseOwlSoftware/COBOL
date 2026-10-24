## PB3518 — an exhausted column publication on Windows no longer hides a conflict Table 19 refuses

The review of PB3498 flagged a witness (`HandleColumnLockHostDriftTests`) that asserted `Unavailable` for a SHARING WITH ALL OTHER I-O
request while its own setup kept a READ ONLY INPUT column, which Table 19 (ISO 14.9.27.4) refuses that request against. Re-probed on a
fresh build, the witness was pinning a real defect: `HandleColumnLockHost.Establish` returned `Unavailable` as soon as `Publish` ran out
of tries, before it tested the refused columns, so the OPEN went on, with the share modes alone, beside a connector the table refuses it
against (9.1.13.9 1) '61'). The lead's open question (61 or the status `Unavailable` maps to) is answered by the rule: a refusal is a
conflict with another connector's published column, and failing to publish is a host limit, not a conflict; neither implies the other.

Fix: `Unpublishable` runs the refused-column test (the asker's own bytes left out, `OwnBytes`) when the column's or the owed-writes
byte's publication is exhausted, answering `Refused` when another holder is seen and `Unavailable` otherwise. The same test serves the
established path and the NO OTHER give-way (`AnotherHoldsARefusedColumn`), which removes three copies of the loop. The Linux host
already answers `Refused` for a held byte of its own column, and the sidecar host already tests when it may not write its lock
directory. A first request, a superseding request (the kept column stands) and an owed-writes failure after the column was published
(the column byte is given back) are all covered by the new
`APublicationThatEveryTryCollidesInIsRefusedByAHeldColumnTable19Refuses`, which was red before the fix (Expected Refused, Actual
Unavailable). The existing witness now exercises the exhaustion with no held column, where `Unavailable` is right.
`DESIGN-runtime-library.md` states the rule.

Self-review: the sidecar host's own exhausted `Publish` (it returns null for "the host takes no lock here" and for 12 lost races
alike) still answers `Unavailable` without testing; that is reported as a lead because the two causes cannot be told apart at the call.
