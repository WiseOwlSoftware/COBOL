- **D-ITEM-SIZE — The size of one data item (§2023:4.5 — the limits of an implementation; kb/Work PB531, PB5695)**:
  ⚠ implementor-defined. One data item, elementary or group, occupies at most **134,217,728 (2^27) character positions**
  of its character image (a group's image counts a national position as the two storage characters it occupies in an
  alphanumeric group, as every group MOVE does). No edition sets a maximum: §2023:13.18.40.3 SR4 bounds only the WRITTEN
  picture character-string, SR14 only a numeric item's digit positions, an OCCURS integer only its own count (each
  integer-n is limited by D-INTEGER-CARRIERS (1)), and Annex A.1 carries no maximum-item-size item; X3.23-1985 and
  ISO/IEC 1989:2002 set none either. 2^27 is the largest power of two whose UTF-16 image stays inside .NET's
  single-object ceiling, and an item's image IS one such string (an elementary item's value, a group's image). ONE
  constant, `RecordLayout.MaxCharacterPositions`, and two screens, each where the size is first known: an elementary
  item's PICTURE expansion is `COBOL2148` (kb/Work PB531: past it the expansion died with an OutOfMemoryException), and
  a group whose subordinates and OCCURS counts make it larger is `COBOL7641`, asked once per item through
  `RecordLayout.ExceedsSizeLimit` by `CheckStorageLimitsPass`, which names the outermost item past the limit in every
  section (WORKING-STORAGE, LOCAL-STORAGE, FILE, LINKAGE, COMMUNICATION, an object's or a method's data) at every
  edition. Before `COBOL7641`, `01 T. 05 E PIC X(1000) OCCURS 2000000000.` compiled clean: its size wrapped in the
  compiler's 32-bit model and the program committed memory until the host ran out (kb/Work PB5695). The size the
  compiler computes for an item past the limit saturates rather than wraps (`RecordLayout.ExactImageWidth`), so the
  diagnostic names the true size and no later reader sees a wrapped one. A limit on the size of an item is not a
  promise that the host can allocate an item within it: a run unit that cannot obtain the storage its data needs ends
  as §2023:4.6 allows ("the limits of an implementation").
