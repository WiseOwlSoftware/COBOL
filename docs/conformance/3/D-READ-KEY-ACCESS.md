- **D-READ-KEY-ACCESS — a READ with a KEY phrase on a file whose access mode is SEQUENTIAL is refused** (kb/Work
  PB3569; COBOL0864, every edition). *The question:* does ISO bar a Format 2 READ on an indexed file in sequential
  access mode? *The determination, one reading of SR8 and GR19 across the compiler:* §2023:14.9.30.3 SR8 ("If neither the
  NEXT phrase nor the PREVIOUS phrase is specified and ACCESS MODE SEQUENTIAL is specified in the file control entry
  for file-name-1, the NEXT phrase is implied") supplies the NEXT phrase of a READ on a SEQUENTIAL-access file, with
  §2023:12.4.5.5.3 GR1 assuming sequential access when the ACCESS MODE clause is absent, and §2023:14.9.30.4 GR19 ("An implicit
  or explicit NEXT phrase or a PREVIOUS phrase results in a sequential read: otherwise, the read is a random read and
  the rules for format 2 apply") makes a READ with an implicit NEXT a sequential read. §2023:14.9.30.2 prints the KEY phrase
  in Format 2 only, so a sequential read has no KEY phrase to bind. That is the reading the READ binder already used
  (`KeyedIoBinder.BindRead` binds `ReadKind.Next` for such a file) and the one that refuses INVALID KEY on a READ of a
  sequential-organization file or on a keyed READ that binds sequential (COBOL1720, `DiagnosticCatalog.IoPhraseForbiddenHere`,
  whose rationale names both READ sites and cites SR8, SR9 and GR19: a READ that binds sequential is a Format-1 read);
  the KEY refusal states the same thing and no other. The standard's own way to read by key on a
  keyed file is §2023:12.4.5.5.3 GR3 (RANDOM) or GR4 (DYNAMIC: "accessed sequentially, randomly, or both"). The
  alternative to a refusal is a sequential read that ignores the key; that is GnuCOBOL's ("KEY ignored with sequential
  READ", a warning), and it reads a different record than the program asked for, so it is not taken (CLAUDE.md rule 1:
  GnuCOBOL settles implementor latitude only, and silently changing which record is read is a wrong answer). It is a
  hard error where the INVALID KEY screen is a strict-error/permissive-warning seam, because a dead INVALID KEY arm
  selects no record and a KEY phrase does. The message names the file's access mode (and, when the clause is absent,
  that sequential access is assumed) and the implied NEXT, and no phrase the statement wrote. A KEY phrase beside
  NEXT, PREVIOUS or ADVANCING ON LOCK is a different refusal (Format 2 prints none of them, §2023:14.9.30.2), named with
  exactly the phrases written; AT END and NOT AT END beside KEY are refused by the parser (COBOL2269, kb/Work PB3830).
  `READ f INVALID KEY` with no KEY phrase on a sequential-access keyed file draws COBOL1720 through the same seam as the
  sequential-organization READ (an error under strict, a warning under `--permissive` with the INVALID arm dead). Witnesses
  `conformance:cases/14.9.30-READ/pb3569-read-key-sequential-access-default`,
  `conformance:cases/14.9.30-READ/pb3569-read-key-sequential-access-written`,
  `conformance:cases/14.9.30-READ/pb3569-read-next-key`,
  `conformance:cases/14.9.30-READ/pb3569-read-invalid-key-sequential-access`; the legal twin on RANDOM and DYNAMIC files is
  `conformance:cases/12.4.5.6-ALTERNATE-RECORD-KEY/l1_read_key_of_reference`; the messages are pinned by
  `conformance-test:ReadKeyFormatTests`.
