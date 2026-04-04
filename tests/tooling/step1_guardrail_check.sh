#!/usr/bin/env bash
set -euo pipefail

mode="${1:-validate}"

declare -a move_pairs=(
  "src/VideoConverter.Api/Features/Profiles/Api/ProfileConversionsEndpoints.cs|src/VideoConverter.Api/Features/Profiles/Api/ProfileConversionsApi.cs"
  "src/VideoConverter.Api/Features/Profiles/Api/ProfilesEndpoints.cs|src/VideoConverter.Api/Features/Profiles/Api/ProfilesRoutes.cs"
  "src/VideoConverter.Api/Features/Conversions/Api/ConversionsEndpoints.cs|src/VideoConverter.Api/Features/Conversions/Api/ConversionsWriteApi.cs"
  "src/VideoConverter.Api/Features/Metadata/Api/ManualResolutionEndpoints.cs|src/VideoConverter.Api/Features/Metadata/Api/ManualResolutionApi.cs"
  "src/VideoConverter.Api/Features/Metadata/Api/MetadataSearchEndpoints.cs|src/VideoConverter.Api/Features/Metadata/Api/MetadataSearchRoutes.cs"
  "src/VideoConverter.Api/Features/Metadata/Api/EpisodeGroupEndpoints.cs|src/VideoConverter.Api/Features/Metadata/Api/EpisodeGroupApi.cs"
  "src/VideoConverter.Api/Features/Notifications/Api/NotificationsController.cs|src/VideoConverter.Api/Features/Notifications/Api/NotificationsApi.cs"
  "src/VideoConverter.Api/Services/Conversion/ConversionExecutionService.cs|src/VideoConverter.Api/Features/Conversions/Services/ConversionExecutionService.cs"
  "src/VideoConverter.Api/Services/Conversion/IConversionExecutionService.cs|src/VideoConverter.Api/Features/Conversions/Services/IConversionExecutionService.cs"
  "src/VideoConverter.Api/Services/Conversion/DirectoryBatchWorker.cs|src/VideoConverter.Api/Features/Conversions/Workers/DirectoryBatchWorker.cs"
  "src/VideoConverter.Api/Services/Conversion/ConversionCleanupService.cs|src/VideoConverter.Api/Features/Conversions/Cleanup/ConversionCleanupService.cs"
  "src/VideoConverter.Api/Services/Conversion/IConversionCleanupService.cs|src/VideoConverter.Api/Features/Conversions/Cleanup/IConversionCleanupService.cs"
  "src/VideoConverter.Api/Services/Conversion/DirectoryBatchCleanupService.cs|src/VideoConverter.Api/Features/Conversions/Cleanup/DirectoryBatchCleanupService.cs"
  "src/VideoConverter.Api/Services/Conversion/IDirectoryBatchCleanupService.cs|src/VideoConverter.Api/Features/Conversions/Cleanup/IDirectoryBatchCleanupService.cs"
  "src/VideoConverter.Api/Services/Conversion/ConversionCleanupWorker.cs|src/VideoConverter.Api/Features/Conversions/Workers/ConversionCleanupWorker.cs"
  "src/VideoConverter.Api/Services/Conversion/DirectoryBatchCleanupService.cs|src/VideoConverter.Api/Features/DirectoryBatches/Cleanup/DirectoryBatchCleanupService.cs"
  "src/VideoConverter.Api/Services/Conversion/CleanupOptions.cs|src/VideoConverter.Api/Features/Conversions/Options/CleanupOptions.cs"
  "src/VideoConverter.Web/Services/VideoConverterApiClient.cs|src/VideoConverter.Web/Services/ApiClient/VideoConverterApiClient.cs"
  "src/VideoConverter.Web/Services/IVideoConverterApiClient.cs|src/VideoConverter.Web/Services/ApiClient/IVideoConverterApiClient.cs"
  "src/VideoConverter.Web/Services/FileBrowserService.cs|src/VideoConverter.Web/Services/FileBrowser/FileBrowserService.cs"
  "src/VideoConverter.Web/Services/IFileBrowserService.cs|src/VideoConverter.Web/Services/FileBrowser/IFileBrowserService.cs"
  "src/VideoConverter.Web/Services/ThemeService.cs|src/VideoConverter.Web/Services/Theme/ThemeService.cs"
  "src/VideoConverter.Web/Services/IThemeService.cs|src/VideoConverter.Web/Services/Theme/IThemeService.cs"
  "src/VideoConverter.Web/Services/ThemeChangedEventArgs.cs|src/VideoConverter.Web/Services/Theme/ThemeChangedEventArgs.cs"
  "src/VideoConverter.Web/Models/UnifiedQueueDialogLogic.cs|src/VideoConverter.Web/Features/Queue/Logic/UnifiedQueueDialogLogic.cs"
  "src/VideoConverter.Web/Models/ResolveMetadataDialogLogic.cs|src/VideoConverter.Web/Features/ManualResolution/Logic/ResolveMetadataDialogLogic.cs"
  "src/VideoConverter.Web/Models/EditConversionDialogLogic.cs|src/VideoConverter.Web/Features/Conversions/Logic/EditConversionDialogLogic.cs"
  "src/VideoConverter.Web/Pages/Queue.razor|src/VideoConverter.Web/Features/Queue/Pages/Queue.razor"
  "src/VideoConverter.Web/Pages/Conversions.razor|src/VideoConverter.Web/Features/Conversions/Pages/Conversions.razor"
  "src/VideoConverter.Web/Pages/ManualResolution.razor|src/VideoConverter.Web/Features/ManualResolution/Pages/ManualResolution.razor"
  "tests/VideoConverter.Tests/Services/Conversion/ConversionExecutionServiceTests.cs|tests/VideoConverter.Tests/Features/Conversions/Services/ConversionExecutionServiceTests.cs"
  "tests/VideoConverter.Tests/Services/Conversion/DirectoryBatchWorkerTests.cs|tests/VideoConverter.Tests/Features/Conversions/Workers/DirectoryBatchWorkerTests.cs"
  "tests/VideoConverter.Tests/Services/Conversion/ConversionCleanupServiceTests.cs|tests/VideoConverter.Tests/Features/Conversions/Cleanup/ConversionCleanupServiceTests.cs"
  "tests/VideoConverter.Tests/Api/ContractSnapshotParityApiTests.cs|tests/VideoConverter.Tests/Features/Contracts/Api/ContractSnapshotParityApiTests.cs"
  "tests/VideoConverter.Web.Tests/Services/VideoConverterApiClientBatchTests.cs|tests/VideoConverter.Web.Tests/Services/ApiClient/VideoConverterApiClientBatchTests.cs"
  "tests/VideoConverter.Web.Tests/Services/VideoConverterApiClientErrorTests.cs|tests/VideoConverter.Web.Tests/Services/ApiClient/VideoConverterApiClientErrorTests.cs"
  "tests/VideoConverter.Web.Tests/Components/QueueListTests.cs|tests/VideoConverter.Web.Tests/Features/Queue/Components/QueueListTests.cs"
)

