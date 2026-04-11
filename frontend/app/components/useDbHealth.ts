"use client";

import { useEffect, useState } from "react";
import { fetchJson } from "../lib/api";

export type DatabaseTarget = "local" | "staging" | "production" | "unknown";

type RuntimeInfoResponse = {
  environment?: string;
  databaseTarget?: DatabaseTarget;
};

type HealthResponse = {
  dbStatus?: string;
};

export interface UseDbHealthResult {
  /** Current database environment target */
  target: DatabaseTarget;
  /** null while loading; true when DB is healthy; false when unhealthy */
  dbHealthy: boolean | null;
}

/**
 * Fetches the database environment target and health status in parallel.
 * Uses the centralised fetchJson wrapper to ensure auth headers are consistent.
 * Both requests swallow errors so the status widget degrades gracefully.
 */
export function useDbHealth(): UseDbHealthResult {
  const [target, setTarget] = useState<DatabaseTarget>("unknown");
  const [dbHealthy, setDbHealthy] = useState<boolean | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const [info, health] = await Promise.all([
          fetchJson<RuntimeInfoResponse | null>("/runtime-info", { swallowErrors: true }) as Promise<RuntimeInfoResponse | null>,
          fetchJson<HealthResponse | null>("/health", { swallowErrors: true }) as Promise<HealthResponse | null>,
        ]);

        if (cancelled) return;

        if (info) {
          setTarget(info.databaseTarget ?? "unknown");
        }

        // health is null when the request failed or returned non-OK
        setDbHealthy(health?.dbStatus === "Healthy" ? true : health !== null ? false : false);
      } catch {
        if (!cancelled) {
          setTarget("unknown");
          setDbHealthy(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  return { target, dbHealthy };
}
