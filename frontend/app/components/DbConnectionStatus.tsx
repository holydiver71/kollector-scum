"use client";

import { useEffect, useState } from "react";

type DatabaseTarget = "local" | "staging" | "production" | "unknown";

type RuntimeInfoResponse = {
  environment?: string;
  databaseTarget?: DatabaseTarget;
};

type HealthResponse = {
  dbStatus?: string;
};

function formatTarget(target: DatabaseTarget): string {
  switch (target) {
    case "local":
      return "Local";
    case "staging":
      return "Staging";
    case "production":
      return "Prod";
    default:
      return "Unknown";
  }
}

/**
 * Displays the database environment label (e.g. "Prod") alongside a live
 * health indicator dot sourced from the API health endpoint.
 */
export default function DbConnectionStatus() {
  const [target, setTarget] = useState<DatabaseTarget>("unknown");
  const [dbHealthy, setDbHealthy] = useState<boolean | null>(null);

  useEffect(() => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;
    if (!baseUrl) {
      setTarget("unknown");
      return;
    }

    const controller = new AbortController();
    const api = baseUrl.replace(/\/$/, "");

    const load = async () => {
      try {
        // Fetch target environment and DB health in parallel
        const [infoRes, healthRes] = await Promise.all([
          fetch(`${api}/runtime-info`, { signal: controller.signal, cache: "no-store" }),
          fetch(`${api}/api/health`,   { signal: controller.signal, cache: "no-store" }),
        ]);

        if (infoRes.ok) {
          const data = (await infoRes.json()) as RuntimeInfoResponse;
          setTarget(data.databaseTarget ?? "unknown");
        }

        if (healthRes.ok) {
          const health = (await healthRes.json()) as HealthResponse;
          setDbHealthy(health.dbStatus === "Healthy");
        } else {
          setDbHealthy(false);
        }
      } catch {
        setTarget("unknown");
        setDbHealthy(false);
      }
    };

    load();

    return () => controller.abort();
  }, []);

  const dotClass =
    dbHealthy === null
      ? "bg-gray-400"
      : dbHealthy
      ? "bg-green-500"
      : "bg-red-500";

  return (
    <span
      className="flex items-center gap-1.5 text-xs text-gray-500"
      data-testid="db-connection-status"
      title={dbHealthy === null ? "Checking DB…" : dbHealthy ? "DB Online" : "DB Offline"}
    >
      <span className={`w-2.5 h-2.5 rounded-full ${dotClass}`} />
      DB: {formatTarget(target)}
    </span>
  );
}
