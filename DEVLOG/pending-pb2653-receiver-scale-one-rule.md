## PB2653: every MOVE receiver scale asks PicInfo.ReceiverScale

PB2653 (an R2 duplication finding of the PB1754 review, pinned at `cd85e4901`) asked to widen PB2163's scale-alignment
unify to the copy in `MoveEmitter.ConvertSource` and make `PicInfo.ReceiverScale` the one scale. Re-probed at
`42f7fdffb5`: the copy was already gone. PB2163's landing (the day after the pin) moved the MOVE's numeric-edited arm
onto `NumericRenderer.EditedLanding` at `pic.ReceiverScale()`. Two MoveEmitter reads still decided the receiver scale by
hand: `AllLiteralNumber` (`edited ? pic.ReceiverScale() : pic.Scale`) and `ConvertSource`'s numeric arm
(`target.OperandPic!.Scale`). Both now ask `ReceiverScale()`, which answers `Scale` for every non-edited receiver (a
LOCALE picture is always numeric-edited), so the change is behavior-neutral:
`=== ARCH-ORACLE: IDENTICAL — 23455 cases ===`. No golden (no behavior change), no inventory row
(`closes_rows: []`, behavior-neutral restructuring).
