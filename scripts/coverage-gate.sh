#!/usr/bin/env bash

set -euo pipefail

threshold="${COVERAGE_THRESHOLD:-85}"
results_dir="${COVERAGE_RESULTS_DIR:-./TestResults/Coverage}"
base_sha="${BASE_SHA:-}"
head_sha="${HEAD_SHA:-HEAD}"
changed_files_override="${CHANGED_FILES:-}"

if [[ -z "${base_sha}" ]]; then
  if git rev-parse HEAD~1 >/dev/null 2>&1; then
    base_sha="HEAD~1"
  else
    echo "Unable to determine BASE_SHA; set BASE_SHA explicitly."
    exit 1
  fi
fi

if [[ ! -d "${results_dir}" ]]; then
  echo "Coverage results directory not found: ${results_dir}"
  exit 1
fi

coverage_files=()
while IFS= read -r file; do
  coverage_files+=("${file}")
done < <(find "${results_dir}" -type f -name 'coverage.cobertura.xml')

if [[ ${#coverage_files[@]} -eq 0 ]]; then
  echo "No coverage.cobertura.xml files found under ${results_dir}"
  exit 1
fi

declare -A changed_files
declare -A line_hits_by_line

if [[ -n "${changed_files_override}" ]]; then
  while IFS= read -r changed_file; do
    [[ -z "${changed_file}" ]] && continue
    [[ "${changed_file}" != *.cs ]] && continue
    [[ "${changed_file}" != src/* ]] && continue

    normalized="${changed_file#./}"
    changed_files["${normalized}"]=1

    if [[ "${normalized}" == src/* ]]; then
      without_src="${normalized#src/}"
      changed_files["${without_src}"]=1
    fi
  done < <(printf '%s\n' "${changed_files_override}")
else
  while IFS= read -r changed_file; do
    [[ -z "${changed_file}" ]] && continue
    [[ "${changed_file}" != *.cs ]] && continue
    [[ "${changed_file}" != src/* ]] && continue

    normalized="${changed_file#./}"
    changed_files["${normalized}"]=1

    if [[ "${normalized}" == src/* ]]; then
      without_src="${normalized#src/}"
      changed_files["${without_src}"]=1
    fi
  done < <(git diff --name-only "${base_sha}" "${head_sha}")
fi

if [[ ${#changed_files[@]} -eq 0 ]]; then
  echo "No changed/new C# source files under src/. Skipping changed-code coverage gate."
  exit 0
fi

for coverage_file in "${coverage_files[@]}"; do
  while IFS='|' read -r file_path line_num visits; do
    [[ -z "${file_path}" || -z "${line_num}" || -z "${visits}" ]] && continue

    file_key="${file_path}"
    if [[ "${file_key}" == /* ]]; then
      cwd="$(pwd)/"
      file_key="${file_key#${cwd}}"
    fi

    if [[ -v "changed_files[${file_key}]" ]]; then
      line_key="${file_key}|${line_num}"
      current_max_hits="${line_hits_by_line["${line_key}"]:-0}"
      if (( visits > current_max_hits )); then
        line_hits_by_line["${line_key}"]=${visits}
      fi
    fi
  done < <(
    python3 - "${coverage_file}" <<'PY'
import sys
import xml.etree.ElementTree as ET

coverage_path = sys.argv[1]
root = ET.parse(coverage_path).getroot()

for cls in root.findall('.//class'):
    filename = cls.attrib.get('filename', '').replace('\\\\', '/')
    for line in cls.findall('./lines/line'):
        number = line.attrib.get('number', '')
        hits = line.attrib.get('hits', '')
        if filename and number and hits:
            print(f"{filename}|{number}|{hits}")
PY
  )
done

declare -A line_hits
declare -A line_visits

for file in "${!changed_files[@]}"; do
  line_hits["${file}"]=0
  line_visits["${file}"]=0
done

for line_key in "${!line_hits_by_line[@]}"; do
  file_key="${line_key%|*}"
  max_hits_for_line="${line_hits_by_line["${line_key}"]:-0}"

  line_visits["${file_key}"]=$((line_visits["${file_key}"] + 1))
  if (( max_hits_for_line > 0 )); then
    line_hits["${file_key}"]=$((line_hits["${file_key}"] + 1))
  fi
done

total_lines=0
covered_lines=0
echo "Changed/new file coverage details:"

for file in "${!line_visits[@]}"; do
  visited=${line_visits["${file}"]}
  hits=${line_hits["${file}"]}

  if (( visited == 0 )); then
    file_pct="0.00"
  else
    file_pct=$(python3 - <<PY
hits = ${hits}
visited = ${visited}
print(f"{(hits / visited) * 100:.2f}")
PY
)
  fi

  echo "  ${file}: ${hits}/${visited} (${file_pct}%)"
  total_lines=$((total_lines + visited))
  covered_lines=$((covered_lines + hits))
done

if (( total_lines == 0 )); then
  echo "Changed/new files were found, but no coverable lines were reported."
  exit 1
fi

overall_pct=$(python3 - <<PY
covered = ${covered_lines}
total = ${total_lines}
print(f"{(covered / total) * 100:.2f}")
PY
)

echo "Changed/new file coverage: ${covered_lines}/${total_lines} (${overall_pct}%)"
echo "Threshold: ${threshold}%"

if python3 - <<PY
coverage = float("${overall_pct}")
threshold = float("${threshold}")
import sys
sys.exit(0 if coverage >= threshold else 1)
PY
then
  echo "Coverage gate passed."
else
  echo "Coverage gate failed: changed/new file coverage ${overall_pct}% is below ${threshold}%."
  exit 1
fi
