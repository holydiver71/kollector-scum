"use client";

import { useState } from "react";
import { requestMagicLink } from "../lib/auth";

/**
 * Form component for initiating passwordless email authentication.
 * Users enter their email address to receive a magic sign-in link.
 */
export function MagicLinkLoginForm() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<"idle" | "loading" | "sent" | "error">("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim()) {
      setErrorMessage("Please enter your email address.");
      return;
    }

    setStatus("loading");
    setErrorMessage(null);

    try {
      await requestMagicLink(email.trim());
      setStatus("sent");
    } catch {
      setStatus("error");
      setErrorMessage("Something went wrong. Please try again.");
    }
  };

  if (status === "sent") {
    return (
      <div className="text-center space-y-3 max-w-sm">
        <div className="text-4xl">📬</div>
        <p className="text-white font-semibold text-base">Check your inbox</p>
        <p className="text-gray-400 text-sm">
          If <span className="text-purple-300">{email}</span> is on our invite list,
          you&apos;ll receive a sign-in link shortly.
        </p>
        <button
          onClick={() => {
            setStatus("idle");
            setEmail("");
          }}
          className="text-sm text-purple-400 hover:text-purple-300 underline"
        >
          Try a different email
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-3">
      <label className="block text-sm text-gray-400 mb-1" htmlFor="magic-link-email">
        Sign in with your email
      </label>
      <div className="flex gap-2">
        <input
          id="magic-link-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="your@email.com"
          required
          autoComplete="email"
          disabled={status === "loading"}
          className="flex-1 bg-[#1C1C28] border border-[#2C2C3A] rounded-lg px-4 py-2 text-white text-sm
                     placeholder-gray-500 focus:outline-none focus:border-purple-500 disabled:opacity-50"
        />
        <button
          type="submit"
          disabled={status === "loading"}
          className="px-4 py-2 bg-[#8B5CF6] hover:bg-[#7C3AED] text-white text-sm font-semibold
                     rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed whitespace-nowrap"
        >
          {status === "loading" ? "Sending…" : "Send Link"}
        </button>
      </div>
      {errorMessage && (
        <p className="text-red-400 text-xs">{errorMessage}</p>
      )}
    </form>
  );
}
