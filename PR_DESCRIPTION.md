# feat: Add queue reorder support with drag-and-drop UI

## Summary

Adds comprehensive support for reordering the conversion queue, including a new API endpoint and drag-and-drop functionality in the web UI. Users can now prioritize pending conversions by dragging items to their desired position.

## Changes

### Backend API

- **New endpoint**: `PUT /api/conversions/queue/reorder`
  - Accepts an ordered list of conversion IDs
  - Reorders only pending conversions (running/completed/failed are excluded)
  - Applies sequential ranks (1000, 2000, 3000, etc.) to maintain order
  - Returns updated list of pending conversions in new order
  - Validates: duplicate IDs, empty arrays, non-pending conversions, null requests

- **Enhanced `ConversionDto`**: Added `Status` field to distinguish between Pending, Running, Completed, and Failed states

- **Refactored reorder logic**: Extracted helper methods for better maintainability:
  - `ValidateOrderedIds()` - Ensures no duplicate IDs
  - `BuildOrderedQueue()` - Constructs ordered list with specified items first
  - `ApplySequentialRanks()` - Assigns sequential ranks for stable ordering

### Frontend UI

- **Drag-and-drop support**: 
  - Pending conversions can be dragged and reordered in the queue table
  - Visual feedback during drag operations (cursor changes, row highlighting)
  - Only pending items are draggable (running/completed/failed are disabled)
  - Automatic API call on drop to persist new order

- **Status display**: 
  - Added status column showing conversion state (Pending, Running, Completed, Failed)
  - Color-coded chips for visual status indication
  - Separated `ConversionItemStatus` (for list items) from `ConversionProgressStatus` (for progress tracking)

- **API client integration**: 
  - New `ReorderQueueAsync()` method in `IVideoConverterApiClient`
  - Proper error handling and user feedback via snackbar notifications

### Testing

**Backend API Tests** (`ConversionQueueReorderTests.cs`):
- ✅ Valid subset reorder with DB persistence verification
- ✅ Unknown ID handling (NotFound)
- ✅ Duplicate ID validation
- ✅ Empty orderedIds validation
- ✅ Null request validation
- ✅ Running conversion rejection
- ✅ Completed conversion rejection
- ✅ Full queue reorder
- ✅ Single item reorder
- ✅ Empty queue handling
- ✅ List retrieval after reorder (verifies GET endpoint returns correct order)
- ✅ Non-pending conversions preservation
- ✅ Sequential rank application

**Frontend API Client Tests** (`VideoConverterApiClientReorderTests.cs`):
- ✅ Valid request endpoint verification
- ✅ HTTP error handling
- ✅ Empty list handling

**Test Coverage**: All new/changed code paths are covered with ≥85% coverage requirement met.

### Documentation

- Updated `README.md` with:
  - New `PUT /api/conversions/queue/reorder` endpoint documentation
  - Queue ordering explanation
  - UI drag-and-drop feature description

## Technical Details

### Rank System
- Uses existing `Rank` field (string) for ordering
- Sequential ranks applied: `00001000`, `00002000`, `00003000`, etc.
- Non-pending conversions retain their ranks but are excluded from queue operations
- Ranks are backfilled on startup for existing records

### API Request/Response Format

**Request**:
```json
{
  "orderedIds": [3, 1, 2]
}
```

**Response**:
```json
[
  {
    "id": 3,
    "inputPath": "c.mp4",
    "outputDirectory": "out",
    "status": "Pending",
    ...
  },
  {
    "id": 1,
    "inputPath": "a.mp4",
    "outputDirectory": "out",
    "status": "Pending",
    ...
  },
  {
    "id": 2,
    "inputPath": "b.mp4",
    "outputDirectory": "out",
    "status": "Pending",
    ...
  }
]
```

### UI Behavior

- **Draggable items**: Only conversions with `Status == Pending` can be dragged
- **Drop zones**: Any position in the pending queue is a valid drop target
- **Visual feedback**: 
  - Drag cursor on hover
  - Row highlighting during drag
  - Disabled state for non-pending items
- **Error handling**: User-friendly error messages via snackbar on API failures

## Breaking Changes

None. This is a purely additive feature.

## Migration Notes

No database migrations required. The existing `Rank` field is used for the new functionality.

## PR Checklist

- [x] Lint passes (`dotnet format`), analyzers clean, warnings-as-errors satisfied
- [x] Tests added/updated and pass locally and in CI
- [x] Coverage for new/changed code ≥ 85%
- [x] Documentation updated (`README.md` as needed)
- [x] Docker build succeeds locally or via CI

## Screenshots / Demo

Users can now:
1. View the conversion queue with status indicators
2. Drag pending conversions to reorder them
3. See immediate visual feedback during drag operations
4. Have the new order persisted automatically via API call

## Related Issues

N/A - Feature request implementation

