# Delete Release Feature Implementation Plan

## Overview
Add the ability to delete a release from the collection via the album detail page. The delete option will be presented as an icon button, and users will be required to confirm deletion before the action is executed. Upon successful deletion, users will be redirected to the collection page.

---

## Backend Tasks

### 1. API Endpoint (Already Implemented ✓)
- [x] DELETE endpoint exists at `/api/musicreleases/{id}` in `MusicReleasesController.cs`
- [x] Returns 204 (No Content) on success
- [x] Returns 404 if release not found
- [x] Returns 500 on server errors
- [x] Service layer implemented in `MusicReleaseCommandService.cs`
- [x] Backend tests exist in `MusicReleasesControllerTests.cs`

### 2. Backend Verification & Enhancement (If Needed)
- [ ] Review and verify cascade delete behavior for related entities (purchase info, media, tracks)
- [ ] Ensure soft-delete is NOT being used (hard delete is appropriate for collection management)
- [ ] Verify transaction handling in delete operation
- [ ] Add integration tests for delete operation if not already present
- [ ] Verify logging is adequate for delete operations

---

## Frontend Tasks

### 3. API Client Function
**File**: `frontend/app/lib/api.ts`

- [ ] Add `deleteRelease(id: number)` function that:
  - Makes DELETE request to `/api/musicreleases/{id}`
  - Handles 204 No Content response
  - Handles 404 Not Found error
  - Handles timeout and network errors
  - Returns Promise<void> or appropriate response type
  - Uses existing `fetchJson` helper with `parse: false` for no response body

### 4. Delete Button Component
**File**: `frontend/app/components/DeleteReleaseButton.tsx` (NEW)

- [ ] Create reusable component for delete button with confirmation
- [ ] Props interface:
  - `releaseId: number`
  - `releaseTitle: string`
  - `onDeleteSuccess?: () => void`
  - `onDeleteError?: (error: Error) => void`
- [ ] Add delete icon (trash/bin icon from Heroicons or similar)
- [ ] Implement two-step deletion:
  1. Initial click shows confirmation modal/dialog
  2. Confirm action triggers API call
- [ ] Display loading state during deletion
- [ ] Handle success: call `onDeleteSuccess` callback
- [ ] Handle errors: display error message and call `onDeleteError`
- [ ] Button styling: 
  - Red/danger color scheme
  - Hover states
  - Disabled state during loading

### 5. Confirmation Modal/Dialog
**Option A**: Create new `ConfirmDialog.tsx` component (Recommended)
**Option B**: Use inline confirmation in `DeleteReleaseButton.tsx`

- [ ] Create confirmation dialog component (if Option A)
- [ ] Display release title in confirmation message
- [ ] Clear warning about permanent deletion
- [ ] Two buttons: "Cancel" and "Delete" (or "Confirm Delete")
- [ ] Focus management (auto-focus on Cancel, Escape to close)
- [ ] Prevent accidental deletion (require explicit confirm click)
- [ ] Accessible modal (ARIA labels, keyboard navigation)
- [ ] Optional: Add checkbox "I understand this cannot be undone"

### 6. Integration with Release Detail Page
**File**: `frontend/app/releases/[id]/page.tsx`

- [ ] Import `DeleteReleaseButton` component
- [ ] Add delete button to page header or appropriate location
- [ ] Position button near "Back" button or in action bar
- [ ] Implement `onDeleteSuccess` handler:
  - Show success message/toast (optional)
  - Navigate to `/collection` using `router.push('/collection')`
- [ ] Implement `onDeleteError` handler:
  - Display error message to user
  - Allow user to retry or go back
- [ ] Ensure button is visible and accessible on mobile devices
- [ ] Styling: Make button visually distinct as destructive action

### 7. Icon Selection
**File**: Multiple component files

- [ ] Choose appropriate delete icon:
  - Trash/bin icon (preferred)
  - X icon with circle
  - Delete icon
