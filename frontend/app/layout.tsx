import type { Metadata } from "next";
import "./globals.css";
import Header from "./components/Header";
import Navigation from "./components/Navigation";
import Footer from "./components/Footer";
import { ErrorBoundary } from "./components/ErrorBoundary";

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
      <body className="font-sans antialiased bg-gray-50">
        <ErrorBoundary>
          <div className="min-h-screen flex flex-col">
            <Header />
            <Navigation />
            <main className="flex-1">
              {children}
            </main>
            <Footer />
          </div>
        </ErrorBoundary>
      </body>
    </html>
  );
}