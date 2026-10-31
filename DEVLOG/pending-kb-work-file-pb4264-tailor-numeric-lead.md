## kb/Work: file PB4264, a hand-written .tailor weight can take the numeric lead's primary (lead of PB107's carrier, PR #909)

PB4264 (LOW, analysis) records that a numeric `.tailor` weight written at the value `root - 1` below the digit zero collides with the numeric lead that `CollationTable.NumericLead` takes at primary - 1: `CldrTailoringBuilder.Line` reserves the top of each primary gap only for tailorings it builds, and nothing guards hand-written `.tailor` values. Same root as PB107. The report's second lead, ICU's 254-digit split, is covered by PB4223 and was not filed again.
