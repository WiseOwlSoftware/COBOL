## Built (wave 20262087 group A, 2026-11-16)

**Re-probe (this tree, before the fix).** `mvsign.cob` at `--std 85` and at `--std 2023` printed `LIT->L  [+00]`,
`S3->L   [+00]`, `S3->T   [00+]`, `S3->P   [0{]` and `S1->L   [-01]`. The note's observation reproduced. After the fix,
85 prints `-00`, `-00`, `00-`, `0}` and `-01`, and 2023 is unchanged.

**Boundary.** §2002:14.8.24.3 GR4b1 has the 1985 sentence. The value's sign arrives with §2014:14.9.24.3 GR6d2a, so the
change is in 2014 (`DialectBehavior.MoveSignIsTheValuesSign`, VCR row 7.85).

**Sites.** `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/MoveEmitter.cs` (`ConvertSource`, `SendersSign`),
`src/WiseOwl.Cobol.Compiler/Binding/Bound/SendersSignStore.cs` (`MayHandNegativeZero`, `KeepsSendersSign`),
`src/WiseOwl.Cobol.Compiler/Binding/Passes/StorageFormPass.cs` (`PromoteNegativeZeroReceivers`),
`src/WiseOwl.Cobol.Compiler/Binding/Passes/UsageCollectionPass.cs` (`Visitor.SignStores`),
`src/WiseOwl.Cobol.Runtime/Values/Numeric/CobolNum.Image.cs` (`FormatImageSendersSign`). The design is DESIGN-data-model §2.1.1.

**Closes.** `1985-GR-VI.6.19.4-4`. Its witness is `VI.6.19@1985-MOVE-STATEMENT/pb5852_move_sign_of_sending_operand`, with
the 2023 twin `14.9.25-MOVE/pb5852_move_sign_of_numeric_value_2023`.

**Open question for the dispatching session.** Is a literal written `-0` a negative sending operand at 1985?
§1985:IV.4.2.2.2.2 SR2 lets a sign be written, and GR4 makes the value "the algebraic quantity represented by the
characters". Neither decides the question, so the literal keeps its normalized positive sign (kb/Work PB2508).
