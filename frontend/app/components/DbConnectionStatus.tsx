"use client";

import { useDbHealth } from "./useDbHealth";
import type { DatabaseTarget } from "./useDbHealth";

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
  const { target, dbHealthy } = useDbHealth();

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
