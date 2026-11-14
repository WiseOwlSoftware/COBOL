---
heading: "## 6. Maintenance"
---
Update this document in the same change set as any change to the supported surface (a new usage, a facility
newly implemented or newly documented as non-support, an I-O status determination). The declined-facility
sites — every one of them routed through `EditionContext.Declined`, whose refuse rule (`EditionContext.Refuses`: the
descriptor's severity, with a `PermissiveInert` refusal accepted under `--permissive`) chooses warn-inert from
refuse — are the code-side counterpart; keep the two in sync. This file is referenced by
`docs/VERSION_CHANGE_REFERENCE.md` (the edition-change checklist) and by `docs/DOC_INDEX.md`.
