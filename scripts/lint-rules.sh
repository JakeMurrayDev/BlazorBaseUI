#!/usr/bin/env bash
# BlazorBaseUI Lint Rules
# Enforces coding standards from AGENTS.md
# Usage: bash scripts/lint-rules.sh [--rule N]

set -u

# Colors (disabled if not a terminal)
if [ -t 1 ]; then
  RED='\033[0;31m'
  GREEN='\033[0;32m'
  YELLOW='\033[0;33m'
  CYAN='\033[0;36m'
  NC='\033[0m'
else
  RED='' GREEN='' YELLOW='' CYAN='' NC=''
fi

REPO_ROOT="$(cd "$(git rev-parse --show-toplevel)" && pwd)"
SRC_DIR="$REPO_ROOT/src/BlazorBaseUI"
RULE_FILTER=""
VIOLATIONS_FILE=$(mktemp)
trap 'rm -f "$VIOLATIONS_FILE"' EXIT

declare -A RULE_NAMES
RULE_NAMES[1]="No logic in stubs"
RULE_NAMES[2]="No async void"
RULE_NAMES[3]="Lazy JS module"
RULE_NAMES[4]="No partition comments"
RULE_NAMES[5]="Data attribute prefix"
RULE_NAMES[6]="No empty-string standalone attrs"
RULE_NAMES[7]="Empty class bodies"
RULE_NAMES[9]="Circuit-safe JS guard"
RULE_NAMES[10]="Cascading params private"
RULE_NAMES[11]="Classes/records must be sealed"
RULE_NAMES[12]="Element.HasValue guard"
RULE_NAMES[13]="Lazy module guard in Dispose"
RULE_NAMES[14]="No underscore prefix"
RULE_NAMES[15]="Lifecycle inheritdoc"

for key in "${!RULE_NAMES[@]}"; do : ; done

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --rule) RULE_FILTER="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

should_run() {
  [[ -z "$RULE_FILTER" || "$RULE_FILTER" == "$1" ]]
}

check_suppression() {
  local file="$1" line_num="$2" rule_num="$3"
  local prev_line=$((line_num - 1))
  local current_line
  current_line=$(sed -n "${line_num}p" "$file")
  if echo "$current_line" | grep -q "lint-ignore:RULE-${rule_num}"; then
    return 0
  fi
  if [ "$prev_line" -gt 0 ]; then
    local prev
    prev=$(sed -n "${prev_line}p" "$file")
    if echo "$prev" | grep -q "lint-ignore:RULE-${rule_num}"; then
      return 0
    fi
  fi
  return 1
}

report() {
  local rule_num="$1" file="$2" line="$3" message="$4"
  local rel_path="${file#$REPO_ROOT/}"
  if check_suppression "$file" "$line" "$rule_num"; then
    return
  fi
  local formatted
  formatted=$(printf "[RULE-%02d] %s:%s - %s" "$rule_num" "$rel_path" "$line" "$message")
  echo -e "${RED}${formatted}${NC}"
  echo "RULE-${rule_num}" >> "$VIOLATIONS_FILE"
}

