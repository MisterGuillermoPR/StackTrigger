import React, { useState } from 'react';
import OnboardingView from './OnboardingView';

export const LandingPage: React.FC = () => {
  const [view, setView] = useState<'landing' | 'onboarding'>('landing');
  const [copied, setCopied] = useState(false);

  // When live, replace with your actual Stripe Checkout Payment Link
  const STRIPE_CHECKOUT_URL = "https://buy.stripe.com/test_placeholder_stacktrigger";

  const handleCopySample = () => {
    navigator.clipboard.writeText(`curl -X POST https://api.stacktrigger.com/v1/webhook/test`);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (view === 'onboarding') {
    return <OnboardingView onBack={() => setView('landing')} />;
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-cyan-500 selection:text-slate-950 font-sans antialiased">
      {/* Background Gradient Glow */}
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full max-w-7xl h-96 bg-gradient-to-b from-cyan-500/10 via-emerald-500/5 to-transparent blur-3xl pointer-events-none" />

      {/* Navigation */}
      <header className="relative z-10 border-b border-slate-800/80 backdrop-blur-md sticky top-0">
        <div className="max-w-7xl mx-auto px-6 h-20 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 to-emerald-400 flex items-center justify-center font-black text-slate-950 text-xl tracking-tighter shadow-lg shadow-cyan-500/20">
              ST
            </div>
            <span className="font-extrabold text-xl tracking-tight text-white">
              Stack<span className="text-cyan-400">Trigger</span>
              <span className="text-xs text-slate-500 font-mono ml-2">.com</span>
            </span>
          </div>

          <div className="flex items-center gap-6">
            <span className="hidden md:inline-flex items-center gap-2 text-xs font-mono uppercase tracking-wider text-emerald-400 bg-emerald-950/40 border border-emerald-800/50 px-3 py-1.5 rounded-full">
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
              Live Engine Active
            </span>
            <button
              onClick={() => setView('onboarding')}
              className="text-sm font-semibold text-white bg-slate-800 hover:bg-slate-700 border border-slate-700 px-4 py-2 rounded-lg transition-colors"
            >
              Configure Feed / Setup
            </button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main className="relative z-10 max-w-5xl mx-auto px-6 pt-24 pb-20 text-center">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full border border-cyan-500/30 bg-cyan-950/30 text-cyan-400 text-xs font-medium uppercase tracking-widest mb-8">
          Autonomous DaaS Engine
        </div>

        <h1 className="text-5xl sm:text-7xl font-extrabold tracking-tight text-white mb-8">
          B2B Intent Data for <br />
          <span className="bg-gradient-to-r from-cyan-400 via-teal-300 to-emerald-400 bg-clip-text text-transparent">
            Cloud &amp; SecOps
          </span>
        </h1>

        <p className="text-lg sm:text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed mb-12">
          We scrape corporate ATS platforms and deliver urgent cloud migration and cybersecurity
          signals directly to your Slack. Connect with companies right before they buy.
        </p>

        {/* CTA Buttons */}
        <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-20">
          <a
            href={STRIPE_CHECKOUT_URL}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-3 px-8 py-4 rounded-xl text-base font-bold text-slate-950 bg-gradient-to-r from-cyan-400 to-emerald-400 hover:from-cyan-300 hover:to-emerald-300 shadow-xl shadow-cyan-500/25 transition-all transform hover:-translate-y-0.5 active:translate-y-0"
          >
            <span>Get Access to the Feed &mdash; $400/mo</span>
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </a>

          <button
            onClick={() => setView('onboarding')}
            className="w-full sm:w-auto inline-flex items-center justify-center px-6 py-4 rounded-xl text-sm font-semibold text-slate-300 hover:text-white bg-slate-900 border border-slate-800 hover:border-slate-700 transition-colors"
          >
            Preview Slack Setup Flow &rarr;
          </button>
        </div>

        {/* Live Slack Lead Interactive Preview */}
        <div className="text-left max-w-2xl mx-auto bg-slate-900/90 border border-slate-800 rounded-2xl p-6 shadow-2xl relative overflow-hidden mb-24">
          <div className="flex items-center justify-between border-b border-slate-800 pb-4 mb-4">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-sm">
                #
              </div>
              <div>
                <span className="text-sm font-semibold text-slate-200">#signals-cloud-leads</span>
                <span className="text-xs text-slate-500 ml-2 font-mono">Slack Webhook Stream</span>
              </div>
            </div>
            <span className="text-xs text-emerald-400 bg-emerald-950/60 border border-emerald-900 px-2 py-0.5 rounded">
              Just now
            </span>
          </div>

          <div className="space-y-3 font-sans">
            <div className="flex items-center gap-2 text-rose-400 font-bold text-sm">
              <span>🚨</span>
              <span>StackTrigger High-Intent Lead: CyberDyne Health</span>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs text-slate-400 bg-slate-950/60 p-3 rounded-lg border border-slate-800/80">
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Target Role</span>
                <span className="text-slate-200 font-medium">VP of Cloud Architecture</span>
              </div>
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Company Scale</span>
                <span className="text-slate-200 font-medium">Series B (180 employees)</span>
              </div>
            </div>

            <div className="text-xs text-slate-300">
              <span className="font-semibold text-slate-400 block mb-1">Detected Intent Triggers:</span>
              <div className="flex flex-wrap gap-1.5">
                {['Azure Migration', 'SOC2 Compliance', '.NET 9', 'Zero Trust'].map((tag) => (
                  <span
                    key={tag}
                    className="px-2 py-0.5 rounded bg-cyan-950/80 border border-cyan-800/60 text-cyan-300 font-mono text-[11px]"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>

            <div className="pt-2">
              <button
                type="button"
                className="text-xs font-semibold text-cyan-400 hover:text-cyan-300 flex items-center gap-1.5 transition-colors"
                onClick={handleCopySample}
              >
                <span>{copied ? "Copied Mock Webhook URL!" : "View Raw ATS Source Posting →"}</span>
              </button>
            </div>
          </div>
        </div>

        {/* 3 Core Features */}
        <div className="grid md:grid-cols-3 gap-8 text-left">
          <div className="p-8 rounded-2xl bg-slate-900/40 border border-slate-800/80 hover:border-cyan-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-cyan-500/10 text-cyan-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-cyan-500/20">
              01
            </div>
            <h3 className="text-xl font-bold text-white mb-3">Zero-Latency Extraction</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              Our automated .NET 9 workers monitor career boards in real-time. Signals hit your channel minutes after jobs post—before competitors even know they exist.
            </p>
          </div>

          <div className="p-8 rounded-2xl bg-slate-900/40 border border-slate-800/80 hover:border-cyan-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-emerald-500/20">
              02
            </div>
            <h3 className="text-xl font-bold text-white mb-3">Native Slack/Teams Integration</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              Leads are pushed directly into your reps' existing workflow via high-fidelity Block Kit payloads with clickable ATS source URLs.
            </p>
          </div>

          <div className="p-8 rounded-2xl bg-slate-900/40 border border-slate-800/80 hover:border-cyan-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-purple-500/10 text-purple-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-purple-500/20">
              03
            </div>
            <h3 className="text-xl font-bold text-white mb-3">No Bloated Dashboards</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              No complex $20k/year enterprise logins to manage. Pure data stream delivered straight to your team with zero operational friction.
            </p>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-900 py-12 text-center text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div>
            &copy; {new Date().getFullYear()} StackTrigger Inc. (stacktrigger.com). All rights reserved.
          </div>
          <div className="flex gap-6">
            <a href="#privacy" className="hover:text-slate-400 transition-colors">Privacy</a>
            <a href="#terms" className="hover:text-slate-400 transition-colors">Terms of Service</a>
            <a href="mailto:support@stacktrigger.com" className="hover:text-slate-400 transition-colors">Support</a>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
