"use client";

import { useCallback, useEffect, useState } from "react";
import { fetchJson } from "../lib/api";

export interface ImportJobSubmission {
  jobId: string;
  status: string;
}

export interface ImportResult {
  success: boolean;
  totalReleases: number;
  importedReleases: number;
  skippedReleases: number;
  failedReleases: number;
  errors: string[];
  errorMessage?: string | null;
  duration: string;
}

export interface ImportProgress {
  totalReleases: number;
  effectiveTotal: number;
  imported: number;
  skipped: number;
  failed: number;
  percentage?: number;
  completed: boolean;
  cooldownUntilUtc?: string | null;
}

interface ImportJobStatus extends ImportProgress {
  jobId: string;
  status: string;
  success: boolean;
  errors: string[];
  errorMessage?: string | null;
  duration?: string;
}

export interface UseDiscogsImportStatusResult {
  isImporting: boolean;
  progress: ImportProgress | null;
  result: ImportResult | null;
  error: string | null;
  cooldownSecondsLeft: number | null;
  /** Submit a Discogs import job and poll until it completes */
  startImport: (username: string, personalToken?: string) => Promise<void>;
  /** Reset all state back to initial values (call before re-opening the dialog) */
  reset: () => void;
}

/**
 * Manages the full Discogs import lifecycle: job submission, status polling,
 * and cooldown countdown. Extracted from DiscogsImportDialog to keep the
 * component focused on presentation.
 */
export function useDiscogsImportStatus(): UseDiscogsImportStatusResult {
  const [isImporting, setIsImporting] = useState(false);
  const [progress, setProgress] = useState<ImportProgress | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [cooldownSecondsLeft, setCooldownSecondsLeft] = useState<number | null>(null);

  // Tick down the API rate-limit cooldown every second.
  useEffect(() => {
    if (!progress?.cooldownUntilUtc) {
      setCooldownSecondsLeft(null);
      return;
    }
    const cooldownEnd = new Date(progress.cooldownUntilUtc).getTime();
    const computeSecsLeft = (): number | null => {
      const secsLeft = Math.max(0, Math.round((cooldownEnd - Date.now()) / 1000));
      return secsLeft > 0 ? secsLeft : null;
    };
    setCooldownSecondsLeft(computeSecsLeft());
    const id = window.setInterval(() => setCooldownSecondsLeft(computeSecsLeft()), 1000);
    return () => window.clearInterval(id);
  }, [progress?.cooldownUntilUtc]);

  const startImport = useCallback(async (username: string, personalToken?: string) => {
    setIsImporting(true);
    setError(null);
    setResult(null);
    setProgress(null);

    let pollId: number | undefined;

    /** Normalise both camelCase and PascalCase API response fields */
    const normalizeStatus = (p: unknown): ImportJobStatus | null => {
      if (!p || typeof p !== "object") return null;
      const o = p as Record<string, unknown>;
      return {
        jobId: String(o.jobId ?? o.JobId ?? ""),
        status: String(o.status ?? o.Status ?? "Queued"),
        totalReleases: Number(o.totalReleases ?? o.TotalReleases ?? 0),
        effectiveTotal: Number(o.effectiveTotal ?? o.EffectiveTotal ?? o.totalReleases ?? 0),
        imported: Number(o.imported ?? o.Imported ?? 0),
        skipped: Number(o.skipped ?? o.Skipped ?? 0),
        failed: Number(o.failed ?? o.Failed ?? 0),
        percentage: o.percentage != null ? Number(o.percentage) : o.Percentage != null ? Number(o.Percentage) : undefined,
        completed: Boolean(o.completed ?? o.Completed ?? false),
        success: Boolean(o.success ?? o.Success ?? false),
        errorMessage: (o.errorMessage ?? o.ErrorMessage ?? null) as string | null,
        errors: (o.errors ?? o.Errors ?? []) as string[],
        duration: o.duration != null ? String(o.duration) : o.Duration != null ? String(o.Duration) : "",
        cooldownUntilUtc: (o.cooldownUntilUtc ?? o.CooldownUntilUtc ?? null) as string | null,
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
        setError(
          status.errorMessage ?? status.errors[0] ?? "Failed to import from Discogs. Please try again."
        );
      }
    };

    try {
      const submission = await fetchJson<ImportJobSubmission>("/api/import/discogs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, personalToken: personalToken || undefined }),
        timeoutMs: 30000,
      });

      await new Promise<void>((resolve) => {
        const poll = async () => {
          try {
            const statusResponse = await fetchJson<unknown>(
              `/api/import/discogs/status?jobId=${encodeURIComponent(submission.jobId)}`,
              { method: "GET", swallowErrors: true, timeoutMs: 5000 }
            );

            const status = normalizeStatus(statusResponse);
            if (!status) return;

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
            // Ignore transient polling failures; next tick will retry.
          }
        };

        pollId = window.setInterval(() => { void poll(); }, 1000);
        void poll();
      });
    } catch (err) {
      const e = err as unknown;
      if (err instanceof Error && err.name === "AbortError") {
        setError("Import timed out. Please try again or contact support.");
      } else if (e && typeof e === "object") {
        const errObj = e as Record<string, unknown>;
        const details = errObj.details;
        if (
          details &&
          typeof details === "object" &&
          typeof (details as Record<string, unknown>).error === "string"
        ) {
          setError((details as Record<string, string>).error);
        } else if (typeof errObj.message === "string") {
          setError(errObj.message);
        } else {
          setError("Failed to import from Discogs. Please try again.");
        }
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to import from Discogs. Please try again.");
      }
    } finally {
      if (pollId) window.clearInterval(pollId);
      setIsImporting(false);
    }
  }, []);

  const reset = useCallback(() => {
    setIsImporting(false);
    setProgress(null);
    setResult(null);
    setError(null);
    setCooldownSecondsLeft(null);
  }, []);

  return { isImporting, progress, result, error, cooldownSecondsLeft, startImport, reset };
}
