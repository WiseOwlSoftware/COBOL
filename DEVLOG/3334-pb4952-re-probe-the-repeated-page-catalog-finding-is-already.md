## Entry 3334 — 2026-11-08 14:54 PST — PB4952 re-probe: the repeated-page catalog finding is already fixed, the four missing pages need an owner copy

Item 2 (parse reports 18 duplicate ids for the repeated PDF pages 527-528) no longer reproduces: PB4886's parser skips a page marked with the `SCAN_REPEAT_85` comment, `extract_rule_catalog.py --edition 1985` prints no findings. Item 1 (folios XIII-30, XIII-31, XIII-54, XIII-55 absent from the scan) cannot be fixed in code; the note stays open pending the owner's copy. No code changed; the note records the re-probe. No gate run (inert change).

