## PB3229: a diagnostic names a category or a usage in the standard's words, never the enum member's

Wave 20262135, group D (PB1754 campaign). The group's eleven notes are eleven mechanisms; this branch lands the first
in harm order, PB3229, and hands the other ten to a fresh agent.

**Re-probe.** The note's UDF scenario no longer reproduces (PB1400 and PB3192 already name a BY VALUE argument's class
through `IntrinsicArgumentRules.ClassNameOf`). The mechanism still did: `01 E PIC ZZ9 SIGN LEADING.` printed
`COBOL2422 … this entry is of category numericedited (PICTURE ZZ9)`. A sweep found 18 more message sites that
interpolated a `PicCategory` member's C# name ("numericedited", "pointer" for a data-pointer, "Alphanumeric") and five
that did the same with a `Usage` member ("PACKED", "Comp5").

**Fix.** `ItemCategory.CategoryName` is the one spelling of a §2023:8.5.2.1 Table 2 category. It gains a `PicCategory`
overload for a value without a PICTURE (a literal, a constant, a function result), and its `Table16Operand` overload is
now public. Every binder message reads it, and every usage word reads `UsageFamilies.UsageWord`.
`SpecialNamesBinder.Literals` also said "class" where it meant category. `IntrinsicArgumentRules.Name` lost its
`ToLowerInvariant` fallback: the switch covers every `CobolClass` member, and a new member throws until it is spelled.

**Drift and golden.** `ClassSpellingDriftTests` gains a category sweep and a usage sweep, each with a theory that pins the
forms the sites were written in. Exception text and `string.Create` identity keys are exempt. The golden
`13.18.52-SIGN/pb3229-sign-numeric-edited-category` is refused at all four editions with `this entry is of category
numeric-edited`.
