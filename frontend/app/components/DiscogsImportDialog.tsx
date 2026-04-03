import { useEffect, useRef, useState, useCallback } from "react";
import { fetchJson } from "../lib/api";

const IMPORT_PHASE_MUSIC_PROMPTS = [
  "Crate-digging through Discogs shelves...",
  "Lining up the next stack of records...",
  "Cueing side B and syncing metadata...",
  "Nudging the pitch while indexes settle...",
];

const TRACKLIST_PHASE_MUSIC_PROMPTS = [
  "Decoding grooves into tracklists...",
  "Reading tiny liner notes at light speed...",
  "Tuning waveform details for each release...",
  "Dropping needles on every track boundary...",
];

const COVER_PHASE_MUSIC_PROMPTS = [
  "Polishing cover art sleeves...",
  "Hanging album jackets in your archive...",
  "Color-correcting crate-front classics...",
  "Sleeving final artwork for playback...",
];

const IMPORT_PHASE_MAX_PERCENTAGE = 85;
const TRACKLIST_PHASE_MAX_PERCENTAGE = 95;
const COVER_PHASE_VISIBLE_MAX_PERCENTAGE = 99;

interface ProgressStep {
  id: number;
  title: string;
  prompt: string;
  current: number;
  total: number;
  isComplete: boolean;
  isActive: boolean;
}

function buildProgressSteps(progress: ImportProgress, completedItems: number): ProgressStep[] {
  const pct = Math.max(0, Math.min(100, progress.percentage ?? 0));
  const importTotal = Math.max(1, progress.effectiveTotal || 0);
  const importCurrent = progress.completed
    ? importTotal
    : Math.min(importTotal, completedItems);

  const enrichTotal = Math.max(1, progress.imported || importTotal);
  const enrichRatio = pct <= IMPORT_PHASE_MAX_PERCENTAGE
    ? 0
    : Math.min(1, (Math.min(pct, TRACKLIST_PHASE_MAX_PERCENTAGE) - IMPORT_PHASE_MAX_PERCENTAGE) / (TRACKLIST_PHASE_MAX_PERCENTAGE - IMPORT_PHASE_MAX_PERCENTAGE));
  const enrichCurrent = progress.completed
    ? enrichTotal
    : Math.min(enrichTotal, Math.round(enrichRatio * enrichTotal));

  const coverTotal = enrichTotal;
  const coverRatio = pct <= TRACKLIST_PHASE_MAX_PERCENTAGE
    ? 0
    : Math.min(1, (Math.min(pct, COVER_PHASE_VISIBLE_MAX_PERCENTAGE) - TRACKLIST_PHASE_MAX_PERCENTAGE) / (COVER_PHASE_VISIBLE_MAX_PERCENTAGE - TRACKLIST_PHASE_MAX_PERCENTAGE));
  const coverCurrent = progress.completed
    ? coverTotal
    : Math.min(coverTotal, Math.round(coverRatio * coverTotal));

  const importComplete = progress.completed || pct >= IMPORT_PHASE_MAX_PERCENTAGE;
  const enrichComplete = progress.completed || pct >= TRACKLIST_PHASE_MAX_PERCENTAGE;
  const coverComplete = progress.completed || pct >= COVER_PHASE_VISIBLE_MAX_PERCENTAGE;

  const importPrompt = importComplete
    ? "Grooves indexed and ready."
    : IMPORT_PHASE_MUSIC_PROMPTS[completedItems % IMPORT_PHASE_MUSIC_PROMPTS.length];
  const enrichPrompt = enrichComplete
    ? "Tracklists locked in the crate."
    : (pct < IMPORT_PHASE_MAX_PERCENTAGE
      ? "Waiting for the first stack to land..."
      : TRACKLIST_PHASE_MUSIC_PROMPTS[completedItems % TRACKLIST_PHASE_MUSIC_PROMPTS.length]);
  const coverPrompt = coverComplete
    ? "Artwork sleeves shelved."
    : (pct < TRACKLIST_PHASE_MAX_PERCENTAGE
      ? "Queued for the final polishing pass..."
      : COVER_PHASE_MUSIC_PROMPTS[completedItems % COVER_PHASE_MUSIC_PROMPTS.length]);

  const steps: ProgressStep[] = [
    {
      id: 1,
      title: "Step 1/3: Importing releases",
      prompt: importPrompt,
      current: importCurrent,
      total: importTotal,
      isComplete: importComplete,
      isActive: false,
    },
    {
      id: 2,
      title: "Step 2/3: Enriching tracklists",
      prompt: enrichPrompt,
      current: enrichCurrent,
      total: enrichTotal,
      isComplete: enrichComplete,
      isActive: false,
    },
    {
      id: 3,
      title: "Step 3/3: Mirroring cover art",
      prompt: coverPrompt,
      current: coverCurrent,
      total: coverTotal,
      isComplete: coverComplete,
      isActive: false,
    },
  ];

  const firstPendingIndex = steps.findIndex((step) => !step.isComplete);
  if (firstPendingIndex >= 0) {
    steps[firstPendingIndex].isActive = true;
  }

  return steps;
}