- [ ] Use consistent icon library (Heroicons, Lucide, or similar)
- [ ] Ensure icon is accessible (has aria-label or sr-only text)
- [ ] Size appropriately (e.g., w-5 h-5 or similar)
- [ ] Color: Red or danger color (#DC2626 or theme equivalent)

---

## Testing Tasks

### 8. Frontend Unit Tests
**File**: `frontend/app/components/__tests__/DeleteReleaseButton.test.tsx` (NEW)

- [ ] Test button renders with correct icon
- [ ] Test confirmation dialog appears on button click
- [ ] Test cancel button closes dialog without API call
- [ ] Test confirm button triggers API call
- [ ] Test loading state displays during deletion
- [ ] Test success callback is invoked on successful deletion
- [ ] Test error handling displays error message
- [ ] Test keyboard navigation (Tab, Enter, Escape)
- [ ] Test accessibility attributes (ARIA labels)

### 9. Integration Tests
**File**: `frontend/app/releases/[id]/__tests__/page.test.tsx`

- [ ] Test delete button is present on detail page
- [ ] Test navigation to collection page after successful deletion
- [ ] Mock API responses (success, 404, 500)
- [ ] Test error states render correctly
- [ ] Verify router.push is called with correct path

### 10. End-to-End Tests (Optional)
**File**: `frontend/e2e/delete-release.spec.ts` (NEW)

- [ ] Test complete delete workflow from detail page
- [ ] Test confirmation dialog interaction
- [ ] Test successful deletion and redirect
- [ ] Test canceling deletion
- [ ] Test deleting non-existent release (404 handling)
- [ ] Test network error handling

---

## UI/UX Considerations

### 11. User Experience Enhancements
- [ ] Add success notification/toast after deletion
- [ ] Consider undo functionality (advanced, optional)
- [ ] Ensure mobile responsiveness of delete button
- [ ] Add hover tooltip "Delete release from collection"
- [ ] Consider keyboard shortcut (e.g., Delete key) with confirmation
- [ ] Ensure sufficient spacing/separation from other action buttons
- [ ] Test with screen readers for accessibility

### 12. Visual Design
- [ ] Match existing app design patterns and color scheme
- [ ] Use danger/destructive action styling (red theme)
- [ ] Ensure button is prominent but not accidental click target
- [ ] Modal overlay dims background appropriately
- [ ] Confirmation dialog is centered and modal
- [ ] Loading spinner or skeleton during deletion

---

## Documentation Tasks

### 13. Code Documentation
- [ ] Add JSDoc comments to `deleteRelease` API function
- [ ] Add component documentation for `DeleteReleaseButton`
- [ ] Document props and behavior in component files
- [ ] Add inline comments for complex logic

### 14. User Documentation (Optional)
- [ ] Update README.md if applicable
- [ ] Add note about delete feature to user guide (if exists)
- [ ] Document that deletion is permanent (no soft delete/trash)

---

## Deployment Checklist

### 15. Pre-Deployment
- [ ] Run all unit tests: `npm test` (frontend)
- [ ] Run all backend tests: `dotnet test` (backend)
- [ ] Test manually on dev environment
- [ ] Test all error scenarios (404, 500, timeout)
- [ ] Verify mobile responsiveness
- [ ] Run accessibility audit (axe DevTools or similar)
- [ ] Check console for errors or warnings

### 16. Post-Deployment Verification
- [ ] Test delete functionality in production
- [ ] Verify navigation works correctly
- [ ] Confirm database records are deleted properly
- [ ] Check application logs for any errors
- [ ] Verify no broken references or orphaned records

---

## Rollback Plan

### 17. Rollback Considerations
- [ ] Document current version before deployment
- [ ] Ensure database backups are current
- [ ] Have plan to restore previous frontend build if needed
- [ ] Test rollback procedure in staging environment

---

## Future Enhancements (Optional)

### 18. Advanced Features (Phase 2)
- [ ] Bulk delete functionality (delete multiple releases)
- [ ] Soft delete with trash/restore functionality
- [ ] Delete from collection list view (not just detail page)
- [ ] Confirmation via typing release name
- [ ] Activity log/audit trail for deletions
- [ ] Export release data before deletion

---

## Estimated Timeline

- **Backend Review**: 1-2 hours
- **API Client Function**: 30 minutes
- **Delete Button Component**: 2-3 hours
- **Confirmation Dialog**: 2-3 hours
- **Integration**: 1-2 hours
- **Testing**: 3-4 hours
- **Documentation**: 1 hour
- **Total Estimated Time**: 11-16 hours

---

## Notes

- Backend delete endpoint is already implemented and tested
- Focus is primarily on frontend implementation
- Ensure accessibility and mobile responsiveness
- Consider user experience carefully for destructive actions
- Permanent deletion - no undo mechanism planned in this phase
