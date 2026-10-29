## The leads of wave 20261743 and the owner's OMITTED decision, filed

Wave 20261743 landed PB3997 (#742) and PB3996 (#743); their implementer reports and round-1 reviews left five leads, filed here as notes with no code change.

PB4012 records the owner's decision of 2026-10-28 ("Yes, permissive accepts"): OMITTED in a Format 1 CALL is a vendor spelling with the PB162 lane, strict refuses it with COBOL0899 and permissive accepts it with the vendor-extension notice. PB3996 had set `VendorLane` false in `CallBinder.OmittedSpelling`, so `--permissive` refused what main accepted; the GnuCOBOL testsuite (run_file.at, run_misc.at) uses the spelling.

PB4013 asks for an adjudication of §13.18.63.3 SR2 for a COMP-5 item with a PICTURE (`PIC 9(4) COMP-5 VALUE 65535` draws COBOL1625 although DOC-A.1-208 gives COMP-5 its 16-bit container).

PB4014 notes that PB3997's refusal goldens pack several entries per program, so the negative arm of `AlgebraicRanges.Admits` and each level-88 form are not individually pinned. PB4015 records the stale "boolean not yet carried across a CALL" sentence in COBOL0899's catalog description. PB4016 records the misquoted GR11 in the PB3996 Format 2 golden (`cite.py --check` fails on it) and the doc comment that claims a drift-test pin that does not exist.
