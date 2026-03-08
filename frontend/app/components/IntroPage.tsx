import React from "react";
import Image from "next/image";
import { LoadingSpinner } from "./LoadingComponents";
import { GoogleSignIn } from "./GoogleSignIn";

export const IntroPage = ({ loading }: { loading?: boolean }) => {
  return (
    <div className="min-h-screen bg-[#0A0A10] flex flex-col items-center justify-center p-6 relative overflow-hidden">
      {/* Background decoration */}
      <div className="absolute top-[-20%] left-[-10%] w-[50%] h-[50%] bg-[#8B5CF6]/10 blur-[120px] rounded-full pointer-events-none" />
      <div className="absolute bottom-[-20%] right-[-10%] w-[50%] h-[50%] bg-[#3B82F6]/10 blur-[120px] rounded-full pointer-events-none" />
      
      <div className="z-10 text-center max-w-4xl mx-auto w-full flex flex-col items-center">
        {/* Logo / Header */}
        <div className="relative w-full max-w-2xl h-32 md:h-64 mb-6">
          <Image 
            src="/images/Kollector-Skum-v2.png" 
            alt="Kollector Sküm Logo" 
            fill
            className="object-contain drop-shadow-[0_0_15px_rgba(139,92,246,0.3)]"
            priority
          />
        </div>
        
        <p className="text-xl md:text-3xl text-gray-300 mb-8 font-light tracking-wide">
          The Ultimate Hub for Your Physical Media.
        </p>
        
        <p className="text-lg md:text-xl text-gray-400 mb-8 max-w-2xl mx-auto leading-relaxed">
          Organize, discover, and track your music collection. Whether you are spinning vinyl, popping in cassettes, or curating CDs—elevate your obsession.
        </p>

        {/* Sign-in CTA */}
        {loading ? (
          <div className="flex flex-col items-center justify-center space-y-4 mb-12">
            <LoadingSpinner />
            <p className="text-gray-400 font-medium">Loading your experience...</p>
          </div>
        ) : (
          <div className="flex justify-center mb-12">
            <GoogleSignIn />
          </div>
        )}

        {/* Feature Highlights Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-left">
          <div className="bg-[#13131F]/80 backdrop-blur border border-[#1C1C28] p-6 rounded-2xl hover:border-[#8B5CF6]/50 transition-colors">
            <div className="text-4xl mb-4">🎵</div>
            <h3 className="text-white font-bold text-xl mb-2">Track Everything</h3>
            <p className="text-gray-400">Add releases manually or integrate with Discogs to effortlessly sync your entire library in seconds.</p>
          </div>
          
          <div className="bg-[#13131F]/80 backdrop-blur border border-[#1C1C28] p-6 rounded-2xl hover:border-[#3B82F6]/50 transition-colors">
            <div className="text-4xl mb-4">📊</div>
            <h3 className="text-white font-bold text-xl mb-2">Deep Insights</h3>
            <p className="text-gray-400">Analyze your collection with beautiful statistics. Discover patterns in genres, formats, and decades.</p>
          </div>
          
          <div className="bg-[#13131F]/80 backdrop-blur border border-[#1C1C28] p-6 rounded-2xl hover:border-pink-500/50 transition-colors">
            <div className="text-4xl mb-4">🪐</div>
            <h3 className="text-white font-bold text-xl mb-2">Dark & Sleek</h3>
            <p className="text-gray-400">Experience a modern, Midnight-themed interface optimized for fast navigation and immersion.</p>
          </div>
        </div>
      </div>
    </div>
  );
};
