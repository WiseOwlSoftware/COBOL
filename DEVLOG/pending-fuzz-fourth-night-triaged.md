## Fourth fuzz night triaged: one wrong answer and two questions filed, all held for the owner (PB3664 to PB3666)

The owner asked on 2026-10-24 for the fuzz analysis and its findings, with no fixing before explicit approval. The fourth
overnight run (fuzz-20261023-204437: qwen3.6:35b, 5,247 programs, 811 `WE_REJECT` and 424 `OUTPUT_DIFFERS` rows among
them) was re-judged on main at 300fd78bd. For the first time the refusal rows were re-judged as well, and all 811 still
refuse. Twelve Opus adjudicator slices then read 241 signatures from one rolling pool four wide: 2,031,074 subagent
tokens in 25.5 minutes, against 45 minutes for the third night's fixed chunks. No slice hit its cap, nothing was left
undecided, and no program of the third night's seven fixes regressed.

The verdicts: 114 signatures WiseOwl-correct, 54 GnuCOBOL-side, 41 latitude, 14 not comparable, 11 uninitialized, 5 model
errors and 2 WiseOwl defects. The operator re-ran both defects on the fresh judge, checked every citation and filed three
notes with `status: owner`, which no lane plans:

- PB3664 is a wrong answer. A fixed editing sign reads the sending value's sign before truncation, so `MOVE -0.001 TO PIC
  -9.99` prints `-0.00` where Table 8 (§13.18.40.5 5)) gives ` 0.00` for the zero the item holds. BLANK WHEN ZERO, Z
  suppression and floating insertion in the same `CobolEdit` call already see zero.
- PB3665 is an adjudication. STRING accepts `FUNCTION INTEGER(n)` as a sender but refuses `FUNCTION LENGTH(g)` as "a
  numeric literal". Whether an integer function meets SR1's usage-display requirement (§15.2 states no usage for it)
  decides which arm is wrong.
- PB3666 is an analysis. Alphanumeric text moved to a USAGE BIT item keeps its characters (`AB000`) while the boolean
  operators read it as zero.

The record is `docs/rearchitecture/evidence/fleet-optimization/2026-10-24-differential-fuzz-fourth-night.md`.
