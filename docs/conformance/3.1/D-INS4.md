- **D-INS4 — an INSPECT CONVERTING from-set, to-set or delimiter in the storage of identifier-1 (§2023:14.9.22.4 GR21,
  Annex A.2 item 21 f)).** `cite.py --check 14.9.22.4 "If identifier-4, identifier-6, or identifier-7 occupies the same
  storage area as identifier-1, the result of the execution of this statement is undefined"` → OK 21); `cite.py
  --check A.2 "the CONVERTING identifier, TO identifier, AFTER identifier, or BEFORE identifier occupies the same
  storage area as the INSPECT identifier"` → OK 21) f). **Determination:** as D-INS3 — the sets and the delimiters are
  the values at the start of the statement, a repeated from-set character maps by its first occurrence (GR23, `cite.py
  --check 14.9.22.4 "the first occurrence of the character is used for replacement"` → OK 23)), and the one store at
  the end is the only write (`CONVERTING X5 TO "WXYZ"` over `ABCA` gives `WXYW`). Already the behavior; GnuCOBOL 3.2
  agrees on every probe; no code changed. Pinned by `conformance:cases/14.9.22-INSPECT/pb1907_inspect_converting_overlap`.
