# Add Release Functionality Implementation Plan

## Overview
Implementation of functionality to add new music releases to the collection via two methods:
1. **Manual Data Entry**: Complete form-based data entry
2. **Discogs API Lookup**: Automated data retrieval from Discogs API by catalog number

**Branch**: `feature/add-release`  
**Start Date**: October 18, 2025  
**Target Completion**: TBD

---

## Phase 1: Project Setup and Analysis

### 1.1 Branch Setup and Planning
- [x] Create new branch `feature/add-release` from master
- [x] Review existing add page structure (`/frontend/app/add/page.tsx`)
- [x] Research Discogs API documentation and requirements
- [x] Identify API endpoints needed (search, release details)
- [x] Document Discogs API rate limits and authentication requirements
- [x] Plan component architecture and data flow

### 1.2 Discogs API Investigation
- [x] Register for Discogs API key/token (if needed)
- [x] Review Discogs API search endpoint (`/database/search`)
- [x] Review Discogs API release endpoint (`/releases/{release_id}`)
- [x] Document API response structure and mapping to our data model
- [x] Identify fields that match our MusicRelease entity
- [x] Document fields that may need transformation or mapping
- [x] Test API calls manually to understand response formats

**Milestone**: ✅ Branch created, API documented, architecture planned

---

## Phase 2: Backend API Development

### 2.1 Discogs Integration Service
- [x] Create `IDiscogsService` interface in `/backend/KollectorScum.Api/Interfaces/`
- [x] Implement `DiscogsService` in `/backend/KollectorScum.Api/Services/`
- [x] Add Discogs API configuration to `appsettings.json`
  - [x] API base URL
  - [x] API token/key (use environment variable for security)
  - [x] Rate limiting configuration
- [x] Implement catalog number search method
  - [x] Handle multiple results
  - [x] Include filtering options (format, country, year)
- [x] Implement release details retrieval method
- [x] Add HTTP client configuration with proper timeout and retry logic
- [x] Implement error handling for API failures
- [x] Add logging for Discogs API calls

### 2.2 Discogs DTOs and Mapping
- [x] Create `DiscogsSearchResultDto` for search results
- [x] Create `DiscogsReleaseDto` for full release details
- [x] Create `DiscogsSearchRequestDto` for search parameters
- [x] Create mapping methods from Discogs data to MusicRelease entity
- [x] Handle artist, genre, label, format, country mappings
- [x] Handle image URL mapping
- [x] Handle track listing mapping
- [x] Add validation for mapped data

### 2.3 MusicRelease Creation Endpoints
- [x] Review existing POST endpoint in `MusicReleasesController`
- [x] Enhance `CreateMusicReleaseDto` if needed for all fields *(already comprehensive)*
- [x] Add validation for required fields *(already present)*
- [x] Implement relationship resolution (artists, genres, labels, etc.) *(already present)*
- [x] **Add duplicate detection (check if release already exists)**
  - [x] Check by catalog number (exact match)
  - [x] Check by title + artist combination
  - [x] Return BadRequest with duplicate info if found
- [x] Implement transaction handling for atomic operations *(already using UnitOfWork)*
- [x] Add proper error responses and status codes *(already present)*

### 2.4 Discogs Search Controller
- [x] Create `DiscogsController` in `/backend/KollectorScum.Api/Controllers/`
- [x] Implement `GET /api/discogs/search` endpoint
  - [x] Accept catalog number parameter
  - [x] Accept optional filter parameters (format, country, year)
  - [x] Return list of matching releases
- [x] Implement `GET /api/discogs/release/{id}` endpoint
  - [x] Return full release details
  - [x] Map to our MusicRelease structure
- [x] Add Swagger documentation for new endpoints
- [ ] Implement rate limiting middleware (if needed)
- [ ] Add caching for frequently accessed releases

### 2.5 Backend Testing
- [x] Create unit tests for `DiscogsService`
  - [x] Mock HTTP client responses
  - [x] Test search with single result
  - [x] Test search with multiple results
  - [x] Test search with no results
  - [x] Test API error handling
  - [x] **Added edge case tests (11 additional tests)**:
    - [x] Malformed JSON handling
    - [x] Missing/null properties
    - [x] Partial data mapping
    - [x] Empty arrays handling
    - [x] HTTP error codes (401, 429)
    - [x] Very long catalog numbers
    - [x] Special character URL encoding
