- **D-ITEM-SIZE — The size of one data item (§2023:4.5 — the limits of an implementation; kb/Work PB531, PB5695)**:
  ⚠ implementor-defined. One data item, elementary or group, is within the limit when (1) its character image is at most
  **134,217,728 (2^27) character positions** (a group's image counts a national position as the two storage characters it
  occupies in an alphanumeric group, as every group MOVE does); (2) its storage, every item that redefines within it
  included (§2023:13.18.44.4 GR1: the larger of the two), is at most **134,217,728 bytes** (a national position is two
  bytes, D-N1, so a national item of more than 67,108,864 positions meets this before its image does); and (3) it holds
  at most 134,217,728 elementary items, each occurrence counted (a table of dynamic-length items occupies no fixed
  positions, but every occurrence is a string the program allocates). No edition sets a maximum: §2023:13.18.40.3 SR4
  bounds only the WRITTEN picture character-string, SR14 only a numeric item's digit positions, an OCCURS integer only its
  own count (each integer-n is limited by D-INTEGER-CARRIERS (1)), and Annex A.1 carries no maximum-item-size item;
  X3.23-1985 and ISO/IEC 1989:2002 set none either. 2^27 is the largest power of two whose UTF-16 image stays inside .NET's
  single-object ceiling, and an item's image IS one such string; the byte bound keeps every BIT offset in an item (eight to
  a byte) an `int`. The limit is ONE set of constants in `RecordLayout` (`MaxCharacterPositions`, `MaxStorageBytes`) and
  two screens, each where the size is first known: an elementary item's PICTURE expansion is `COBOL2148` (kb/Work PB531),
  and an item whose subordinates, OCCURS counts or overlays make it larger is `COBOL7641`, decided by the one function
  `RecordLayout.ExceedsSizeLimit` in `CheckStorageLimitsPass`, which names the outermost item past the limit in every
  section (WORKING-STORAGE, LOCAL-STORAGE, FILE, SORT, LINKAGE, COMMUNICATION, an object's or a method's data) and every
  TYPEDEF template, at every edition. **The compile then stops**: the pass runs before any pass that measures a group's
  size as an `int`, and once it reports, the binding of the compilation group ends and the diagnostics so far are the
  answer. Every size the compiler takes of an item (image, bytes, storage, an OCCURS product, a group sum, a REDEFINES
  comparison, an offset, a bit extent) is computed in 64 bits by `RecordLayout`'s size section, and an item's OWN size is
  narrowed to `int` by one function, `RecordLayout.ItemSize`, which raises an internal compiler error for a value no item
  within the limit has; a size at a hypothetical count (the 2023 READ's search for the largest DEPENDING count a record
  maximum admits) is no item's size and stays in 64 bits (`BitLayout.TableBitsAtCount`) or saturates (`SaturatedBound`);
  `SizeArithmeticDriftTests` holds that arithmetic to the one file. Before `COBOL7641`, `01 T. 05 E PIC X(1000) OCCURS
  2000000000.` compiled clean (its size wrapped in the compiler's 32-bit model and the program committed memory until the
  host ran out), and an over-limit group with a REDEFINES over it crashed the compiler with an OverflowException (kb/Work
  PB5695 review R1-1). A limit on the size of an item is not a promise that the host can allocate an item within it: a run
  unit that cannot obtain the storage its data needs ends as §2023:4.6 allows ("the limits of an implementation").
