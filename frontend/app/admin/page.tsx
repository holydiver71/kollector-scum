"use client";

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { getUserProfile } from '../lib/auth';
import AdminDashboard from '../components/AdminDashboard';

export default function AdminPage() {
  const router = useRouter();
  const [isAdmin, setIsAdmin] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkAdmin = async () => {
      try {
        const profile = await getUserProfile();
        if (!profile || !profile.isAdmin) {
          router.replace('/');
          return;
        }
        setIsAdmin(true);
      } catch (error) {
        console.error('Error checking admin status:', error);
        router.replace('/');
      } finally {
        setLoading(false);
      }
    };

    checkAdmin();
  }, [router]);

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="text-gray-400">Loading...</div>
      </div>
    );
  }

  if (!isAdmin) {
    return null;
  }

  return <AdminDashboard />;
}
