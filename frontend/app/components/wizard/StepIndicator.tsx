"use client";

import type { WizardStep } from "./types";

interface StepIndicatorProps {
  steps: WizardStep[];
  currentStep: number;
  /** Steps the user has already visited / touched */
  visitedSteps: number[];
  onStepClick: (stepId: number) => void;
}

/**
 * Horizontal step indicator displayed above each wizard panel.
 * Displays each step's status: completed, current, or upcoming.
 * Allows backward navigation by clicking completed steps.
 * On mobile the labels are hidden and only numbered circles are shown.
 */
export default function StepIndicator({
  steps,
  currentStep,
  visitedSteps,
  onStepClick,
}: StepIndicatorProps) {
  const isCompleted = (stepId: number) =>
    visitedSteps.includes(stepId) && stepId < currentStep;
  const isCurrent = (stepId: number) => stepId === currentStep;
  const isVisited = (stepId: number) => visitedSteps.includes(stepId);

  return (
    <div className="w-full overflow-x-auto pb-1">
      <div className="flex items-start w-full">
        {steps.map((step, idx) => {
          const completed = isCompleted(step.id);
          const current = isCurrent(step.id);
          const visited = isVisited(step.id);
          const isLast = idx === steps.length - 1;

          const canNavigate = completed || (visited && !current);

          return (
            <div key={step.id} className={`flex items-start ${isLast ? "" : "flex-1"}`}>
              {/* Step circle + label */}
              <button
                type="button"
                onClick={() => canNavigate && onStepClick(step.id)}
                disabled={!canNavigate}
                className={`flex flex-col items-center gap-1 w-[62px] flex-shrink-0 transition-opacity ${
                  canNavigate
                    ? "cursor-pointer opacity-100"
                    : current
                    ? "cursor-default opacity-100"
                    : "cursor-default opacity-40"
                }`}
                aria-current={current ? "step" : undefined}
              >
                {/* Circle */}
                <div
                  className={`w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold border-2 transition-all ${
                    current
                      ? "bg-[var(--theme-accent)] border-[var(--theme-accent)] text-white shadow-lg"
                      : completed
                      ? "bg-[var(--theme-accent)]/15 border-[var(--theme-accent)] text-[var(--theme-accent)]"
                      : "bg-[var(--theme-card-bg)] border-[var(--theme-card-border)] text-[var(--theme-card-text)]/50"
                  }`}
                >
                  {completed ? (
                    <svg
                      className="w-4 h-4"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      strokeWidth={2.5}
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  ) : (
                    <span>{step.id + 1}</span>
                  )}
                </div>

                {/* Label (hidden on smallest screens) */}
                <span
                  className={`hidden sm:block text-[9px] font-semibold text-center max-w-[60px] leading-tight uppercase tracking-wide ${
                    current
                      ? "text-[var(--theme-accent)]"
                      : completed
                      ? "text-[var(--theme-accent)]/80"
                      : "text-[var(--theme-card-text)]/50"
                  }`}
                >
                  {step.title}
                </span>
              </button>

              {/* Connector line */}
              {!isLast && (
                <div className="flex-1 mx-1 mt-3.5 h-0.5 min-w-[8px] bg-[var(--theme-card-border)] overflow-hidden">
                  <div
                    className="h-full bg-[var(--theme-accent)] transition-all duration-500"
                    style={{ width: completed ? "100%" : "0%" }}
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
