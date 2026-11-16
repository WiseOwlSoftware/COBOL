## PB5851: a group VALUE reaches every member when a REDEFINES lies under the group

kb/Work PB5851 (wave 20262075 A). A group item's VALUE was dropped at every edition when any entry under the group
carried REDEFINES: `01 G3 VALUE "ABCD". 05 A3 PIC X(4). 05 A4 REDEFINES A3 PIC X(4).` displayed spaces, and one pair a
level down lost the VALUE for every member of the group. §2023:13.18.63.4 GR5 and §1985:VI.5.15.6 GR3 both say "the
group area is initialized without consideration for the individual elementary or group items".

**Re-probe.** On main 19423a49a4 the note's program printed `G3=[    ]` and `G5=[0000]` at `--std 85`; a wider probe at
2023 lost the VALUE for a nested pair, a figurative `VALUE ZEROS` group, a format 2 group VALUE, a national group and a
bit group, and printed a national group redefined at level 01 as `U+4B4C U+2020`.

**Root cause.** `GroupValueSlicer.ComposedInit` sliced the area only over a subtree with no shared-storage member and
gave every other subtree the member-wise default. Its comment relied on the image lane to seed a REDEFINES backing
from "the same group", which holds only for a class rooted AT the VALUE group; a class rooted at a member was seeded
from the member's own description, which has no VALUE.

**Fix.** When an area exists the slice walk is total: `SliceInit` and `SliceBitInit` give every non-redefining member
its window through one placement, `MemberFromSlice`, which seeds a Tier-B backing from the slice
(`GroupImageCodec.BackingSeedOver`), decodes it into a typed member, and throws naming any member it cannot place. The
image lane's group arm and the inherited slice share `GroupImageCodec.AreaImageOf`, which now stores a national
group's area as two bytes per position (the sibling the sweep found). `DistributableSubtree` is deleted.
`DESIGN-data-model.md` §2.3 and `DATA_MODEL_DESIGN.md` §10 say so.

**Siblings swept.** Format 2 group VALUE and bit groups are fixed by the same walk. INITIALIZE ... TO VALUE, a BASED
record's ALLOCATE INITIALIZED (§2023:14.9.3.4 GR7) and a plain EXTERNAL record (§2023:13.18.63.4 GR4 a) take no group
VALUE and are unchanged. LOCAL-STORAGE and a RECURSIVE program's static reset take the fixed path.

**Tests.** `GroupValueAreaStorageFormDriftTests` plants a group VALUE over each storage form (typed members, each stored
tier rooted at a member in the character, national and bit units, rooted at the group, a level down, a table, a format
2 VALUE, LOCAL-STORAGE, and the 85 edition) and asks the binder that each case builds the class it names; with the old
gate put back, 12 of its 15 run cases fail. Witnesses `VI.5.15@1985-VALUE-CLAUSE/1985-GR-VI_5_15_6-3` (85) and
`13.18.63-VALUE/pb5851_group_value_over_redefines` (2023). Rows: `GR-13.18.63.4-5` and `1985-GR-VI.5.15.6-3` take the new
witnesses (both witness-only records).
