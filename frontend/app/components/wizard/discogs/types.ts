/**
 * Type definitions for the Discogs add-release wizard flow.
 *
 * The wizard has three sequential steps:
 *  1. search   – user enters catalogue number and optional filters
 *  2. results  – user selects one result from the list
 *  3. details  – full release preview; user can add to collection or edit
 *
 * An optional fourth synthetic step "edit" represents the handoff into the
 * manual AddReleaseWizard.  That wizard is rendered in place of this one so
 * it does not need its own step model here.
 */

import type { DiscogsSearchRequest, DiscogsSearchResult, DiscogsRelease } from "../../../lib/discogs-types";
import type { CreateMusicReleaseDto } from "../../AddReleaseForm";

/** The named steps of the Discogs wizard flow. */
export type DiscogsWizardStep = "search" | "results" | "details";

/**
 * All state held by the Discogs add-release wizard.
 *
 * Step guards:
 *  - cannot enter `results` without a completed `searchRequest` that produced results
 *  - cannot enter `details`  without a `selectedResult`
 *  - cannot launch the edit wizard without `mappedDraft`
 */
export interface DiscogsWizardState {
  /** Current wizard step. */
  step: DiscogsWizardStep;
  /** The last submitted search request (preserved for Back navigation). */
  searchRequest: DiscogsSearchRequest | null;
  /** Results returned from the last successful search. */
  searchResults: DiscogsSearchResult[];
  /** The result the user has chosen to inspect. */
  selectedResult: DiscogsSearchResult | null;
  /** Full release payload fetched for the selected result. */
  selectedRelease: DiscogsRelease | null;
  /** Form data mapped from the Discogs release, ready to prefill the manual wizard. */
  mappedDraft: Partial<CreateMusicReleaseDto> | null;
  /** Original Discogs image URLs used to trigger a background download after save. */
  sourceImages: { cover: string | null; thumbnail: string | null };
}