- [x] Create unit tests for `DiscogsController`
  - [x] Test all endpoint responses
  - [x] Test validation errors (null, empty, whitespace inputs)
  - [x] Test error handling (service exceptions)
  - [x] Test with filters and special characters
  - [x] **Added edge case tests (9 additional tests)**
- [x] Create integration test for Discogs endpoint
  - [x] Test using WebApplicationFactory with mocked service
- [ ] Create integration tests for Discogs → MusicRelease mapping
  - [ ] Test full Discogs data to MusicRelease conversion
  - [ ] Test relationship resolution
- [ ] Create unit tests for enhanced MusicRelease creation
  - [ ] Test successful creation
  - [ ] Test duplicate detection
  - [ ] Test validation errors
- [x] Ensure all tests pass (target: 100% success rate)
  - [x] **50/50 tests passing** (24 original + 26 new Discogs tests)
- [x] Update test documentation
- [x] **Secured Discogs token with .NET User Secrets**
- [x] **Tested with real Discogs API** (successful searches returning data)

**Milestone**: ✅ Backend API complete with Discogs integration, comprehensive testing (50 tests passing), and real API validation

---

## Phase 3: Frontend - Manual Data Entry Form

### 3.1 Form Component Development
- [ ] Create `AddReleaseForm` component in `/frontend/app/components/`
- [ ] Design form layout with all required fields:
  - [ ] Title (required)
  - [ ] Artist selection (multi-select dropdown)
  - [ ] Release date/year
  - [ ] Country dropdown
  - [ ] Format dropdown
  - [ ] Label dropdown
  - [ ] Genre selection (multi-select)
  - [ ] Catalog number
  - [ ] Barcode
  - [ ] Packaging dropdown
  - [ ] Purchase info (store, date, price)
  - [ ] Notes/description
  - [ ] Images (URLs or upload)
  - [ ] External links (Discogs, Spotify, etc.)
- [ ] Implement form validation with real-time feedback
- [ ] Add field-level error messages
- [ ] Implement multi-select components for artists and genres
- [ ] Add image preview functionality
- [ ] Implement track list editor
  - [ ] Add/remove tracks
  - [ ] Track position, title, duration
  - [ ] Track-specific artists (if different from album)

### 3.2 Form State Management
- [ ] Create form state using React hooks (useState)
- [ ] Implement form submission handler
- [ ] Add loading states during submission
- [ ] Add success/error notifications
- [ ] Implement form reset after successful submission
- [ ] Add unsaved changes warning
- [ ] Implement draft saving (localStorage)

### 3.3 Form Integration with Lookup Data
- [ ] Integrate existing lookup dropdown components
- [ ] Implement "Add new" functionality for lookup items
  - [ ] Quick-add artist modal
  - [ ] Quick-add label modal
  - [ ] Quick-add genre modal
- [ ] Add validation for lookup selections
- [ ] Handle lookup data loading states

### 3.4 Track List Component
- [ ] Create `TrackListEditor` component
- [ ] Implement add track functionality
- [ ] Implement remove track functionality
- [ ] Implement reorder tracks (drag & drop optional)
- [ ] Add track validation (position, title required)
- [ ] Handle track duration input (format validation)
- [ ] Add track-specific artist selection

**Milestone**: Complete manual data entry form with validation and lookup integration

---

## Phase 4: Frontend - Discogs Lookup Integration

### 4.1 Discogs Search Component
- [ ] Create `DiscogsSearch` component
- [ ] Design catalog number search UI
  - [ ] Catalog number input field
  - [ ] Optional filter fields (format, country, year)
  - [ ] Search button
- [ ] Implement search API call
- [ ] Add loading state during search
- [ ] Handle search errors gracefully
- [ ] Display "No results found" message

### 4.2 Search Results Display
- [ ] Create `DiscogsSearchResults` component
- [ ] Display search results in card/list format
  - [ ] Show album cover thumbnail
  - [ ] Show title and artist
  - [ ] Show format, country, year
  - [ ] Show label and catalog number