# ============================================================
# R01: No logic in component stubs
# ============================================================
check_rule_01() {
  while IFS= read -r -d '' cs_file; do
    local base="${cs_file%.cs}"
    local razor_file="${base}.razor"
    [[ -f "$razor_file" ]] || continue

    # Exclude non-stub files
    local fname
    fname=$(basename "$cs_file")
    case "$fname" in
      *Context.cs|*State.cs|*State\`*.cs|Enumerations.cs|EventArgs.cs|Extensions.cs) continue ;;
    esac

    # Check if file contains method bodies or property accessors beyond the stub
    # A valid stub has: namespace, using, XML docs, attributes, partial class declaration with ;
    # Flag if we find { } blocks that aren't empty class bodies
    if grep -Pn '(public|private|protected|internal)\s+(override\s+)?(async\s+)?(void|Task|ValueTask|bool|int|string|object)\s+\w+\s*\(' "$cs_file" | grep -v 'partial' > /dev/null 2>&1; then
      local line_num
      line_num=$(grep -Pn '(public|private|protected|internal)\s+(override\s+)?(async\s+)?(void|Task|ValueTask|bool|int|string|object)\s+\w+\s*\(' "$cs_file" | grep -v 'partial' | head -1 | cut -d: -f1)
      if [ -n "$line_num" ]; then
        report 1 "$cs_file" "$line_num" "Component stub contains method definitions — logic should be in .razor @code block"
      fi
    fi
  done < <(find "$SRC_DIR" -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Base/*" -print0)
}

# ============================================================
# R02: No async void
# ============================================================
check_rule_02() {
  while IFS= read -r -d '' file; do
    grep -n "async void" "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 2 "$file" "$line_num" "async void found — use async Task or async ValueTask"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R03: Lazy JS module pattern
# ============================================================
check_rule_03() {
  while IFS= read -r -d '' file; do
    grep -n "IJSObjectReference" "$file" 2>/dev/null | grep -v "Lazy<Task<IJSObjectReference>>" | grep -v "InvokeAsync<IJSObjectReference>" | grep -v "//.*IJSObjectReference" | while IFS=: read -r line_num content; do
      # Only flag field/variable declarations
      if echo "$content" | grep -qP '(private|readonly|internal|public)\s+.*IJSObjectReference'; then
        report 3 "$file" "$line_num" "IJSObjectReference not wrapped in Lazy<Task<IJSObjectReference>>"
      fi
    done
  done < <(find "$SRC_DIR" -name "*.razor" -not -path "*/obj/*" -print0)
}

# ============================================================
# R04: No partition comments
# ============================================================
check_rule_04() {
  while IFS= read -r -d '' file; do
    grep -nP '//\s*={3,}|//\s*-{3,}' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 4 "$file" "$line_num" "Partition comment found — do not use section dividers"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R05: Data attribute prefix
# ============================================================
check_rule_05() {
  while IFS= read -r -d '' file; do
    grep -n "data-base-ui-" "$file" 2>/dev/null | grep -v "data-blazor-base-ui-" | while IFS=: read -r line_num _; do
      report 5 "$file" "$line_num" "Uses 'data-base-ui-' instead of 'data-blazor-base-ui-'"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" -o -name "*.js" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R06: No empty-string standalone attributes
# ============================================================
check_rule_06() {
  while IFS= read -r -d '' file; do
    # Pattern 1: AddAttribute(N, "data-*", "")
    grep -nP 'AddAttribute\(.*"data-.*",\s*""\)' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned empty string in AddAttribute"
    done
    # Pattern 2: attrs["data-*"] = ""
    grep -nP '\["data-.*"\]\s*=\s*""' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned empty string"
    done
    # Pattern 3: attrs["data-*"] = string.Empty
    grep -nP '\["data-.*"\]\s*=\s*string\.Empty' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned string.Empty"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R07: Empty class bodies
# ============================================================
check_rule_07() {
  while IFS= read -r -d '' file; do
    grep -nP 'class\s+\w+.*:\s*\w+.*\{\s*\}' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 7 "$file" "$line_num" "Empty class body uses { } instead of ;"
    done
  done < <(find "$SRC_DIR" -name "*.cs" -not -path "*/obj/*" -print0)
}

# ============================================================
# R09: Circuit-safe JS guard
# ============================================================
check_rule_09() {
  while IFS= read -r -d '' file; do
    # Find module.InvokeVoidAsync / module.InvokeAsync< calls
    grep -nP '\w+\.Invoke(Void)?Async[<(]' "$file" 2>/dev/null | grep -v "ComponentBase" | grep -v "EventCallback" | grep -v "InvokeAsync(StateHasChanged" | grep -v "InvokeAsync<IJSObjectReference>" | while IFS=: read -r line_num _; do
      # Check if JSDisconnectedException appears within 20 lines after
      local start=$((line_num > 20 ? line_num - 20 : 1))
      local end=$((line_num + 20))
      if ! sed -n "${start},${end}p" "$file" | grep -qE "JSDisconnectedException|TaskCanceledException"; then
        report 9 "$file" "$line_num" "JS interop call without circuit-safe try/catch (JSDisconnectedException/TaskCanceledException)"
      fi
    done
  done < <(find "$SRC_DIR" -name "*.razor" -not -path "*/obj/*" -print0)
}

# ============================================================
# R10: Cascading parameters private
# ============================================================
check_rule_10() {
  while IFS= read -r -d '' file; do
    # Find [CascadingParameter] and check next non-empty line for private
    grep -n "\[CascadingParameter\]" "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      local next_line=$((line_num + 1))
      local next_content
      next_content=$(sed -n "${next_line}p" "$file")
      if ! echo "$next_content" | grep -q "private"; then
        report 10 "$file" "$line_num" "[CascadingParameter] not followed by private accessor"
      fi
    done
  done < <(find "$SRC_DIR" -name "*.razor" -not -path "*/obj/*" -print0)
}

# ============================================================
# R11: Classes/records must be sealed
# ============================================================
check_rule_11() {
  # Pass 1: Build set of base class names (types that are inherited)
  local base_classes
  base_classes=$(grep -rP '(class|record)\s+\w+.*:\s*' "$SRC_DIR" --include="*.cs" --include="*.razor" --exclude-dir=obj --exclude-dir=bin 2>/dev/null \
    | sed -n 's/.*:\s*\([A-Z][A-Za-z0-9_]*\).*/\1/p' \
    | sort -u)

  # Pass 2: Find non-sealed, non-abstract, non-static classes/records
  while IFS= read -r -d '' file; do
    grep -nP '^\s*(public|internal)\s+(?!sealed\s)(?!abstract\s)(?!static\s)(partial\s+)?(class|record)\s+\w+' "$file" 2>/dev/null | while IFS=: read -r line_num content; do
      # Skip partial stubs with ; (they get sealed from their .razor counterpart)
      if echo "$content" | grep -qP 'partial\s+(class|record)\s+\w+.*;\s*$'; then
        continue
      fi
      # Extract class name
      local class_name
      class_name=$(echo "$content" | grep -oP '(class|record)\s+\K\w+')
      # Skip if this class is inherited by something
      if echo "$base_classes" | grep -qw "$class_name"; then
        continue
      fi
      # Skip interfaces
      if echo "$content" | grep -q "interface"; then
        continue
      fi
      report 11 "$file" "$line_num" "Class/record '$class_name' is not sealed and is not inherited"
    done
  done < <(find "$SRC_DIR" -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -print0)
}

# ============================================================
# R12: Element.HasValue guard
# ============================================================
check_rule_12() {
  while IFS= read -r -d '' file; do
    grep -n "Element\.Value" "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      local start=$((line_num > 10 ? line_num - 10 : 1))
      if ! sed -n "${start},${line_num}p" "$file" | grep -q "Element.HasValue\|Element\.Value\."; then
        # Element.Value. (property access on ElementReference) is fine without guard in some contexts
        # Only flag if no HasValue check nearby
        local content
        content=$(sed -n "${line_num}p" "$file")
        if ! echo "$content" | grep -q "Element\.Value\."; then
          report 12 "$file" "$line_num" "Element.Value used without Element.HasValue guard"
        fi
      fi
    done
  done < <(find "$SRC_DIR" -name "*.razor" -not -path "*/obj/*" -print0)
}

# ============================================================
# R13: Lazy module guard in Dispose
# ============================================================
check_rule_13() {
  while IFS= read -r -d '' file; do
    grep -n "moduleTask\.Value\|moduleTask!\.Value" "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      local start=$((line_num > 10 ? line_num - 10 : 1))
      if ! sed -n "${start},${line_num}p" "$file" | grep -q "IsValueCreated"; then
        report 13 "$file" "$line_num" "moduleTask.Value without IsValueCreated guard"
      fi
    done
  done < <(find "$SRC_DIR" -name "*.razor" -not -path "*/obj/*" -print0)
}

# ============================================================
# R14: No underscore prefix
# ============================================================
check_rule_14() {
  while IFS= read -r -d '' file; do
    # Match private field declarations with _ prefix, exclude discards (_ =) and lambdas
    grep -nP '^\s*(private|readonly)\s+.*\s+_[a-zA-Z]\w*\s*[;=]' "$file" 2>/dev/null | grep -v "_ =" | while IFS=: read -r line_num _; do
      report 14 "$file" "$line_num" "Field uses underscore prefix — project convention is no underscore"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R15: Lifecycle methods must have /// <inheritdoc />
# ============================================================
check_rule_15() {
  local lifecycle_pattern='protected\s+override\s+(async\s+)?(void|Task|ValueTask)\s+(OnInitialized|OnInitializedAsync|OnParametersSet|OnParametersSetAsync|OnAfterRender|OnAfterRenderAsync|SetParametersAsync|BuildRenderTree|Dispose|DisposeAsync)\b'
  while IFS= read -r -d '' file; do
    grep -nP "$lifecycle_pattern" "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      local prev_line=$((line_num - 1))
      if [ "$prev_line" -gt 0 ]; then
        local prev
        prev=$(sed -n "${prev_line}p" "$file")
        if ! echo "$prev" | grep -q '/// <inheritdoc'; then
          local method_name
          method_name=$(sed -n "${line_num}p" "$file" | grep -oP '(OnInitialized|OnInitializedAsync|OnParametersSet|OnParametersSetAsync|OnAfterRender|OnAfterRenderAsync|SetParametersAsync|BuildRenderTree|Dispose|DisposeAsync)')
          report 15 "$file" "$line_num" "Lifecycle override '${method_name}' missing /// <inheritdoc /> on preceding line"
        fi
      fi
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -not -path "*/bin/*" -print0)
}

# ============================================================
# Main
# ============================================================
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}BlazorBaseUI Lint Rules${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

should_run 1  && check_rule_01
should_run 2  && check_rule_02
should_run 3  && check_rule_03
should_run 4  && check_rule_04
should_run 5  && check_rule_05
should_run 6  && check_rule_06
should_run 7  && check_rule_07
should_run 9  && check_rule_09
should_run 10 && check_rule_10
should_run 11 && check_rule_11
should_run 12 && check_rule_12
should_run 13 && check_rule_13
should_run 14 && check_rule_14
should_run 15 && check_rule_15

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Summary${NC}"
echo -e "${CYAN}========================================${NC}"

TOTAL_VIOLATIONS=0
for key in 1 2 3 4 5 6 7 9 10 11 12 13 14 15; do
  local_count=$(grep -c "^RULE-${key}$" "$VIOLATIONS_FILE" 2>/dev/null) || local_count=0
  TOTAL_VIOLATIONS=$((TOTAL_VIOLATIONS + local_count))
  if [ "$local_count" -gt 0 ]; then
    color="$RED"
  else
    color="$GREEN"
  fi
  printf "${color}RULE-%02d (%s): %d violations${NC}\n" "$key" "${RULE_NAMES[$key]}" "$local_count"
done

echo ""
if [ "$TOTAL_VIOLATIONS" -gt 0 ]; then
  echo -e "${RED}Total: $TOTAL_VIOLATIONS violations${NC}"
  exit 1
else
  echo -e "${GREEN}Total: 0 violations${NC}"
  exit 0
fi