function ImportProgressWheel({ progress }: { progress: ImportProgress }) {
  const completedItems = progress.imported + progress.skipped + progress.failed;
  const rawPercentage = Math.min(
    100,
    Math.round((completedItems / Math.max(1, progress.effectiveTotal)) * 100)
  );
  const percentage = Math.max(0, Math.min(100, progress.percentage ?? rawPercentage));
  const steps = buildProgressSteps(progress, completedItems);
  const currentStep = steps.find((step) => step.isActive) ?? steps[steps.length - 1];

  return (
    <div className="w-full rounded-2xl border border-[var(--theme-card-border)] bg-[var(--theme-body-bg-start)]/60 p-3">
      <div className="mx-auto flex w-full items-center gap-3 sm:gap-4">
        <div
          className="relative h-36 w-36 shrink-0"
          role="progressbar"
          aria-label="Discogs import progress"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={percentage}
          data-testid="discogs-import-progress-spinner"
        >
          <div className="absolute inset-0 rounded-full bg-[radial-gradient(circle_at_center,color-mix(in_srgb,var(--theme-accent)_24%,transparent_76%)_0%,transparent_72%)]" />
          <div className="absolute inset-[4px] rounded-full bg-[conic-gradient(from_0deg,color-mix(in_srgb,var(--theme-accent)_22%,transparent_78%)_0deg,transparent_130deg,color-mix(in_srgb,var(--theme-accent)_14%,transparent_86%)_220deg,transparent_360deg)] opacity-80 motion-safe:animate-[spin_6.8s_linear_infinite] motion-reduce:animate-none" />
          <div className="absolute inset-[10px] rounded-full border-2 border-[var(--theme-card-border)]/65" />
          <div className="absolute inset-[10px] rounded-full border-[4px] border-transparent border-t-[var(--theme-accent)] border-r-[var(--theme-accent)]/75 motion-safe:animate-[spin_2.4s_linear_infinite] motion-reduce:animate-none" />
          <div className="absolute inset-[16px] rounded-full border-[3px] border-transparent border-b-[var(--theme-accent)]/55 border-l-[var(--theme-accent)]/35 motion-safe:animate-[spin_1.75s_linear_infinite_reverse] motion-reduce:animate-none" />
          <div className="absolute inset-[20px] rounded-full border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] shadow-[0_18px_42px_rgba(0,0,0,0.2)]" />
          <div className="absolute inset-[22px] rounded-full bg-[radial-gradient(circle_at_center,color-mix(in_srgb,var(--theme-accent)_12%,transparent_88%)_0%,transparent_72%)] motion-safe:animate-pulse motion-reduce:animate-none" />
          <div className="absolute inset-[6px] rounded-full border border-[var(--theme-accent)]/20 blur-[1px]" />

          <div className="absolute inset-[24px] flex items-center justify-center rounded-full border border-[color-mix(in_srgb,var(--theme-accent)_28%,white_12%)] bg-[var(--theme-card-bg)]/95 shadow-[0_0_0_2px_color-mix(in_srgb,var(--theme-accent)_10%,transparent),0_8px_20px_rgba(0,0,0,0.18)]">
            <img
              src="https://www.discogs.com/favicon.ico"
              alt="Discogs"
              className="h-12 w-12 object-contain"
              data-testid="discogs-import-logo"
            />
          </div>
        </div>

        <div className="min-w-0 flex-[1.35] space-y-2" data-testid="discogs-import-steps-panel">
          {steps.map((step) => (
            <div
              key={step.id}
              data-testid={`discogs-import-step-${step.id}`}
              className={`rounded-xl border px-3 py-2 transition-colors ${step.isActive
                ? "border-[var(--theme-accent)]/55 bg-[color-mix(in_srgb,var(--theme-accent)_10%,var(--theme-card-bg)_90%)]"
                : "border-[var(--theme-card-border)] bg-[var(--theme-card-bg)]"}`}
            >
              <div className="flex items-center justify-between gap-2">
                <div
                  data-testid={`discogs-import-step-title-${step.id}`}
                  className="min-w-0 overflow-hidden text-ellipsis whitespace-nowrap pr-2 text-[10px] font-semibold uppercase leading-none tracking-[0.05em] text-[var(--theme-accent)]/90 sm:text-[11px]"
                  title={step.title}
                >
                  {step.title}
                </div>
                {step.isComplete ? (
                  <span
                    className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full border border-[var(--theme-accent)]/50 bg-[color-mix(in_srgb,var(--theme-accent)_18%,transparent)]"
                    data-testid={`discogs-import-step-complete-${step.id}`}
                  >
                    <svg viewBox="0 0 16 16" className="h-3 w-3" aria-hidden="true">
                      <path d="M3 8.5L6.3 11.8L13 5.1" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--theme-accent)]" />
                    </svg>
                  </span>
                ) : (
                  <span
                    className={`inline-block h-2.5 w-2.5 shrink-0 rounded-full ${step.isActive ? "bg-[var(--theme-accent)] animate-pulse" : "bg-[var(--theme-card-border)]/70"}`}
                    data-testid={`discogs-import-step-pending-${step.id}`}
                  />
                )}
              </div>

              <div className="mt-1 flex items-center justify-between gap-2">
                <div className="text-xs font-semibold text-[var(--theme-card-text)]/75" data-testid={`discogs-import-step-count-${step.id}`}>
                  {step.current}/{step.total}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="mt-3 rounded-xl border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] px-3 py-2 text-center" data-testid="discogs-import-current-status">
        <div className="text-xs leading-tight text-[var(--theme-card-text)]/70" data-testid="discogs-import-current-status-text">
          {currentStep.prompt}
        </div>
      </div>

      <div className="mt-2 rounded-xl border border-amber-400/25 bg-amber-500/8 px-3 py-2 text-center" data-testid="discogs-import-warning">
        <div className="text-[11px] font-medium leading-tight text-amber-200/90">
          Do not close or refresh your browser during the import. Wait until the process is fully complete.
        </div>
      </div>
    </div>
  );
}

export interface DiscogsImportDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

interface ImportJobSubmission {
  jobId: string;
  status: string;
}

interface ImportResult {
  success: boolean;
  totalReleases: number;
  importedReleases: number;
  skippedReleases: number;
  failedReleases: number;
  errors: string[];
  errorMessage?: string | null;
  duration: string;
}

interface ImportProgress {
  totalReleases: number;
  effectiveTotal: number;
  imported: number;
  skipped: number;
  failed: number;
  percentage?: number;
  completed: boolean;
}

interface ImportJobStatus extends ImportProgress {
  jobId: string;
  status: string;
  success: boolean;
  errors: string[];
  errorMessage?: string | null;
  duration?: string;
}

/**
 * Dialog for importing collection from Discogs
 */
export function DiscogsImportDialog({
  isOpen,
  onClose,
  onSuccess,
}: DiscogsImportDialogProps) {
  const [username, setUsername] = useState("");
  const [isImporting, setIsImporting] = useState(false);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [progress, setProgress] = useState<ImportProgress | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleClose = useCallback(() => {
    if (!isImporting) {
      // If import was successful, trigger the success callback
      if (result?.success) {
        try { window.dispatchEvent(new CustomEvent('collectionChanged')); } catch {}
        onSuccess();
      }
      setUsername("");
      setResult(null);
      setProgress(null);
      setError(null);
      onClose();
    }
  }, [isImporting, result, onSuccess, onClose]);

  // Focus username input when dialog opens
  useEffect(() => {
    if (isOpen && inputRef.current && !isImporting && !result) {
      inputRef.current.focus();
    }
  }, [isOpen, isImporting, result]);

  // Handle Escape key to close dialog
  // include `handleClose` in deps since it's referenced in the effect
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen && !isImporting) {
        handleClose();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      document.body.style.overflow = "hidden";
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "";
    };
  }, [isOpen, isImporting, handleClose]);

  const handleImport = async () => {
    if (!username.trim()) {
      setError("Please enter a Discogs username");
      return;
    }

    setIsImporting(true);
    setError(null);
    setResult(null);
    setProgress(null);

    let pollId: number | undefined;

    const normalizeStatus = (p: any): ImportJobStatus | null => {
      if (!p) return null;
      const jobId = p.jobId ?? p.JobId ?? "";
      const status = p.status ?? p.Status ?? "Queued";
      const totalReleases = p.totalReleases ?? p.TotalReleases ?? 0;
      const effectiveTotal = p.effectiveTotal ?? p.EffectiveTotal ?? totalReleases;
      const imported = p.imported ?? p.Imported ?? 0;
      const skipped = p.skipped ?? p.Skipped ?? 0;
      const failed = p.failed ?? p.Failed ?? 0;
      const percentage = p.percentage ?? p.Percentage;
      const completed = p.completed ?? p.Completed ?? false;
      const success = p.success ?? p.Success ?? false;
      const errorMessage = p.errorMessage ?? p.ErrorMessage ?? null;
      const errors = p.errors ?? p.Errors ?? [];
      const duration = p.duration ?? (p.Duration ? String(p.Duration) : "");

      return {
        jobId,
        status,
        totalReleases,
        effectiveTotal,
        imported,
        skipped,
        failed,
        percentage,
        completed,
        success,
        errorMessage,
        errors,
        duration,
      };
    };

    const setResultFromStatus = (status: ImportJobStatus) => {
      setProgress(status);
      setResult({
        success: status.success,
        totalReleases: status.totalReleases,
        importedReleases: status.imported,
        skippedReleases: status.skipped,
        failedReleases: status.failed,
        errors: status.errors,
        errorMessage: status.errorMessage,
        duration: status.duration ?? "",
      });

      if (!status.success) {
        setError(status.errorMessage ?? status.errors[0] ?? "Failed to import from Discogs. Please try again.");
      }
    };

    try {
      const submission = await fetchJson<ImportJobSubmission>("/api/import/discogs", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ username: username.trim() }),
        timeoutMs: 30000,
      });

      await new Promise<void>((resolve) => {
        const poll = async () => {
          try {
            const statusResponse = await fetchJson<any>(`/api/import/discogs/status?jobId=${encodeURIComponent(submission.jobId)}`, {
              method: "GET",
              swallowErrors: true,
              timeoutMs: 5000,
            });

            const status = normalizeStatus(statusResponse);
            if (!status) {
              return;
            }

            setProgress(status);

            if (status.completed) {
              setResultFromStatus(status);
              if (pollId) {
                window.clearInterval(pollId);
                pollId = undefined;
              }
              resolve();
            }
          } catch {
            // Ignore transient polling failures and keep waiting for the next interval.
          }
        };

        pollId = window.setInterval(() => {
          void poll();
        }, 1000);

        void poll();
      });
    } catch (err) {
      console.error("Error importing from Discogs:", err);
      const e = err as unknown;
      if (err instanceof Error && err.name === 'AbortError') {
        setError('Import timed out. Please try again or contact support.');
      } else if (e && typeof e === 'object') {
        const errObj = e as Record<string, unknown>;
        const details = errObj.details;
        if (details && typeof details === 'object' && typeof (details as Record<string, unknown>).error === 'string') {
          setError((details as Record<string, string>).error);
        } else if (typeof errObj.message === 'string') {
          setError(errObj.message);
        } else {
          setError('Failed to import from Discogs. Please try again.');
        }
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to import from Discogs. Please try again.');
      }
    } finally {
      if (pollId) window.clearInterval(pollId);
      setIsImporting(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !isImporting && !result) {
      handleImport();
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isImporting) {
          handleClose();
        }
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="import-dialog-title"
    >
      <div
        ref={dialogRef}
        className="bg-[var(--theme-card-bg)] rounded-lg border border-[var(--theme-card-border)] shadow-xl max-w-2xl w-full p-4 transform transition-all"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Dialog Header */}
        <div className="mb-2">
          <h2
            id="import-dialog-title"
            className="text-xl font-semibold text-[var(--theme-card-text)]"
          >
            Import from Discogs
          </h2>
          {/* Subheading removed per request */}
        </div>

        {/* Dialog Content */}
        <div className="mb-4">
          {!result && !isImporting && (
            <>
              <label
                htmlFor="discogs-username"
                className="block text-sm font-medium text-[var(--theme-card-text)] mb-2"
              >
                Discogs Username
              </label>
              <input
                ref={inputRef}
                id="discogs-username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                onKeyPress={handleKeyPress}
                className="w-full px-3 py-2 bg-[var(--theme-body-bg-start)] border border-[var(--theme-card-border)] rounded-md text-[var(--theme-card-text)] placeholder:text-[var(--theme-card-text)]/45 focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)] focus:border-transparent"
                placeholder="your_username"
                disabled={isImporting}
              />
              {error && (
                <p className="text-red-400 text-sm mt-2" role="alert">
                  {error}
                </p>
              )}
            </>
          )}

          {isImporting && (
            <div className="flex flex-col items-center justify-center py-4 w-full">
              {progress ? (
                <ImportProgressWheel progress={progress} />
              ) : (
                <div className="flex flex-col items-center justify-center py-4">
                  <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[var(--theme-accent)] mb-2"></div>
                  <p className="text-[var(--theme-card-text)] text-center text-sm">
                    This may take several minutes for large collections. Please do not close this dialog.
                  </p>
                </div>
              )}
            </div>
          )}

          {result && (
            <div className="space-y-2">
              {result.success ? (
                <div className="bg-green-900/30 border border-green-500/50 rounded-md p-3">
                  <h3 className="text-green-400 font-semibold mb-1">
                    Import Successful!
                  </h3>
                  <div className="text-sm text-[var(--theme-card-text)]/85 space-y-1">
                    <p>Total releases: {result.totalReleases}</p>
                    <p>Imported: {result.importedReleases}</p>
                    <p>Skipped (already exist): {result.skippedReleases}</p>
                    {result.failedReleases > 0 && (
                      <p className="text-yellow-400">
                        Failed: {result.failedReleases}
                      </p>
                    )}
                    <p className="text-[var(--theme-card-text)]/65 text-xs mt-1">
                      Duration: {result.duration}
                    </p>
                  </div>
                </div>
              ) : (
                <div className="bg-red-900/30 border border-red-500/50 rounded-md p-3">
                  <h3 className="text-red-400 font-semibold mb-1">
                    Import Failed
                  </h3>
                  <p className="text-sm text-[var(--theme-card-text)]/85">
                    {result.errors.length > 0
                      ? result.errors[0]
                      : (result.errorMessage ?? "An unknown error occurred")}
                  </p>
                </div>
              )}

              {(result.errors?.length ?? 0) > 1 && (
                <details className="text-sm">
                  <summary className="text-[var(--theme-card-text)]/70 cursor-pointer hover:text-[var(--theme-card-text)]">
                    View all errors ({result.errors?.length ?? 0})
                  </summary>
                  <div className="mt-2 space-y-1 max-h-40 overflow-y-auto">
                    {(result.errors ?? []).slice(0, 10).map((err, idx) => (
                      <p key={idx} className="text-[var(--theme-card-text)]/70 text-xs">
                        • {err}
                      </p>
                    ))}
                    {(result.errors?.length ?? 0) > 10 && (
                      <p className="text-[var(--theme-card-text)]/55 text-xs italic">
                        ... and {(result.errors?.length ?? 0) - 10} more errors
                      </p>
                    )}
                  </div>
                </details>
              )}
            </div>
          )}
        </div>

        {/* Dialog Actions */}
        <div className="flex gap-3 justify-end">
          {!isImporting && (
            <button
              type="button"
              onClick={handleClose}
              className="px-4 py-2 text-sm font-medium text-[var(--theme-card-text)]/85 bg-transparent border border-[var(--theme-card-border)] rounded-md hover:border-[var(--theme-accent)]/50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--theme-accent)]"
            >
              {result ? "Close" : "Cancel"}
            </button>
          )}
          {!result && !isImporting && (
            <button
              type="button"
              onClick={handleImport}
              disabled={!username.trim()}
              className="px-4 py-2 text-sm font-medium text-white bg-[var(--theme-accent)] rounded-md hover:bg-[var(--theme-accent-hover)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--theme-accent)] disabled:opacity-50 disabled:cursor-not-allowed shadow-lg"
            >
              Import Collection
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
