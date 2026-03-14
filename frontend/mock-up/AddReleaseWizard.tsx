"use client";

import { useState } from "react";
import type { MockFormData, ValidationErrors } from "./types";
import { WIZARD_STEPS, SEED_FORM_DATA, EMPTY_FORM_DATA } from "./fixtures";
import StepIndicator from "./StepIndicator";
import BasicInformationPanel from "./panels/BasicInformationPanel";
import ClassificationPanel from "./panels/ClassificationPanel";
import LabelInformationPanel from "./panels/LabelInformationPanel";
import PurchaseInformationPanel from "./panels/PurchaseInformationPanel";
import ImagesPanel from "./panels/ImagesPanel";
import TrackListingPanel from "./panels/TrackListingPanel";
import ExternalLinksPanel from "./panels/ExternalLinksPanel";
import DraftPreviewPanel from "./panels/DraftPreviewPanel";

// ─── Validation ───────────────────────────────────────────────────────────────

/**
 * Validates the fields for the given step.
 * Only Basic Information (step 0) has required fields.
 */
function validateStep(step: number, data: MockFormData): ValidationErrors {
  if (step === 0) {
    const errors: ValidationErrors = {};
    if (!data.title.trim()) {
      errors.title = "Title is required";
    }
    if (data.artistNames.length === 0) {
      errors.artists =
        "At least one artist is required – search or type a name and press Enter";
    }
    return errors;
  }
  return {};
}

// ─── Wizard component ─────────────────────────────────────────────────────────

/**
 * AddReleaseWizard – the guided manual add-release flow.
 *
 * Renders one panel at a time with a step indicator at the top.
 * Navigation rules:
 *   - Previous is always available (where applicable).
 *   - Next is blocked on the current panel if it fails step validation.
 *   - Optional panels (steps 1–8) can always be skipped.
 *   - Step 9 is the Draft Preview, which has its own action bar.
 *
 * All data is in local state – no API calls are made from this mock-up.
 *
 * @param useSeedData - When true (default) the form is pre-filled with sample
 *   Iron Maiden data so every panel shows representative content immediately.
 */
