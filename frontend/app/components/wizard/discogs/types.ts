/**
 * Type definitions and step model for the Discogs add-release wizard.
 *
 * The wizard moves through three explicit steps:
 *   1. search  – user enters catalogue number and optional filters
 *   2. results – user selects a result from the list (mandatory before continuing)
 *   3. details – full release details are shown; user chooses Add or Edit
 *
 * Step guards prevent forward navigation unless the preceding step is complete.
 */

import type { DiscogsSearchRequest, DiscogsSearchResult, DiscogsRelease } from "../../../lib/discogs-types";
import type { CreateMusicReleaseDto } from "../../AddReleaseForm";

/** Ordered step identifiers for the Discogs wizard */
export type DiscogsWizardStep = "search" | "results" | "details";

/**
 * Image source URLs captured from the Discogs payload.
 * Stored separately so they can be used to trigger background downloads
 * after the release has been saved to the local database.
 */
export interface DiscogsSourceImages {
  cover: string | null;
  thumbnail: string | null;
}

/**
 * Complete state for the Discogs add-release wizard.
 * Each field is populated as the wizard advances through its steps.
 */
export interface DiscogsWizardState {
  /** The search parameters entered in step 1 */
  search: DiscogsSearchRequest;
  /** Results returned from the Discogs API in step 1 */
  results: DiscogsSearchResult[];
  /** The result the user has highlighted (or confirmed) in step 2 */
  selectedResult: DiscogsSearchResult | null;
  /** Full release data fetched when the user enters step 3 */
  selectedRelease: DiscogsRelease | null;
  /**
   * Mapped DTO ready to pre-populate the manual edit wizard.
   * Populated when the user chooses "Edit Release" in step 3.
   */
  mappedDraft?: Partial<CreateMusicReleaseDto>;
  /** Original Discogs image URLs needed for post-save download */
  sourceImages: DiscogsSourceImages;
}

/** Default / empty wizard state used on first mount */
export const EMPTY_DISCOGS_STATE: DiscogsWizardState = {
  search: { catalogNumber: "" },
  results: [],
  selectedResult: null,
  selectedRelease: null,
  sourceImages: { cover: null, thumbnail: null },
};

// ─── Step guard helpers ───────────────────────────────────────────────────────

/**
 * Returns true when the search step is complete and the wizard may advance
 * to the results step.
 * Condition: at least one result has been returned.
 */
export function canEnterResults(state: DiscogsWizardState): boolean {
  return state.results.length > 0;
}

/**
 * Returns true when the results step is complete and the wizard may advance
 * to the details step.
 * Condition: a result has been selected.
 */
export function canEnterDetails(state: DiscogsWizardState): boolean {
  return state.selectedResult !== null;
}

/**
 * Returns true when the details step is ready for the Edit Release action.
 * Condition: the full release payload has been loaded and mapped.
 */
export function canEditRelease(state: DiscogsWizardState): boolean {
  return state.selectedRelease !== null && state.mappedDraft !== undefined;
}
