import type { Metadata } from "next";
import { Inter, Metal_Mania } from "next/font/google";
import "./globals.css";
import Header from "./components/Header";
import Navigation from "./components/Navigation";
import Footer from "./components/Footer";
import { ErrorBoundary } from "./components/ErrorBoundary";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

const metal = Metal_Mania({
  subsets: ["latin"],
  variable: "--font-metal",
  weight: "400",
});

export const metadata: Metadata = {
  title: "Kollector Sküm - Music Collection Manager",
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
  <body className={`${inter.variable} ${metal.variable} font-sans antialiased bg-gray-50`}>
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
