**2026-11-15, wave 20262045 group A: the 18 plain WRITE and REWRITE rows the FILE-phrase landing left claimed are witnessed at COBOL-2002; the note lands.**

Re-probe: each rule's 2023 twin witness (l1_write_from_space, l1c26_rewrite_relative_invalid_key, l1c26_rewrite_indexed_prime_key,
l1c38_write_alt_key_dup_order, l1c38_write_key_any_order, l1c37_write_keeps_fpi) compiled at `--std 2002` gives its `.out` unchanged, and the
two SR refusals (COBOL1720, COBOL2243) refuse at 2002: no code defect, only missing 2002 witnesses.

New goldens, expected values from the 2002 text: `cases/14.8.33@2002-REWRITE-STATEMENT/pb5579_indexed_write_rewrite_keys_2002`
(§2002:14.8.47.2 SR1, §2002:14.8.47.3 GR9, GR33, GR34, GR35, §2002:14.8.29.3 GR23, §2002:14.8.33.3 GR2, GR4, GR21, GR22, GR24c) and
`cases/14.8.47@2002-WRITE-STATEMENT/pb5579_relative_write_rewrite_2002` (§2002:14.8.47.3 GR3, GR4, GR7, §2002:14.8.33.3 GR19,
§2002:9.1.12.7); the refusals `rewrite-invalid-key-sequential-org`, `rewrite-invalid-key-relative-sequential` (2002-SR-14.8.33.2-3)
and `pb862-write-advancing-device-mnemonic` (2002-SR-14.8.47.2-16) now name the 2002 rule. Batch
`E:/COBOL-coord/scratch/w20262045a/batch-pb5579.json` (record_verdicts --edition 2002): 15 rows CONFORMS, GAP 2494 to 2479. The other three
claimed rows had been closed by other landings (2002-GR-14.8.33.3-20 by PB5602, 2002-SR-14.8.47.2-2 by PB5620, 2002-GR-14.8.47.3-14 by
PB5578), so `closes_rows` lists the 13 of the FILE-phrase landing and the 15 of this one.
