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

declare -A line_hits
declare -A line_visits

if [[ -n "${changed_files_override}" ]]; then
  while IFS= read -r changed_file; do
    [[ -z "${changed_file}" ]] && continue
    [[ "${changed_file}" != *.cs ]] && continue
    [[ "${changed_file}" != src/* ]] && continue

    normalized="${changed_file#./}"
    line_hits["${normalized}"]=0
    line_visits["${normalized}"]=0

    if [[ "${normalized}" == src/* ]]; then
      without_src="${normalized#src/}"
      line_hits["${without_src}"]=0
      line_visits["${without_src}"]=0
    fi
  done < <(printf '%s\n' "${changed_files_override}")
else
  while IFS= read -r changed_file; do
    [[ -z "${changed_file}" ]] && continue
    [[ "${changed_file}" != *.cs ]] && continue
    [[ "${changed_file}" != src/* ]] && continue

    normalized="${changed_file#./}"
    line_hits["${normalized}"]=0
    line_visits["${normalized}"]=0

    if [[ "${normalized}" == src/* ]]; then
      without_src="${normalized#src/}"
      line_hits["${without_src}"]=0
      line_visits["${without_src}"]=0
    fi
  done < <(git diff --name-only "${base_sha}" "${head_sha}")
fi

if [[ ${#line_hits[@]} -eq 0 ]]; then
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

    if [[ -v "line_hits[${file_key}]" ]]; then
      line_visits["${file_key}"]=$((line_visits["${file_key}"] + 1))
      if (( visits > 0 )); then
        line_hits["${file_key}"]=$((line_hits["${file_key}"] + 1))
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

total_lines=0
covered_lines=0
echo "Changed/new code coverage details:"

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

echo "Changed/new code coverage: ${covered_lines}/${total_lines} (${overall_pct}%)"
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
  echo "Coverage gate failed: changed/new code coverage ${overall_pct}% is below ${threshold}%."
  exit 1
fi
