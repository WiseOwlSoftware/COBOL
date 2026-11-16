## Entry 3745 — 2026-11-16 03:20 PST — PB5776, PB5816: STRING into a group with an OCCURS DEPENDING ON table reads its maximum length; a COBOL-85 UNSTRING from a zero-length sender is an overflow

**PB5776.** `STRING "ab" ... INTO H WITH POINTER PTR` where H holds `OCCURS 1 TO 5 DEPENDING ON HN` with HN inside H printed `OVF`
and `H=[5a    ] PTR=03` at `--std 85` and `--std 2023`, re-probed on this tree. `StringEmitter.EmitString` read identifier-3 through the
sending reader, which slices an occurs-depending group to its current extent, and stored it through the receiving writer, which
keeps the maximum when data-name-1 is inside the group (`§1985:VI.5.8.4 GR3b`; `§2023:13.18.38.4 GR8b`). The image was 2 positions
long where the receiver is 6, so GR8's overflow test fired and the store blanked the rest. `PlaceRenderer.ReceivingGroupValue`
is now the read twin of `WriteGroupValue` (one `UsesCurrentExtent` law, alphanumeric, bit and national arms) and
`StringEmitter.ReadReceiverImage` asks it. After: `H=[5ab***] PTR=04`, no overflow, at both editions. Sibling sweep: INSPECT
sizes its operand as a sending item by rule (`§2023:14.9.22.4 GR1`) and stores through the sending direction; UNSTRING receivers
store through bound MOVEs; no other verb reads a receiver and writes it back.

**PB5816.** At `--std 85`, `UNSTRING OG INTO WX WITH POINTER WP` with OG at count 0 ran neither overflow phrase. The 2002 rule
that a zero-length sender ends the statement (`§2002:14.8.44.3 GR2`) was applied at every edition; `§1985:VI.6.27.4 GR17` makes
a pointer of 1 past a size of 0 an overflow. I added `DialectBehavior.UnstringZeroLengthSenderTerminates` (changed in 2002; VCR
row 7.80) and `StringEmitter.EmitUnstring` emits the guard only when it is active. After: `OVERFLOW`, then the rest, at 85.

**Witnesses.** 85: `VI.5.8@1985-OCCURS-CLAUSE/pb5776_string_odo_group_receiver_maximum_length`,
`VI.6.27@1985-UNSTRING-STATEMENT/pb5816_unstring_zero_length_sender_overflow`. 2002: `14.8.44@2002-UNSTRING-STATEMENT/pb5816_unstring_zero_length_sender_terminates`.
2023: `13.18.38-OCCURS/pb5776_string_odo_group_receiver_maximum_length_2023`, `14.9.48-UNSTRING/pb5816_unstring_zero_length_sender_terminates_2023`.
Rows: `1985-GR-VI.5.8.4-3`, `1985-GR-VI.6.27.4-17` and `GR-13.18.38.4-8` gained their test-ref (batches under the run's scratch; 2023 GAP 10 -> 10).

