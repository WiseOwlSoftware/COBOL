## PB4903 - DEBUG-ITEM is modeled as the group its description gives, so INITIALIZE and MOVE CORRESPONDING treat it as one

**Re-probe.** On the branch build `INITIALIZE DEBUG-ITEM` stored one alphanumeric image (every member spaces, the DEBUG-SUB-n included) and
`MOVE CORRESPONDING G TO DEBUG-ITEM` drew COBOL1757 ("an elementary item").

**Root.** `ImplicitRegisters.ActivateDebugRegisters` declared DEBUG-ITEM as one alphanumeric `DataItem`, but X3.23-1985 USE FOR DEBUGGING general
rule 10 describes a group of six members with a one-character FILLER after each but the last. It now builds that group (a level-1 `DataItem` with the
eleven entries as `Children`); the three DEBUG-SUB-n members became three items instead of one shared one, so `DebugSubRegister` became
`DebugSubRegisters` through the bound tree and the dispatcher emitter (one numeric profile each).

**Fix shape.** INITIALIZE walks the register's entries by category (`InitializeBinder.BindRegisterReceiver`, now the one body of the elementary-
register arm and the group arm): DEBUG-SUB-n to zero, the alphanumeric members to spaces, FILLER left alone, a REPLACING phrase only for the
categories it names (§2023:14.9.20.4 GR4, GR5a2, GR6). MOVE/ADD CORRESPONDING gets a `CorrAccess` arm for the register whose pair places are the
register members. The renderer's group read and store arms return the register image, and `DebugItem.StoreImage` stores the FILLER entries too
(they are storage of the group; the facility still space-fills them with the rest).

**Goldens.** `pb4903-debug-item-group` (accept 85) covers MOVE and ADD CORRESPONDING both ways, INITIALIZE with and without REPLACING and the FILLER
bytes after `MOVE ALL "X"`; `pb4903-debug-item-group-removed-2002` is the refusal from 2002. Expected values are derived from the rule text.
D-DEBUG-STORE in `docs/CONFORMANCE.md` records that GnuCOBOL's one-item model is not followed.
