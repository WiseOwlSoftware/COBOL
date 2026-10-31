## PB4181 closed: the gate cap stays 3

The owner closed PB4181 on the reserved-host proxy series (2026-10-31): N concurrent Conformance legs in one slot gave
29.0, 33.5, 35.7, 35.6, 35.7 and 34.6 runs per hour at N = 1, 2, 3, 4, 6 and 8, so throughput saturates at 3, and N = 8
starves programs past their run budget. `DEFAULT_SLOTS` stays 3; the real-gate batches are not run. The note flips to
landed and DESIGN-test-build-ci.md section 3.14.6 records the decision.
