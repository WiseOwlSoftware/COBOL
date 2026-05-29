#!/bin/bash
# Extract COPY-library members (copybooks) from newcob.val into a copy library dir.
# NIST CCVS stores library text between '*HEADER,CLBRY,<name>' and '*END-OF,<name>'.
# Members are written verbatim (fixed-form, columns 1-80); the compiler's
# reference-format normalization strips sequence/identification areas at COPY time.
#
# Usage: extract-nist-copylib.sh <newcob.val> <output-dir>

INPUT="$1"
OUTDIR="$2"

if [ -z "$INPUT" ] || [ -z "$OUTDIR" ]; then
    echo "Usage: extract-nist-copylib.sh <newcob.val> <output-dir>"
    exit 1
fi

mkdir -p "$OUTDIR"

CURRENT_FILE=""
COUNT=0

while IFS= read -r line; do
    if [[ "$line" == \*HEADER,CLBRY,* ]]; then
        NAME=$(echo "$line" | sed 's/\*HEADER,CLBRY,//' | cut -d',' -f1 | tr -d ' \r')
        CURRENT_FILE="$OUTDIR/${NAME}.cpy"
        COUNT=$((COUNT + 1))
        : > "$CURRENT_FILE"   # truncate/create
        continue
    fi

    if [[ "$line" == \*END-OF,* ]]; then
        CURRENT_FILE=""
        continue
    fi

    if [[ "$line" == \*HEADER,* ]]; then
        # A different (non-CLBRY) member begins — stop writing.
        CURRENT_FILE=""
        continue
    fi

    if [ -n "$CURRENT_FILE" ]; then
        printf '%s\n' "$line" >> "$CURRENT_FILE"
    fi
done < "$INPUT"

echo "Extracted $COUNT copy-library members to $OUTDIR"
