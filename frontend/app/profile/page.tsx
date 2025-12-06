"use client";

export default function ProfilePage() {
  return (
    <div className="min-h-screen bg-gray-50">
      <section className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-12">
          <h1 className="text-4xl font-black text-gray-900 mb-2 flex items-center gap-3">
            <span className="text-3xl">👤</span>
            Profile
          </h1>
          <p className="text-lg text-gray-600 font-medium">
            Manage your profile and preferences
          </p>
        </div>
      </section>

      <main className="max-w-7xl mx-auto px-4 py-12">
        <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm text-center">
          <p className="text-gray-600">Profile page coming soon...</p>
        </div>
      </main>
    </div>
  );
}
