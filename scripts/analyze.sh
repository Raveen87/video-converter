#!/bin/bash
# Static Analysis Script for Video Converter Project
# This script runs static analysis as a separate command

set -e

# Default values
VERBOSE=false
CONFIGURATION="Debug"
FAIL_ON_ISSUES=true

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose)
            VERBOSE=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --no-fail)
            FAIL_ON_ISSUES=false
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo "🔍 Running Static Analysis for Video Converter Project..."

# Clean previous analysis results
echo "🧹 Cleaning previous analysis results..."
rm -rf analysis-results
mkdir -p analysis-results

# Restore packages
echo "📦 Restoring packages..."
dotnet restore VideoConverter.sln

# Build with analysis
echo "🔨 Building with static analysis..."
BUILD_ARGS=(
    "build"
    "VideoConverter.sln"
    "--configuration" "$CONFIGURATION"
    "--no-restore"
)

if [ "$VERBOSE" = true ]; then
    BUILD_ARGS+=("--verbosity" "detailed")
else
    BUILD_ARGS+=("--verbosity" "normal")
fi

if ! dotnet "${BUILD_ARGS[@]}"; then
    echo "❌ Build failed with static analysis errors"
    if [ "$FAIL_ON_ISSUES" = true ]; then
        exit 1
    fi
else
    echo "✅ Static analysis completed successfully"
fi

# Run tests with coverage for additional analysis
echo "🧪 Running tests with coverage analysis..."
TEST_ARGS=(
    "test"
    "VideoConverter.sln"
    "--configuration" "$CONFIGURATION"
    "--collect" "XPlat Code Coverage"
    "--results-directory" "analysis-results/test-results"
    "--logger" "trx;LogFileName=test-results.trx"
    "--no-build"
)

dotnet "${TEST_ARGS[@]}"

# Generate analysis report
echo "📊 Generating analysis report..."

cat > analysis-results/analysis-report.md << EOF
# Static Analysis Report
Generated: $(date)

## Build Status
- Configuration: $CONFIGURATION
- Build Result: $(if [ $? -eq 0 ]; then echo "✅ Success"; else echo "❌ Failed"; fi)

## Analysis Summary
- SonarAnalyzer: Enabled
- .NET Analyzers: Enabled
- Code Style: Enforced
- Warnings as Errors: Enabled

## Files Analyzed
- Source files: src/
- Test files: tests/
- Configuration: sonar-project.properties

## Next Steps
1. Review any warnings or errors in the build output
2. Fix issues identified by static analysis
3. Re-run analysis to verify fixes

## Configuration
Static analysis rules can be configured in:
- sonar-project.properties (SonarQube rules)
- .editorconfig (Code style rules)
- Directory.Build.props (Build-time analysis settings)
EOF

echo "📄 Analysis report generated: analysis-results/analysis-report.md"
echo "🎉 Static analysis completed!"
