#!/bin/bash
# Extract individual COBOL programs from newcob.val
# Each program starts with *HEADER,COBOL,<name>
# Lines are fixed-form (columns 1-80)

INPUT="$1"
OUTDIR="$2"

if [ -z "$INPUT" ] || [ -z "$OUTDIR" ]; then
    echo "Usage: extract-nist.sh <newcob.val> <output-dir>"
    exit 1
fi

mkdir -p "$OUTDIR"

CURRENT_FILE=""
COUNT=0

while IFS= read -r line; do
    if [[ "$line" == \*HEADER,COBOL,* ]]; then
        # Extract program name (first name after COBOL,)
        NAME=$(echo "$line" | sed 's/\*HEADER,COBOL,//' | cut -d',' -f1 | tr -d ' ')
        CURRENT_FILE="$OUTDIR/${NAME}.cob"
        COUNT=$((COUNT + 1))
        # Don't write the header line itself
        continue
    fi

    if [[ "$line" == \*END-OF,* ]]; then
        # End-of-program marker — stop writing this program
        CURRENT_FILE=""
        continue
    fi

    if [[ "$line" == \*HEADER,* ]]; then
        # Non-COBOL header (data files, etc.) — stop writing
        CURRENT_FILE=""
        continue
    fi

    if [ -n "$CURRENT_FILE" ]; then
        echo "$line" >> "$CURRENT_FILE"
    fi
done < "$INPUT"

echo "Extracted $COUNT COBOL programs to $OUTDIR"
