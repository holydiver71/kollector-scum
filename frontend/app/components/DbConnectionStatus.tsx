"use client";

import { useEffect, useState } from "react";

type DatabaseTarget = "local" | "staging" | "production" | "unknown";

type RuntimeInfoResponse = {
  environment?: string;
  databaseTarget?: DatabaseTarget;
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

export default function DbConnectionStatus() {
  const [target, setTarget] = useState<DatabaseTarget>("unknown");

  useEffect(() => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;
    if (!baseUrl) {
      setTarget("unknown");
      return;
    }

    const controller = new AbortController();

    const load = async () => {
      try {
        const res = await fetch(`${baseUrl.replace(/\/$/, "")}/runtime-info`, {
          signal: controller.signal,
          cache: "no-store",
        });

        if (!res.ok) {
          setTarget("unknown");
          return;
        }

        const data = (await res.json()) as RuntimeInfoResponse;
        setTarget(data.databaseTarget ?? "unknown");
      } catch {
        setTarget("unknown");
      }
    };

    load();

    return () => controller.abort();
  }, []);

  return (
    <span className="text-xs text-gray-500" data-testid="db-connection-status">
      DB: {formatTarget(target)}
    </span>
  );
}
