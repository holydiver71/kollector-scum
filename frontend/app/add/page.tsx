"use client";

export default function AddReleasePage() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Add Release</h1>
        <p className="mt-2 text-gray-600">Add a new music release to your collection</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
        <svg className="mx-auto h-16 w-16 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
        </svg>
        <h2 className="text-xl font-semibold text-gray-900 mb-2">Add New Release</h2>
        <p className="text-gray-600 mb-4">
          Form to add new music releases with all metadata, including cover art, tracks, and purchase information.
        </p>
        <p className="text-sm text-gray-500">
          <strong>Future Enhancement:</strong> CRUD operations for music releases
        </p>
      </div>
    </div>
  );
}
