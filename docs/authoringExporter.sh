BASE=/Users/todorus/Documents/todorus/CyberpunkJam013/surveillance-state-godot
AUTHORING=$BASE/scripts/authoring
OUTPUT=$BASE/docs/authoring-export.md

echo "# Authoring Layer – Source Export" > "$OUTPUT"
echo "" >> "$OUTPUT"
echo "_Generated: $(date '+%Y-%m-%d')_" >> "$OUTPUT"
echo "" >> "$OUTPUT"
echo "---" >> "$OUTPUT"
echo "" >> "$OUTPUT"

find "$AUTHORING" -name "*.cs" ! -name "*.uid" | sort | while read -r FILE; do
  REL="${FILE#$BASE/}"
  echo "## \`$REL\`" >> "$OUTPUT"
  echo "" >> "$OUTPUT"
  echo '```csharp' >> "$OUTPUT"
  cat "$FILE" >> "$OUTPUT"
  echo "" >> "$OUTPUT"
  echo '```' >> "$OUTPUT"
  echo "" >> "$OUTPUT"
done

echo "Done. Lines written: $(wc -l < "$OUTPUT")"

