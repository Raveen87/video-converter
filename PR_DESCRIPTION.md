## Summary

This PR improves the visual distinction of conversion status indicators by implementing brighter, more distinguishable colors for running and waiting conversions. The default MudBlazor colors (green for running, blue for waiting) are replaced with brighter, more vibrant shades that provide better visual clarity.

## Changes

- **Added `GetProgressClass()` helper method** in `Conversions.razor` to return CSS class names based on conversion status
- **Updated `MudProgressLinear` component** to apply custom CSS classes (`conversion-running` and `conversion-waiting`)
- **Added custom CSS styles** in `app.css` with brighter colors:
  - Running conversions: `#00d084` (bright green)
  - Waiting conversions: `#2196f3` (bright blue)

## Technical Details

The implementation uses CSS class-based styling to override MudBlazor's default progress bar colors. The CSS targets the `.mud-progress-linear-bar` element within progress bars that have the custom status classes, ensuring the brighter colors are applied while maintaining compatibility with MudBlazor's component structure.

## PR Checklist

- [x] Lint passes (`dotnet format`), analyzers clean, warnings-as-errors satisfied
- [x] Tests added/updated and pass locally and in CI
- [x] Coverage for new/changed code ≥ 85%
- [ ] Documentation updated (`README.md` as needed)
- [x] Docker build succeeds locally or via CI

## Notes

- No functional changes to conversion logic
- UI-only enhancement for better visual feedback
- Build verified: `dotnet build VideoConverter.sln` passes with 0 warnings and 0 errors
