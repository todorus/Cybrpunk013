BASE=/Users/todorus/Documents/todorus/CyberpunkJam013/surveillance-state-godot
DOMAIN=$BASE/scripts/domain
OUTPUT=$BASE/docs/domain-export.md

echo "# Domain Layer – Source Export" > "$OUTPUT"
echo "" >> "$OUTPUT"
echo "_Generated: $(date '+%Y-%m-%d')_" >> "$OUTPUT"
echo "" >> "$OUTPUT"
echo "---" >> "$OUTPUT"
echo "" >> "$OUTPUT"

find "$DOMAIN" -name "*.cs" ! -name "*.uid" | sort | while read -r FILE; do
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
