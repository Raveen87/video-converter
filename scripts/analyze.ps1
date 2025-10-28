# Static Analysis Script for Video Converter Project
# This script runs static analysis as a separate command

param(
    [switch]$Verbose,
    [string]$Configuration = "Debug",
    [switch]$FailOnIssues = $true
)

Write-Host "🔍 Running Static Analysis for Video Converter Project..." -ForegroundColor Cyan

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Clean previous analysis results
    Write-Host "🧹 Cleaning previous analysis results..." -ForegroundColor Yellow
    if (Test-Path "analysis-results") {
        Remove-Item -Recurse -Force "analysis-results"
    }
    New-Item -ItemType Directory -Path "analysis-results" -Force | Out-Null

    # Restore packages
    Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
    dotnet restore VideoConverter.sln

    # Verify formatting (fail if changes are needed)
    Write-Host "🧯 Verifying code formatting with dotnet format..." -ForegroundColor Yellow
    $formatArgs = @(
        "format"
        "VideoConverter.sln"
        "--severity", "error"
        "--verify-no-changes"
    )
    dotnet @formatArgs

    # Build with analysis
    Write-Host "🔨 Building with static analysis..." -ForegroundColor Yellow
    $buildArgs = @(
        "build"
        "VideoConverter.sln"
        "--configuration", $Configuration
        "--verbosity", "normal"
        "--no-restore"
    )
    
    if ($Verbose) {
        $buildArgs += "--verbosity", "detailed"
    }

    dotnet @buildArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed with static analysis errors" -ForegroundColor Red
        if ($FailOnIssues) {
            exit 1
        }
    } else {
        Write-Host "✅ Static analysis completed successfully" -ForegroundColor Green
    }

    # Run tests with coverage for additional analysis
    Write-Host "🧪 Running tests with coverage analysis..." -ForegroundColor Yellow
    $testArgs = @(
        "test"
        "VideoConverter.sln"
        "--configuration", $Configuration
        "--collect", "XPlat Code Coverage"
        "--results-directory", "analysis-results/test-results"
        "--logger", "trx;LogFileName=test-results.trx"
        "--no-build"
    )

    dotnet @testArgs

    # Generate analysis report
    Write-Host "📊 Generating analysis report..." -ForegroundColor Yellow
    
    # Create a simple analysis summary
    $reportContent = @"
# Static Analysis Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Build Status
- Configuration: $Configuration
- Build Result: $(if ($LASTEXITCODE -eq 0) { "✅ Success" } else { "❌ Failed" })

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
"@

    $reportContent | Out-File -FilePath "analysis-results/analysis-report.md" -Encoding UTF8

    Write-Host "📄 Analysis report generated: analysis-results/analysis-report.md" -ForegroundColor Green

} catch {
    Write-Host "❌ Static analysis failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($FailOnIssues) {
        exit 1
    }
}

Write-Host "🎉 Static analysis completed!" -ForegroundColor Green
