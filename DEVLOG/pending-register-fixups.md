## Register fix-ups: PB3340's duplicated rows removed; PB3108's CI-log sentence corrected

PB3340's `inventory_rows` listed five rows twice (18 entries for 13 rows); each id now appears once, and PB3556 records the removal and stays open for the register check that missed it. PB3108 no longer says CI does not upload the harness observation log: since its fix (PR #413) every CI test step uploads it as an artifact, which `CiHarnessLogDriftTests` checks.
