## Review r1 of the landed fix (PR #2048, 4d457e15a5): a basis of neither legal form, fixed

Review r1 (E:/COBOL-coord/scratch/review-pb6131-r1.jsonl) arrived after the branch had landed:

- **R1-1 (medium).** `adjudicated()` treated ANY basis other than exactly `mechanical` (missing, `Mechanical`, `mechanical `) as
  an adjudication, and the audit never checked the basis, so `--build` kept a broken entry forever with both verdicts GREEN.
  **Fixed:** exactly two bases are legal, `mechanical` and `adjudicated: <non-empty reason>` (the form of NC115A's 11 entries).
  `basis_problems` names any other value. `--build` keeps such an entry (it may be a decision written wrong, so it is never
  silently replaced), reports it and ends `BUILD RED`, and `--check` refuses it. Only a legal adjudication counts as kept.
- **R1-2 (low).** The self-test fixtures wrote crosswalk files their own way. **Fixed:** they write through `T.write_file`, the
  one writer.

Evidence: self-test case 22 covers the probe's cases (missing, `mechanical `, `Mechanical`) plus `adjudicated`, `adjudicated:   `,
`Adjudicated: x` and a non-string. `test_crosswalk.py` passes with 22 cases and 91 expectations. The reviewer's probe
(`review-pb6131-r1/probe2.py`) now prints a conflict and an audit problem for each of its three bases. `--build` over the whole
crosswalk prints `BUILD GREEN: wrote 459 file(s) ..., 11 adjudicated entries kept verbatim, 0 conflict(s)` and leaves
`tests/nist/crosswalk` unchanged. `--check` is GREEN with 0 problems over 459 programs.
