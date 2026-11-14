## An area-A item that starts in area A after other text on its line is accepted at --std 85 (PB5497)

`X. Y.` with Y in position 11 was refused at `--std 85` with COBOL3734 ("follows other text of its line"). Re-probe on a Debug build of main 6cc1376ab5 reproduced it (p9d2). §1985:IV.7.3.3 says a paragraph header or paragraph-name "starts in area A of any line", and area A is positions 8-11, so the rule is about where the item starts, not whether it begins the line. `AreaPlacementPass` had folded the two together because `AreaPlacement` had one `WithinLine` value.

Fix: `AreaPlacement.WithinLine` became `WithinLineInAreaA` and `WithinLineInAreaB`; `ReferenceFormatMap.PlacementAt` picks by the tab-expanded program-text offset (`FixedFormLine.IsAreaAOffset`); `AreaPlacementPass.Screen` accepts a head that starts in area A, at line start or within the line. The COBOL3735 arm (text beginning a line in area A) is unchanged. DESIGN-frontend-grammar updated.

Goldens: `pb5497-paragraph-name-in-area-a-after-text-85` (runs, prints `IN Y`) and `pb5497-paragraph-name-in-area-b-after-text-85` (Y in position 12, COBOL3734). Rows closed: none (no inventory row claimed).

Review round 1 (R1-1, blocking): accepting every within-line head that starts in area A also accepted `X. END DECLARATIVES.`, which §1985:IV.7.5 forbids (DECLARATIVES and END DECLARATIVES "must each appear on a line by itself"); main refused it. `AreaItems.LineAlone` now marks both heads and `Screen` accepts WithinLineInAreaA only for the others; the message names the rule. Golden `pb5497-end-declaratives-after-text-85` (refuse-at 85, COBOL3734). R1-2: the COBOL3734 construct text, its description and the pass summary now say "starts in area A".
