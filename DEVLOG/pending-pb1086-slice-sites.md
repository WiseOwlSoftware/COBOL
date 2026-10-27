## PB1086 slices 4, 5, 7 and 8 declare their sites

The four open slice notes of the external-repository chain (kb/Work PB2100, PB2101, PB2103, PB2104) carried no `**Sites:**` block, so `plan_wave.py --cluster PB1086` planned PB2100 as UNPARTITIONED, a group the dispatch ledger's partition holds nothing for, free to collide with any parallel group. Each note now names the files and members its slice changes, taken from DESIGN-external-repository.md section 12 and checked against the tree; files a slice creates are named by the existing files beside them and the design sections that create them.

PB2100 also says NEEDS-OPUS for slice 4's restricted-pointer typing (design 6.4 parts 1-3), on the owner's approval of the chain relayed by the Mythos lane on 2026-10-27, so the routing rule sends it to an Opus implementer. The dry-run plan now shows group A on opus with its partition file set and no UNPARTITIONED line.
