import type { Metadata } from "next";
import React, { Suspense } from 'react';
import { Inter } from "next/font/google";
import "./globals.css";
import Header from "./components/Header";
import Sidebar from "./components/Sidebar";
import Footer from "./components/Footer";
import { ErrorBoundary } from "./components/ErrorBoundary";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "KOLLECTOR SKÜM - Music Collection Manager",
  description: "Manage and organize your music collection with comprehensive tracking and search capabilities.",
  keywords: ["music", "collection", "vinyl", "CD", "discography", "catalog"],
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${inter.variable} font-sans antialiased bg-gray-50`}>
        <ErrorBoundary>
          <div className="min-h-screen flex">
            <Sidebar />
            <div
                className="flex-1 flex flex-col transition-all duration-300 overflow-y-auto app-scroll-container"
                // padding top equals header height so content starts underneath
                // margin-left driven by --sidebar-offset which is set by Sidebar
                style={{ paddingTop: 'var(--app-header-height)', marginLeft: 'var(--sidebar-offset, 64px)' }}
            >
              <Suspense fallback={<div />}>
                <Header />
              </Suspense>
              <main className="flex-1">
                <Suspense fallback={<div />}>
                  {children}
                </Suspense>
              </main>
              <Footer />
            </div>
          </div>
        </ErrorBoundary>
      </body>
    </html>
  );
}