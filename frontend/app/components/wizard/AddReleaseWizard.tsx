"use client";

import { useState, useCallback, useRef } from "react";
import type { CreateMusicReleaseDto } from "../AddReleaseForm";
import {
  type WizardFormData,
  type ValidationErrors,
  WIZARD_STEPS,
  EMPTY_FORM_DATA,
  fromCreateDto,
  toCreateDto,
} from "./types";
import { useReleaseLookups } from "./useReleaseLookups";
import StepIndicator from "./StepIndicator";
import BasicInformationPanel from "./panels/BasicInformationPanel";
import ClassificationPanel from "./panels/ClassificationPanel";
import LabelInformationPanel from "./panels/LabelInformationPanel";
import PurchaseInformationPanel from "./panels/PurchaseInformationPanel";
import ImagesPanel from "./panels/ImagesPanel";
import TrackListingPanel from "./panels/TrackListingPanel";
import ExternalLinksPanel from "./panels/ExternalLinksPanel";
import DraftPreviewPanel from "./panels/DraftPreviewPanel";
import ConfirmDialog from "./ConfirmDialog";
import { fetchJson } from "../../lib/api";

// ─── Validation ────────────────────────────────────────────────────────────────

/**
 * Validates field values for the given wizard step.
 * Only step 0 (Basic Information) has required fields.
 */
function validateStep(step: number, data: WizardFormData): ValidationErrors {
  if (step === 0) {
    const errors: ValidationErrors = {};
    if (!data.title.trim()) {
      errors.title = "Title is required";
    }
    if (data.artistIds.length === 0 && data.artistNames.length === 0) {
      errors.artists =
        "At least one artist is required – search or type a name and press Enter";
    }
    return errors;
  }
  return {};
}

// ─── Props ─────────────────────────────────────────────────────────────────────

interface AddReleaseWizardProps {
  /**
   * Optional pre-fill data (e.g. from Discogs import).
   * Passed through fromCreateDto() so the wizard form understands it.
   */
  initialData?: Partial<CreateMusicReleaseDto>;
  /** Called with the new release's ID after a successful POST */
  onSuccess?: (releaseId: number) => void;
  /** Called when the user abandons the wizard */
  onCancel?: () => void;
}

// ─── Component ─────────────────────────────────────────────────────────────────

/**
 * AddReleaseWizard – the guided manual add-release flow.
 *
 * Renders one panel at a time behind a step indicator and fires a real
 * POST /api/musicreleases when the user confirms the draft preview.
 *
 * Navigation rules:
 *  - Back is always available except on step 0.
 *  - Next validates the current step before advancing; only step 0 requires fields.
 *  - Optional panels (steps 1–6) can always be skipped.
 *  - Step 7 (Draft Preview) has its own action bar; the standard footer is hidden.
 *
 * On first mount the hook fetches lookup lists (artists, labels, genres, formats,
 * packagings, countries, stores) in parallel and shows a loading spinner until
 * they are ready. Lookup errors are displayed inline; the user can still proceed.
 */
