- **D-INS3 — an INSPECT REPLACING pattern, replacement or delimiter in the storage of identifier-1 (§2023:14.9.22.4 GR18,
  Annex A.2 item 21 e)).** `cite.py --check 14.9.22.4 "If identifier-3, identifier-4, or identifier-5 occupies the same
  storage area as identifier-1, the result of the execution of this statement is undefined"` → OK 18); `cite.py
  --check A.2 "occupies the same storage area as the INSPECT identifier"` → OK 21) e). **Determination:** every
  operand value and identifier-1's image are read when the REPLACING pass starts (GR6, GR9), the comparison cycle
  matches that image, and the single store at the end is the only write, so an operand never observes a replacement
  of its own statement (`REPLACING ALL "Q" BY "R" ALL "S" BY X1(1:1)` over `QS` gives `RQ`). This was already the
  behavior and GnuCOBOL 3.2 gives the same results on every probe; no code changed. Pinned by
  `conformance:cases/14.9.22-INSPECT/pb1907_inspect_replacing_overlap`, and at 2002 by
  `conformance:cases/14.8.21@2002-INSPECT-STATEMENT/g2-inspect-replacing-overlap`.