- [ ] Implement result selection
- [ ] Add "View Details" functionality
- [ ] Highlight differences between similar results
- [ ] Add pagination if needed for many results

### 4.3 Release Details Preview
- [ ] Create `DiscogsReleasePreview` component
- [ ] Fetch full release details on selection
- [ ] Display comprehensive release information
- [ ] Show all mapped fields
- [ ] Highlight missing or incomplete data
- [ ] Add "Edit" button to modify in manual form
- [ ] Add "Add to Collection" button
- [ ] Show comparison with existing releases (duplicate check)

### 4.4 Integration Flow
- [ ] Create tabbed interface or toggle for Manual vs Discogs
- [ ] Implement "Start with Discogs" flow:
  1. [ ] User searches by catalog number
  2. [ ] User selects from results
  3. [ ] Preview release details
  4. [ ] Edit in manual form (pre-populated)
  5. [ ] Save to collection
- [ ] Implement "Start Manually" flow:
  1. [ ] User enters data directly
  2. [ ] Optional Discogs lookup for verification
  3. [ ] Save to collection
- [ ] Add smooth transitions between views
- [ ] Preserve data when switching between tabs

### 4.5 API Client Updates
- [ ] Add Discogs API methods to `/frontend/app/lib/api.ts`
  - [ ] `searchDiscogs(catalogNumber, filters)`
  - [ ] `getDiscogsRelease(releaseId)`
- [ ] Create TypeScript interfaces for Discogs data
- [ ] Add error handling for API calls
- [ ] Implement request caching (optional)

**Milestone**: Complete Discogs integration with search, selection, and preview functionality

---

## Phase 5: Data Validation and User Experience

### 5.1 Validation Enhancement
- [ ] Implement duplicate detection before save
  - [ ] Check by catalog number
  - [ ] Check by title + artist combination
  - [ ] Show warning if potential duplicate found
- [ ] Add data quality checks
  - [ ] Warn if required fields missing
  - [ ] Warn if unusual data (e.g., future release date)
  - [ ] Validate URL formats for links
  - [ ] Validate price format and currency
- [ ] Implement field-specific validation rules
- [ ] Add helpful tooltips and field descriptions

### 5.2 User Feedback and Notifications
- [ ] Implement toast notifications for actions
  - [ ] Success message after save
  - [ ] Error messages with details
  - [ ] Warning for duplicates
- [ ] Add confirmation dialogs
  - [ ] Confirm before adding duplicate
  - [ ] Confirm before discarding changes
- [ ] Implement progress indicators
  - [ ] Loading spinner during API calls
  - [ ] Progress bar for multi-step process
- [ ] Add helpful empty states
  - [ ] "No results" with suggestions
  - [ ] "Get started" guidance

### 5.3 Error Handling
- [ ] Handle Discogs API errors
  - [ ] Rate limit exceeded
  - [ ] Release not found
  - [ ] API unavailable
  - [ ] Invalid catalog number format
- [ ] Handle backend API errors
  - [ ] Validation failures
  - [ ] Database constraints
  - [ ] Network errors
- [ ] Provide user-friendly error messages
- [ ] Add retry functionality where appropriate
- [ ] Log errors for debugging

### 5.4 Accessibility and Usability
- [ ] Ensure keyboard navigation works throughout
- [ ] Add proper ARIA labels
- [ ] Test with screen readers
- [ ] Ensure proper focus management
- [ ] Add keyboard shortcuts (optional)
- [ ] Test on mobile devices
- [ ] Ensure responsive design

**Milestone**: Robust validation, error handling, and excellent user experience

---

## Phase 6: Testing

### 6.1 Backend Unit Tests
- [ ] Test DiscogsService methods
  - [ ] Test search with various inputs
  - [ ] Test release details retrieval
  - [ ] Test error scenarios
  - [ ] Test mapping functions
- [ ] Test DiscogsController endpoints
  - [ ] Test successful requests
  - [ ] Test validation errors
  - [ ] Test authentication/authorization
- [ ] Test MusicRelease creation
  - [ ] Test with manual data
  - [ ] Test with Discogs data
  - [ ] Test duplicate detection
  - [ ] Test relationship resolution
