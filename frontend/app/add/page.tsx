"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import AddReleaseForm from "../components/AddReleaseForm";

export default function AddReleasePage() {
  const router = useRouter();
  const [showSuccess, setShowSuccess] = useState(false);
  const [newReleaseId, setNewReleaseId] = useState<number | null>(null);

  const handleSuccess = (releaseId: number) => {
    setNewReleaseId(releaseId);
    setShowSuccess(true);
    // Auto-redirect after 2 seconds
    setTimeout(() => {
      router.push(`/releases/${releaseId}`);
    }, 2000);
  };

  const handleCancel = () => {
    router.push("/collection");
  };

  if (showSuccess && newReleaseId) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="bg-green-50 border border-green-200 rounded-lg p-8 text-center">
          <svg
            className="mx-auto h-16 w-16 text-green-500 mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
          <h2 className="text-2xl font-semibold text-gray-900 mb-2">
            Release Created Successfully!
          </h2>
          <p className="text-gray-600 mb-4">
            Redirecting to release details...
          </p>
          <button
            onClick={() => router.push(`/releases/${newReleaseId}`)}
            className="text-blue-600 hover:text-blue-800 underline"
          >
            View Now
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Add Release</h1>
        <p className="mt-2 text-gray-600">
          Add a new music release to your collection
        </p>
      </div>

      <AddReleaseForm onSuccess={handleSuccess} onCancel={handleCancel} />
    </div>
  );
}
