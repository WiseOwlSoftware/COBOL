**2026-11-15, lead from the PB5577 review (w20262044a-PB5577-report.md, R2-1, low).**


"src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ValidateBinder.cs:80 Expand restates the Table 2 edited-category predicate inline (pic.Category == NumericEdited ||
pic.IsCharacterEdited) instead of calling the existing ItemCategory.IsEditedCategory(pic), so the rule of which pictures are edited lives in two places."

Fold into design stage S4 (the edited-picture check): replace the inline predicate with `ItemCategory.IsEditedCategory(pic)`, so one place says which pictures are edited.

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ValidateBinder.cs:80` (`ValidateBinder.Expand`)