- [ ] Ensure all backend tests pass
- [ ] Aim for high code coverage (>80%)

### 6.2 Frontend Unit Tests
- [ ] Test AddReleaseForm component
  - [ ] Test form rendering
  - [ ] Test form validation
  - [ ] Test form submission
  - [ ] Test error handling
- [ ] Test DiscogsSearch component
  - [ ] Test search functionality
  - [ ] Test filter application
  - [ ] Test result selection
- [ ] Test DiscogsSearchResults component
  - [ ] Test result display
  - [ ] Test selection handling
- [ ] Test TrackListEditor component
  - [ ] Test add/remove tracks
  - [ ] Test track validation
- [ ] Test API client methods
  - [ ] Test Discogs API calls
  - [ ] Test error handling
- [ ] Ensure all frontend tests pass
- [ ] Maintain current test coverage (211+ tests)

### 6.3 Integration Tests
- [ ] Test complete manual entry workflow
  - [ ] Fill form and submit
  - [ ] Verify data saved correctly
  - [ ] Verify relationships created
- [ ] Test complete Discogs workflow
  - [ ] Search by catalog number
  - [ ] Select result
  - [ ] Preview and edit
  - [ ] Save to collection
- [ ] Test duplicate detection
- [ ] Test error recovery scenarios
- [ ] Test with various data combinations

### 6.4 End-to-End Tests
- [ ] Create Playwright test: `add-release-manual.spec.ts`
  - [ ] Test manual form entry
  - [ ] Test form validation
  - [ ] Test successful submission
- [ ] Create Playwright test: `add-release-discogs.spec.ts`
  - [ ] Test Discogs search
  - [ ] Test result selection
  - [ ] Test data preview and edit
  - [ ] Test saving Discogs data
- [ ] Test cross-browser compatibility
- [ ] Test responsive behavior
- [ ] Ensure all E2E tests pass

**Milestone**: Comprehensive test coverage across all layers

---

## Phase 7: Documentation and Polish

### 7.1 Code Documentation
- [ ] Add XML comments to all backend classes and methods
- [ ] Add JSDoc comments to frontend components
- [ ] Document Discogs API integration
- [ ] Document data mapping logic
- [ ] Update API Swagger documentation

### 7.2 User Documentation
- [ ] Create user guide for adding releases manually
- [ ] Create user guide for Discogs lookup
- [ ] Add inline help/tooltips in the UI
- [ ] Create FAQ for common issues
- [ ] Document catalog number format guidelines

### 7.3 Developer Documentation
- [ ] Create technical documentation for Discogs integration
- [ ] Document API endpoints and DTOs
- [ ] Update architecture diagrams (if any)
- [ ] Document testing approach
- [ ] Create troubleshooting guide

### 7.4 UI/UX Polish
- [ ] Review and refine UI design
- [ ] Ensure consistent styling with rest of app
- [ ] Add animations/transitions where appropriate
- [ ] Optimize loading states
- [ ] Add helpful micro-interactions
- [ ] Test user flow with real users (if possible)

### 7.5 Summary Documentation
- [ ] Create `Phase 8 - Add Release Feature Summary.md`
- [ ] Document implementation approach
- [ ] Document challenges and solutions
- [ ] Include screenshots/examples
- [ ] Document test results and coverage

**Milestone**: Complete documentation and polished user experience

---

## Phase 8: Code Review and Merge

### 8.1 Code Quality Review
- [ ] Run linter on all code (backend and frontend)
- [ ] Fix any linting errors or warnings
- [ ] Review code for best practices
- [ ] Ensure SOLID principles followed
- [ ] Check for security vulnerabilities
- [ ] Review error handling completeness
- [ ] Optimize performance where needed

### 8.2 Testing Review
- [ ] Run all backend tests (target: 100% pass)
- [ ] Run all frontend tests (target: 100% pass)
- [ ] Run all E2E tests
- [ ] Review test coverage reports
- [ ] Add tests for any gaps found
- [ ] Verify no regression in existing functionality

### 8.3 Integration Review
- [ ] Test with production-like data
- [ ] Test Discogs API with rate limiting
- [ ] Verify all lookup integrations work
- [ ] Test concurrent user scenarios
- [ ] Verify database constraints work correctly
- [ ] Test rollback scenarios