validate_move_map() {
  local missing=0
  local target_exists=0
  local duplicate_targets=0
  declare -A target_seen=()

  echo "MOVE MAP VALIDATION (Step 1 planning only)"
  echo "Total pairs: ${#move_pairs[@]}"

  for pair in "${move_pairs[@]}"; do
    local source="${pair%%|*}"
    local target="${pair#*|}"

    if [[ ! -e "$source" ]]; then
      echo "MISSING_SOURCE: $source"
      missing=1
    fi

    if [[ -n "${target_seen[$target]:-}" ]]; then
      echo "DUPLICATE_TARGET: $target"
      duplicate_targets=1
    fi
    target_seen["$target"]=1

    if [[ -e "$target" ]]; then
      echo "TARGET_ALREADY_EXISTS: $target"
      target_exists=1
    fi
  done

  echo "Summary: missing_source=$missing duplicate_target=$duplicate_targets target_exists=$target_exists"

  if [[ $missing -ne 0 || $duplicate_targets -ne 0 || $target_exists -ne 0 ]]; then
    return 1
  fi

  echo "PASS: all sources exist and targets are unique/non-existing"
}

check_forbidden_drift() {
  local failed=0
  for file in "$@"; do
    if [[ "$file" =~ ^src/VideoConverter.Api/Features/.*/Api/.*\.(cs)$ ]]; then
      echo "BLOCK_API_CONTRACT_FILE: $file"
      failed=1
    fi
    if [[ "$file" =~ ^src/VideoConverter.Api/Data/(Migrations/|VideoConverterDbContext\.cs|Entities/) ]]; then
      echo "BLOCK_DB_SCHEMA_OR_BEHAVIOR_FILE: $file"
      failed=1
    fi
    if [[ "$file" =~ ^src/VideoConverter.Api/(Program\.cs|Features/.*/DependencyInjection/.*\.cs)$ ]]; then
      echo "BLOCK_DI_LIFETIME_OR_REGISTRATION_FILE: $file"
      failed=1
    fi
    if [[ "$file" =~ ^src/VideoConverter.Api/appsettings.*\.json$ || "$file" =~ ^src/VideoConverter.Web/wwwroot/appsettings.*\.json$ || "$file" =~ ^src/VideoConverter.Web/Program\.cs$ ]]; then
      echo "BLOCK_CONFIG_KEY_DEFAULT_RUNTIME_FILE: $file"
      failed=1
    fi
  done

  if [[ $failed -ne 0 ]]; then
    echo "FORBIDDEN_DRIFT_DETECTED"
    return 1
  fi

  echo "PASS: no forbidden drift paths detected"
}

if [[ "$mode" == "validate" ]]; then
  validate_move_map
elif [[ "$mode" == "forbidden-demo" ]]; then
  echo "FORBIDDEN DRIFT DEMO (expected failure)"
  check_forbidden_drift \
    "src/VideoConverter.Api/Features/Conversions/Api/ConversionsApi.cs" \
    "src/VideoConverter.Api/Data/VideoConverterDbContext.cs" \
    "src/VideoConverter.Api/Program.cs" \
    "src/VideoConverter.Api/appsettings.json"
elif [[ "$mode" == "forbidden-check" ]]; then
  shift
  check_forbidden_drift "$@"
else
  echo "Unknown mode: $mode"
  echo "Usage: step1_guardrail_check.sh [validate|forbidden-demo|forbidden-check <paths...>]"
  exit 2
fi