export default function AddReleaseWizard({
  useSeedData = true,
}: {
  useSeedData?: boolean;
}) {
  const [currentStep, setCurrentStep] = useState(0);
  const [visitedSteps, setVisitedSteps] = useState<number[]>([0]);
  const [formData, setFormData] = useState<MockFormData>(
    useSeedData ? SEED_FORM_DATA : EMPTY_FORM_DATA
  );
  const [stepErrors, setStepErrors] = useState<ValidationErrors>({});
  const [savedMock, setSavedMock] = useState(false);

  const TOTAL_STEPS = WIZARD_STEPS.length; // 10 (0–9)
  const PREVIEW_STEP = TOTAL_STEPS - 1; // 7

  // ── Helpers ──────────────────────────────────────────────────────────────

  const markVisited = (step: number) => {
    setVisitedSteps((prev) =>
      prev.includes(step) ? prev : [...prev, step]
    );
  };

  const goToStep = (target: number) => {
    setStepErrors({});
    setCurrentStep(target);
    markVisited(target);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  // ── Navigation handlers ──────────────────────────────────────────────────

  const handleNext = () => {
    const errors = validateStep(currentStep, formData);
    if (Object.keys(errors).length > 0) {
      setStepErrors(errors);
      return;
    }
    setStepErrors({});
    goToStep(currentStep + 1);
  };

  const handlePrevious = () => {
    if (currentStep > 0) {
      goToStep(currentStep - 1);
    }
  };

  const handleStepClick = (stepId: number) => {
    // Only allow clicking back to already-visited steps
    if (visitedSteps.includes(stepId) && stepId !== currentStep) {
      goToStep(stepId);
    }
  };

  const handleSaveMock = () => {
    // Mock-up only – show a confirmation state instead of calling an API
    setSavedMock(true);
  };

  // ── Form data update ─────────────────────────────────────────────────────

  const handleChange = (updates: Partial<MockFormData>) => {
    setFormData((prev) => ({ ...prev, ...updates }));
    // Clear errors for any updated fields
    const updatedKeys = Object.keys(updates);
    if (updatedKeys.some((k) => k in stepErrors)) {
      setStepErrors((prev) => {
        const next = { ...prev };
        updatedKeys.forEach((k) => delete next[k]);
        return next;
      });
    }
  };

  // ── Current step meta ────────────────────────────────────────────────────

  const stepConfig = WIZARD_STEPS[currentStep];
  const isPreview = currentStep === PREVIEW_STEP;
  const isRequired = stepConfig.required;
  const hasErrors = Object.keys(stepErrors).length > 0;

  // ── Saved confirmation screen ────────────────────────────────────────────

  if (savedMock) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="text-center space-y-4 max-w-sm">
          <div className="w-16 h-16 rounded-full bg-emerald-600/20 border border-emerald-500/40 flex items-center justify-center mx-auto">
            <svg
              className="w-8 h-8 text-emerald-400"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2.5}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M4.5 12.75l6 6 9-13.5"
              />
            </svg>
          </div>
          <h2 className="text-2xl font-black text-white">
            Mock: Release Saved!
          </h2>
          <p className="text-gray-500 text-sm">
            In the real flow this would call{" "}
            <span className="font-mono text-gray-400">POST /api/musicreleases</span>,
            persist the release and redirect to its detail page.
          </p>
          <div className="flex flex-col gap-3 pt-2">
            <button
              onClick={() => {
                setSavedMock(false);
                setCurrentStep(0);
                setVisitedSteps([0]);
                setFormData(useSeedData ? SEED_FORM_DATA : EMPTY_FORM_DATA);
              }}
              className="px-6 py-2.5 rounded-xl text-sm font-bold bg-[#8B5CF6] hover:bg-[#7C3AED] text-white transition-colors"
            >
              Start again
            </button>
            <button
              onClick={() => setSavedMock(false)}
              className="px-6 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-400 hover:text-white transition-colors"
            >
              Back to preview
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="max-w-4xl mx-auto space-y-4">
      {/* ── Step indicator ──────────────────────────────────────────────── */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl p-3">
        <StepIndicator
          steps={WIZARD_STEPS}
          currentStep={currentStep}
          visitedSteps={visitedSteps}
          onStepClick={handleStepClick}
        />
      </div>

      {/* ── Panel card ──────────────────────────────────────────────────── */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl overflow-hidden">
        {/* Panel header */}
        <div className="px-6 py-3 border-b border-[#1C1C28] flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="text-[10px] font-bold uppercase tracking-widest text-gray-600">
                Step {currentStep + 1} of {TOTAL_STEPS}
              </span>
              {isRequired && (
                <span className="text-[10px] font-bold uppercase tracking-wider text-red-400 bg-red-500/10 border border-red-500/20 px-2 py-0.5 rounded">
                  Required
                </span>
              )}
              {!isRequired && !isPreview && (
                <span className="text-[10px] font-bold uppercase tracking-wider text-gray-600 bg-[#0F0F1A] border border-[#1C1C28] px-2 py-0.5 rounded">
                  Optional
                </span>
              )}
            </div>
            <h2 className="text-xl font-black text-white">{stepConfig.title}</h2>
            <p className="text-sm text-gray-500 mt-0.5">{stepConfig.description}</p>
          </div>

          {/* Step count mini progress */}
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
            />
          )}
          {currentStep === 1 && (
            <ClassificationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
            />
          )}
          {currentStep === 2 && (
            <LabelInformationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
            />
          )}
          {currentStep === 3 && (
            <PurchaseInformationPanel
              data={formData}
              onChange={handleChange}
              errors={stepErrors}
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
              onSaveMock={handleSaveMock}
            />
          )}
        </div>

        {/* Panel footer navigation (not shown on Draft Preview which has its own bar) */}
        {!isPreview && (
          <div className="px-6 py-3 border-t border-[#1C1C28] flex items-center justify-between gap-4">
            {/* Back */}
            <button
              type="button"
              onClick={handlePrevious}
              disabled={currentStep === 0}
              className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border transition-colors ${
                currentStep === 0
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
              Previous
            </button>

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

      {/* ── Data summary debug panel (development aid) ──────────────────── */}
      <details className="bg-[#0A0A10] border border-[#1C1C28] rounded-xl overflow-hidden">
        <summary className="px-4 py-3 text-xs text-gray-600 uppercase tracking-wider font-semibold cursor-pointer hover:text-gray-400 transition-colors select-none">
          Developer view – Current form data (collapsed by default)
        </summary>
        <pre className="px-4 pb-4 text-[11px] text-gray-500 overflow-x-auto whitespace-pre-wrap">
          {JSON.stringify(formData, null, 2)}
        </pre>
      </details>
    </div>
  );
}