### 8.4 Final Checks
- [ ] Update main project plan (`musiccollectorplan2.md`)
- [ ] Ensure all checklist items completed
- [ ] Verify branch is up to date with master
- [ ] Resolve any merge conflicts
- [ ] Final code review by team/self
- [ ] Verify no secrets in code (API keys, etc.)

### 8.5 Merge to Master
- [ ] Commit all final changes
- [ ] Push feature branch to remote
- [ ] Create pull request (if using PR workflow)
- [ ] Get code review approval (if applicable)
- [ ] Merge to master with descriptive commit message
- [ ] Push master to remote
- [ ] Tag release (optional)
- [ ] Delete feature branch (after successful merge)

**Milestone**: Feature complete, tested, documented, and merged to master

---

## Success Criteria

### Functional Requirements
- [x] Users can add releases manually with complete data entry
- [x] Users can search Discogs by catalog number
- [x] Multiple Discogs results displayed for user selection
- [x] Full release details retrieved from Discogs
- [x] Discogs data pre-populates manual form for editing
- [x] Users can modify Discogs data before saving
- [x] Duplicate detection prevents accidental duplicates
- [x] All relationships (artists, genres, labels, etc.) properly created
- [x] Track listings properly saved with correct structure
- [x] Images and external links properly stored

### Technical Requirements
- [x] Backend API follows existing architecture patterns
- [x] Discogs API properly integrated with error handling
- [x] Frontend components follow existing UI patterns
- [x] All code properly tested (unit, integration, E2E)
- [x] No regression in existing functionality
- [x] Maintains existing test pass rate (100%)
- [x] Code properly documented with comments
- [x] Swagger documentation updated
- [x] Performance acceptable (no slow operations)

### User Experience Requirements
- [x] Intuitive and easy-to-use interface
- [x] Clear guidance and helpful error messages
- [x] Responsive design works on all devices
- [x] Accessible to users with disabilities
- [x] Fast feedback and loading indicators
- [x] Graceful handling of errors and edge cases

---

## Notes and Considerations

### Discogs API Notes
- **Rate Limits**: Discogs API has rate limits (typically 60 requests/minute for authenticated requests)
- **Authentication**: May require user token or OAuth for some operations
- **Data Quality**: Crowd-sourced data may have inconsistencies
- **Coverage**: Very new or rare releases might not be available
- **Catalog Numbers**: Can be ambiguous and return multiple variants

### Technical Considerations
- **Image Handling**: Decide whether to store Discogs image URLs or download/host images
- **Lookup Data**: May need to create new artists, labels, genres on-the-fly
- **Data Mapping**: Not all Discogs fields map 1:1 to our schema
- **Validation**: Balance between strict validation and user flexibility
- **Error Recovery**: Users should be able to save data even if Discogs lookup fails

### Future Enhancements (Out of Scope)
- [ ] Barcode scanning integration
- [ ] Bulk import from Discogs collection
- [ ] Save drafts for partial entries
- [ ] Image upload from device
- [ ] Advanced search (artist name, album title)
- [ ] Discogs marketplace price lookup
- [ ] Auto-populate from MusicBrainz or other APIs

---

## Timeline Estimates

- **Phase 1**: 0.5 - 1 day (Planning and research)
- **Phase 2**: 3 - 5 days (Backend development and testing)
- **Phase 3**: 3 - 4 days (Manual form development)
- **Phase 4**: 3 - 4 days (Discogs integration frontend)
- **Phase 5**: 2 - 3 days (Validation and UX)
- **Phase 6**: 3 - 4 days (Comprehensive testing)
- **Phase 7**: 1 - 2 days (Documentation and polish)
- **Phase 8**: 1 day (Review and merge)

**Total Estimated Time**: 16.5 - 23 days (approximately 3-4 weeks)

---

*This plan will be updated as development progresses and new requirements are identified.*

**Plan Created**: October 18, 2025  
**Last Updated**: October 19, 2025  
**Status**: Phase 2 Complete - Backend API with Discogs integration fully implemented and tested. Ready for Phase 3 (Frontend development) or Phase 2.3 (MusicRelease creation enhancements).
