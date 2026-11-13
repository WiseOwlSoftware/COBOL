Review round 1, finding R1-1 (wave 20262019 group D): a signed binary or packed item was sent as its DISPLAY image with a leading minus, one character more than its size (the S is not counted, §1985:VI.5.9.4 GR8d). Fixed at --std 85: `CobolNum.FormatStandardData` gives the digits with the sign fused onto the last, used for the elementary item (`CobolStandardData.Characters`) and for a group's parts (`CobolStandardData.Image`). Golden pb5378 gained signed items; unit `StandardDataFormatTests`.

**Sites:**

- `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/CommunicationEmitter.cs` (`SendingText`)
- `src/WiseOwl.Cobol.Compiler/CodeGen/Roslyn/RuntimeApi.cs` (`StandardDataCharacters`)
- `src/WiseOwl.Cobol.Compiler/Binding/Model/ItemLength.cs` (`IsStoredAsDigits`)
- `src/WiseOwl.Cobol.Runtime/Values/StandardDataFormat.cs` (`CobolStandardData.Characters`, `Image`)
- `src/WiseOwl.Cobol.Runtime/Values/Numeric/CobolNum.cs` (`FormatStandardData`)
