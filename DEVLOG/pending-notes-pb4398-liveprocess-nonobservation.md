## PB4398 filed: a live holder's timeout carries no evidence

Wave 20261808's group A gated its second review-fix round on the whole population, since it is a tooling change. The
gate went RED on one Unit case of 39,225: a `RunUnitProcessBoundaryTests` holder printed no line within the 120 s
harness unit, and `LiveProcess.ReadLine` raised HARNESS NON-OBSERVATION. Conformance took 5,718 s in that gate, so the
host was loaded. The message, though, cannot tell a starved holder from a hung one. PB3108 gave `ProcessObserver`'s
timeout the host load and the child's own processor time for exactly that reason, but `LiveProcess`, the instrument
for a process that stays alive (PB2485), took the budget and not the evidence. The agent re-gated the same tip
unchanged. The owner's rule is that a load-sensitive red is a defect to root-cause, so PB4398 files the gap: one
timeout reporter for both instruments, a drift test that holds every timeout to it, and then the witness root-caused
from the evidence the new message gives.