export default function AddReleaseWizard({
  initialData,
  onSuccess,
  onCancel,
}: AddReleaseWizardProps) {
  // ── Form state ──────────────────────────────────────────────────────────────
  const [formData, setFormData] = useState<WizardFormData>(() =>
    initialData ? fromCreateDto(initialData) : { ...EMPTY_FORM_DATA }
  );
  const [currentStep, setCurrentStep] = useState(0);
  const [visitedSteps, setVisitedSteps] = useState<number[]>([0]);

  // ── Lookup data (lazy-loaded per step group) ─────────────────────────────
  const lookups = useReleaseLookups(currentStep);
  const [stepErrors, setStepErrors] = useState<ValidationErrors>({});
  // Ref mirrors stepErrors so handleNext always reads the latest value even
  // when blur and click fire before a re-render commits.
  const stepErrorsRef = useRef<ValidationErrors>({});
  const updateStepErrors = useCallback((e: ValidationErrors) => {
    stepErrorsRef.current = e;
    setStepErrors(e);
  }, []);

  // ── Submit state ────────────────────────────────────────────────────────────
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);

  const TOTAL_STEPS = WIZARD_STEPS.length;
  const PREVIEW_STEP = TOTAL_STEPS - 1;

  // ── Helpers ─────────────────────────────────────────────────────────────────

  const markVisited = (step: number) => {
    setVisitedSteps((prev) => (prev.includes(step) ? prev : [...prev, step]));
  };

  const goToStep = (target: number) => {
    updateStepErrors({});
    setCurrentStep(target);
    markVisited(target);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  // ── Navigation ───────────────────────────────────────────────────────────────

  const handleNext = () => {
    const errors = validateStep(currentStep, formData);
    // Merge with any panel-injected errors (e.g. invalid durations) read from
    // the ref so we see the value set by the blur handler even when blur and
    // click fire before a re-render commits.
    const merged = { ...stepErrorsRef.current, ...errors };
    if (Object.keys(merged).length > 0) {
      updateStepErrors(merged);
      return;
    }
    updateStepErrors({});
    goToStep(currentStep + 1);
  };

  const handlePrevious = () => {
    if (currentStep > 0) goToStep(currentStep - 1);
  };

  const handleStepClick = (stepId: number) => {
    if (visitedSteps.includes(stepId) && stepId !== currentStep) {
      goToStep(stepId);
    }
  };

  // ── Form update ──────────────────────────────────────────────────────────────

  const handleChange = (updates: Partial<WizardFormData>) => {
    setFormData((prev) => ({ ...prev, ...updates }));
    // Clear validation errors for any field that was just updated.
    // `artistIds`/`artistNames` are the underlying data keys but the validation
    // error is stored under the display key `artists`, so include it explicitly.
    const updatedKeys = Object.keys(updates);
    if (updatedKeys.includes("artistIds") || updatedKeys.includes("artistNames")) {
      updatedKeys.push("artists");
    }
    if (updatedKeys.some((k) => k in stepErrors)) {
      updateStepErrors(
        Object.fromEntries(
          Object.entries(stepErrorsRef.current).filter(([k]) => !updatedKeys.includes(k))
        )
      );
    }
  };

  // ── Submit ───────────────────────────────────────────────────────────────────

  const handleSubmit = async () => {
    setIsSubmitting(true);
    setSubmitError(null);
    try {
      const dto = toCreateDto(formData);
      const response = await fetchJson<{ release: { id: number } }>(
        "/api/musicreleases",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(dto),
        }
      );
      const releaseId = response?.release?.id;
      if (releaseId) {
        onSuccess?.(releaseId);
      } else {
        setSubmitError(
          "The release was saved but the server did not return an ID. Please check your collection."
        );
      }
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : "An unexpected error occurred.";
      setSubmitError(`Failed to save release: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ── Derived state ────────────────────────────────────────────────────────────

  const stepConfig = WIZARD_STEPS[currentStep];
  const isPreview = currentStep === PREVIEW_STEP;
  const hasErrors = Object.keys(stepErrors).length > 0;

  // ── Loading / error state for lookups ────────────────────────────────────────

  if (lookups.loading) {
    return (
      <div className="max-w-4xl mx-auto flex items-center justify-center min-h-[40vh]">
        <div className="text-center space-y-3">
          <svg
            className="w-8 h-8 animate-spin text-[#8B5CF6] mx-auto"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
            />
          </svg>
          <p className="text-gray-500 text-sm">Loading release data…</p>
        </div>
      </div>
    );
  }

  // ── Render ───────────────────────────────────────────────────────────────────

  return (
    <>
    <div className="max-w-4xl mx-auto space-y-4">
      {/* Non-blocking lookup error warning */}
      {lookups.error && (
        <div className="flex items-start gap-3 bg-yellow-500/10 border border-yellow-500/30 rounded-xl p-3">
          <p className="text-sm text-yellow-300">
            Some lookup lists could not be loaded. You can still proceed but
            autocomplete options may be limited.
          </p>
        </div>
      )}

      {/* ── Step indicator ────────────────────────────────────────────────── */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl p-3">
        <StepIndicator
          steps={WIZARD_STEPS}
          currentStep={currentStep}
          visitedSteps={visitedSteps}
          onStepClick={handleStepClick}
        />
      </div>

      {/* ── Panel card ────────────────────────────────────────────────────── */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl overflow-hidden">
        {/* Panel header */}
        <div className="px-6 py-3 border-b border-[#1C1C28] flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="text-[10px] font-bold uppercase tracking-widest text-gray-600">
                Step {currentStep + 1} of {TOTAL_STEPS}
              </span>
              {stepConfig.required && (
                <span className="text-[10px] font-bold uppercase tracking-wider text-red-400 bg-red-500/10 border border-red-500/20 px-2 py-0.5 rounded">
                  Required
                </span>
              )}
              {!stepConfig.required && !isPreview && (
                <span className="text-[10px] font-bold uppercase tracking-wider text-gray-600 bg-[#0F0F1A] border border-[#1C1C28] px-2 py-0.5 rounded">
                  Optional
                </span>
              )}
            </div>
            <h2 className="text-xl font-black text-white">{stepConfig.title}</h2>
            <p className="text-sm text-gray-500 mt-0.5">{stepConfig.description}</p>
          </div>

          {/* Step progress dots */}
          <div className="flex-shrink-0 hidden sm:flex gap-1 pt-1">
            {WIZARD_STEPS.map((s) => (
              <div
                key={s.id}
                className={`h-1.5 rounded-full transition-all duration-300 ${
                  s.id < currentStep
                    ? "w-3 bg-[#8B5CF6]"
                    : s.id === currentStep
                    ? "w-5 bg-[#8B5CF6] shadow-[0_0_6px_rgba(139,92,246,0.6)]"
                    : "w-2 bg-[#1C1C28]"
                }`}
              />
            ))}
          </div>
        </div>

        {/* Panel body */}
        <div className="px-6 py-4">
          {currentStep === 0 && (
            <BasicInformationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
              lookups={lookups}
            />
          )}
          {currentStep === 1 && (
            <ClassificationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
              lookups={lookups}
            />
          )}
          {currentStep === 2 && (
            <LabelInformationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
              lookups={lookups}
            />
          )}
          {currentStep === 3 && (
            <PurchaseInformationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
              lookups={lookups}
            />
          )}
          {currentStep === 4 && (
            <ImagesPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
            />
          )}
          {currentStep === 5 && (
            <TrackListingPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
              onErrors={updateStepErrors}
            />
          )}
          {currentStep === 6 && (
            <ExternalLinksPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
            />
          )}
          {currentStep === 7 && (
            <DraftPreviewPanel
              data={formData}
              onGoBack={handlePrevious}
              onSubmit={handleSubmit}
              onCancel={onCancel}
              isSubmitting={isSubmitting}
              submitError={submitError}
            />
          )}
        </div>

        {/* Panel footer navigation (hidden on Draft Preview which has its own bar) */}
        {!isPreview && (
          <div className="px-6 py-3 border-t border-[#1C1C28] flex items-center justify-between gap-4">
            {/* Primary left action: Cancel (step 0) or Previous (later steps) */}
            <div className="flex items-center gap-2">
              {currentStep === 0 ? (
                <button
                  type="button"
                  onClick={onCancel}
                  disabled={!onCancel}
                  className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border transition-colors ${
                    !onCancel
                      ? "border-[#1C1C28] text-gray-700 cursor-not-allowed"
                      : "border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50"
                  }`}
                >
                  <svg
                    className="w-4 h-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    strokeWidth={2}
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M15.75 19.5L8.25 12l7.5-7.5"
                    />
                  </svg>
                  Cancel
                </button>
              ) : (
                <button
                  type="button"
                  onClick={handlePrevious}
                  className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border transition-colors border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50"
                >
                  <svg
                    className="w-4 h-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    strokeWidth={2}
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M15.75 19.5L8.25 12l7.5-7.5"
                    />
                  </svg>
                  Previous
                </button>
              )}
            </div>

            {/* Validation summary */}
            {hasErrors && (
              <div className="flex-1 text-center">
                <p className="text-sm text-red-400 font-semibold">
                  Please fix the highlighted fields before continuing
                </p>
              </div>
            )}

            {/* Next */}
            <button
              type="button"
              onClick={handleNext}
              className={`flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold transition-all ${
                hasErrors
                  ? "bg-[#8B5CF6]/30 text-[#8B5CF6]/60 cursor-not-allowed"
                  : "bg-[#8B5CF6] hover:bg-[#7C3AED] text-white shadow-lg shadow-[#8B5CF6]/20"
              }`}
            >
              {currentStep === PREVIEW_STEP - 1 ? "Preview Release" : "Next"}
              <svg
                className="w-4 h-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M8.25 4.5l7.5 7.5-7.5 7.5"
                />
              </svg>
            </button>
          </div>
        )}
      </div>
    </div>
    <ConfirmDialog
      isOpen={showCancelConfirm}
      onConfirm={() => { setShowCancelConfirm(false); onCancel?.(); }}
      onDismiss={() => setShowCancelConfirm(false)}
    />
    </>
  );
}
