import { useEffect, useRef, useState, useCallback } from "react";
import { fetchJson } from "../lib/api";

const PROGRESS_SEGMENT_COUNT = 32;

const WHEEL_SIZE = 160;
const WHEEL_CENTER = WHEEL_SIZE / 2;
const SEGMENT_INNER_RADIUS = 54;
const SEGMENT_OUTER_RADIUS = 69;
const SEGMENT_GAP_DEGREES = 2;

function polarToCartesian(cx: number, cy: number, radius: number, angleInDegrees: number) {
  const radians = (angleInDegrees - 90) * (Math.PI / 180);
  return {
    x: cx + radius * Math.cos(radians),
    y: cy + radius * Math.sin(radians),
  };
}

function createDonutSegmentPath(startAngle: number, endAngle: number) {
  const outerStart = polarToCartesian(WHEEL_CENTER, WHEEL_CENTER, SEGMENT_OUTER_RADIUS, startAngle);
  const outerEnd = polarToCartesian(WHEEL_CENTER, WHEEL_CENTER, SEGMENT_OUTER_RADIUS, endAngle);
  const innerEnd = polarToCartesian(WHEEL_CENTER, WHEEL_CENTER, SEGMENT_INNER_RADIUS, endAngle);
  const innerStart = polarToCartesian(WHEEL_CENTER, WHEEL_CENTER, SEGMENT_INNER_RADIUS, startAngle);
  const largeArcFlag = endAngle - startAngle > 180 ? 1 : 0;

  return [
    `M ${outerStart.x} ${outerStart.y}`,
    `A ${SEGMENT_OUTER_RADIUS} ${SEGMENT_OUTER_RADIUS} 0 ${largeArcFlag} 1 ${outerEnd.x} ${outerEnd.y}`,
    `L ${innerEnd.x} ${innerEnd.y}`,
    `A ${SEGMENT_INNER_RADIUS} ${SEGMENT_INNER_RADIUS} 0 ${largeArcFlag} 0 ${innerStart.x} ${innerStart.y}`,
    "Z",
  ].join(" ");
}

function ImportProgressWheel({ progress }: { progress: ImportProgress }) {
  const completedItems = progress.imported + progress.skipped + progress.failed;
  const percentage = Math.min(
    100,
    Math.round((completedItems / Math.max(1, progress.effectiveTotal)) * 100)
  );
  const activeSegments = Math.round((percentage / 100) * PROGRESS_SEGMENT_COUNT);

  return (
    <div className="w-full rounded-2xl border border-[var(--theme-card-border)] bg-[var(--theme-body-bg-start)]/60 p-3">
      <div className="mx-auto flex w-full max-w-[18rem] items-center justify-center gap-4 sm:max-w-none">
        <div
          className="relative h-36 w-36 shrink-0"
          role="progressbar"
          aria-label="Discogs import progress"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={percentage}
          data-testid="discogs-import-progress-wheel"
        >
          <div className="absolute inset-0 rounded-full bg-[radial-gradient(circle_at_center,var(--theme-card-bg)_0%,transparent_68%)] opacity-90" />

          <svg
            className="absolute inset-0 h-full w-full"
            viewBox={`0 0 ${WHEEL_SIZE} ${WHEEL_SIZE}`}
            aria-hidden="true"
          >
            {Array.from({ length: PROGRESS_SEGMENT_COUNT }).map((_, index) => {
              const segmentSweep = 360 / PROGRESS_SEGMENT_COUNT;
              const startAngle = index * segmentSweep + SEGMENT_GAP_DEGREES / 2;
              const endAngle = (index + 1) * segmentSweep - SEGMENT_GAP_DEGREES / 2;
              const isActive = index < activeSegments;
              const activeRatio = activeSegments <= 1
                ? 1
                : index / Math.max(1, activeSegments - 1);
              const darkness = Math.round(12 + activeRatio * 34);

              return (
                <path
                  key={index}
                  data-testid="discogs-import-progress-segment"
                  data-active={isActive ? "true" : "false"}
                  d={createDonutSegmentPath(startAngle, endAngle)}
                  style={{
                    fill: isActive
                      ? `color-mix(in srgb, var(--theme-accent) ${100 - darkness}%, black ${darkness}%)`
                      : "color-mix(in srgb, var(--theme-card-border) 70%, transparent 30%)",
                    opacity: isActive ? 1 : 0.28,
                    filter: isActive
                      ? "drop-shadow(0 0 6px color-mix(in srgb, var(--theme-accent) 45%, transparent 55%))"
                      : "none",
                    transition: "fill 280ms ease, opacity 280ms ease",
                  }}
                />
              );
            })}
          </svg>

          <div className="absolute inset-[18px] rounded-full border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] shadow-[0_16px_40px_rgba(0,0,0,0.18)]" />

          <div className="absolute inset-0 flex flex-col items-center justify-center text-center">
            <div className="text-3xl font-black leading-none tracking-tight text-[var(--theme-card-text)]">
              {percentage}%
            </div>
            <div className="mt-0 text-[9px] font-semibold text-[var(--theme-card-text)]/55">
              Progress
            </div>
          </div>
        </div>

        <div className="min-w-0 flex-1 space-y-2">
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div className="flex flex-col rounded-xl border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] px-3 py-1">
              <div className="flex w-full justify-center text-[var(--theme-card-text)]/55">
                <span className="inline-block text-center">Processed</span>
              </div>
              <div className="mt-1 flex w-full justify-center whitespace-nowrap text-base font-semibold text-[var(--theme-card-text)]">
                <span className="inline-block text-center">{completedItems}/{progress.effectiveTotal}</span>
              </div>
            </div>
            <div className="flex flex-col rounded-xl border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] px-3 py-1">
              <div className="flex w-full justify-center text-[var(--theme-card-text)]/55">
                <span className="inline-block text-center">Imported</span>
              </div>
              <div className="mt-1 flex w-full justify-center text-lg font-semibold text-[var(--theme-accent)]">
                <span className="inline-block text-center">{progress.imported}</span>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3 text-sm">
            <div className="flex flex-col rounded-xl border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] px-3 py-1">
              <div className="flex w-full justify-center text-[var(--theme-card-text)]/55">
                <span className="inline-block text-center">Skipped</span>
              </div>
              <div className="mt-1 flex w-full justify-center text-lg font-semibold text-[var(--theme-card-text)]">
                <span className="inline-block text-center">{progress.skipped}</span>
              </div>
            </div>
            <div className="flex flex-col rounded-xl border border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] px-3 py-1">
              <div className="flex w-full justify-center text-[var(--theme-card-text)]/55">
                <span className="inline-block text-center">Failed</span>
              </div>
              <div className="mt-1 flex w-full justify-center text-lg font-semibold text-amber-400">
                <span className="inline-block text-center">{progress.failed}</span>
              </div>
            </div>
          </div>
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
  duration: string;
}

interface ImportProgress {
  totalReleases: number;
  effectiveTotal: number;
  imported: number;
  skipped: number;
  failed: number;
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
        className="bg-[var(--theme-card-bg)] rounded-lg border border-[var(--theme-card-border)] shadow-xl max-w-md w-full p-4 transform transition-all"
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
                      : "An unknown error occurred"}
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
