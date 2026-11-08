import React, { useState } from 'react';
import OnboardingView from './OnboardingView';
import AdminDashboard from './AdminDashboard';
import Logo from './Logo';

export const LandingPage: React.FC = () => {
  const [view, setView] = useState<'landing' | 'onboarding' | 'admin'>('landing');
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

  if (view === 'admin') {
    return <AdminDashboard onBack={() => setView('landing')} />;
  }

  return (
    <div className="min-h-screen bg-[#060c14] text-slate-100 selection:bg-cyan-500 selection:text-slate-950 font-sans antialiased overflow-x-hidden">
      {/* Background Ambient Mesh Lights */}
      <div className="fixed inset-0 pointer-events-none z-0">
        <div className="absolute -top-40 left-1/2 -translate-x-1/2 w-[800px] h-[450px] bg-gradient-to-b from-cyan-500/15 via-emerald-500/10 to-transparent blur-[120px]" />
        <div className="absolute top-1/3 -left-48 w-96 h-96 bg-cyan-600/10 rounded-full blur-[100px]" />
        <div className="absolute top-2/3 -right-48 w-96 h-96 bg-emerald-600/10 rounded-full blur-[100px]" />
      </div>

      {/* Header */}
      <header className="relative z-20 border-b border-slate-800/80 bg-[#070e17]/80 backdrop-blur-xl sticky top-0">
        <div className="max-w-7xl mx-auto px-6 h-20 flex items-center justify-between">
          <div className="flex items-center gap-4 cursor-pointer" onClick={() => setView('landing')}>
            <Logo size="md" />
          </div>

          <div className="flex items-center gap-4 sm:gap-6">
            <span className="hidden md:inline-flex items-center gap-2 text-xs font-mono uppercase tracking-wider text-emerald-400 bg-emerald-950/40 border border-emerald-800/50 px-3 py-1.5 rounded-full">
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
              1,037 Signals Ingested
            </span>

            <button
              onClick={() => setView('admin')}
              className="text-xs font-mono font-semibold text-slate-400 hover:text-cyan-400 border border-slate-800 hover:border-cyan-800/60 bg-slate-900/60 px-3.5 py-2 rounded-xl transition-all"
            >
              Founder Console
            </button>

            <button
              onClick={() => setView('onboarding')}
              className="text-xs sm:text-sm font-bold text-slate-950 bg-gradient-to-r from-cyan-400 to-emerald-400 hover:from-cyan-300 hover:to-emerald-300 px-4 sm:px-5 py-2 rounded-xl transition-all shadow-md shadow-cyan-500/20"
            >
              Setup Slack Feed
            </button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main className="relative z-10 max-w-5xl mx-auto px-6 pt-20 pb-20 text-center">
        {/* Top Badge */}
        <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full border border-cyan-500/30 bg-cyan-950/40 text-cyan-300 text-xs font-mono font-medium tracking-wide mb-8 shadow-inner">
          <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-ping" />
          <span>Autonomous ATS Job Board Extraction Engine</span>
        </div>

        {/* Hero Title */}
        <h1 className="text-4xl sm:text-6xl md:text-7xl font-extrabold tracking-tight text-white mb-8 leading-[1.1]">
          B2B Intent Data for <br />
          <span className="bg-gradient-to-r from-cyan-400 via-teal-300 to-emerald-400 bg-clip-text text-transparent">
            Cloud &amp; SecOps Agencies
          </span>
        </h1>

        <p className="text-base sm:text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed mb-12">
          We monitor corporate career boards across Greenhouse and Lever in real-time. Urgent cloud migrations,
          SOC2 deadlines, and Kubernetes transformations are routed directly to your reps' Slack channels
          with enriched decision-maker profiles.
        </p>

        {/* CTA Buttons */}
        <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-20">
          <a
            href={STRIPE_CHECKOUT_URL}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-3 px-8 py-4 rounded-xl text-base font-bold text-slate-950 bg-gradient-to-r from-cyan-400 via-teal-300 to-emerald-400 hover:from-cyan-300 hover:to-emerald-300 shadow-xl shadow-cyan-500/25 transition-all transform hover:-translate-y-0.5"
          >
            <span>Subscribe to Stream &mdash; $400/mo</span>
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </a>

          <button
            onClick={() => setView('onboarding')}
            className="w-full sm:w-auto inline-flex items-center justify-center px-6 py-4 rounded-xl text-sm font-semibold text-slate-300 hover:text-white bg-slate-900/80 border border-slate-800 hover:border-slate-700 transition-colors"
          >
            Interactive Feed Setup &rarr;
          </button>
        </div>

        {/* High-Fidelity Slack Interactive Simulation */}
        <div className="text-left max-w-2xl mx-auto bg-[#08121d] border border-cyan-800/40 rounded-2xl p-6 shadow-2xl relative overflow-hidden mb-24">
          <div className="absolute top-0 right-0 w-40 h-40 bg-cyan-500/10 rounded-full blur-2xl pointer-events-none" />

          {/* Slack Channel Header */}
          <div className="flex items-center justify-between border-b border-slate-800 pb-4 mb-4">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-sm">
                #
              </div>
              <div>
                <span className="text-sm font-semibold text-slate-200">#signals-cloud-leads</span>
                <span className="text-xs text-slate-500 ml-2 font-mono">Real-Time Slack Bot</span>
              </div>
            </div>
            <span className="text-xs text-emerald-400 bg-emerald-950/60 border border-emerald-900/60 px-2.5 py-0.5 rounded-full font-mono">
              Just now
            </span>
          </div>

          {/* Bot Message Block */}
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
                INTENT SIGNAL DETECTED
              </span>
              <span className="text-xs text-slate-400 font-mono">Datadog (Greenhouse ATS)</span>
            </div>

            <div>
              <h4 className="text-base font-bold text-white leading-snug">
                Datadog is hiring a <span className="text-cyan-400">Partner Solutions Architect - Cloud Migration</span>
              </h4>
              <p className="text-xs text-slate-400 mt-1 leading-relaxed">
                Active hiring for Azure/AWS enterprise migrations indicates urgent consulting budget and vendor selection.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs text-slate-400 bg-slate-950/70 p-3.5 rounded-xl border border-slate-800/80">
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Inferred Leader</span>
                <span className="text-slate-200 font-medium">VP of Cloud Infrastructure</span>
              </div>
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Extraction Method</span>
                <span className="text-slate-200 font-medium">Greenhouse Direct REST API</span>
              </div>
            </div>

            <div className="text-xs text-slate-300">
              <span className="font-semibold text-slate-400 block mb-1.5 font-mono text-[11px]">Detected Tech Keywords:</span>
              <div className="flex flex-wrap gap-1.5">
                {['Azure', 'Cloud Migration', 'Kubernetes', 'Terraform', 'SOC2'].map((tag) => (
                  <span
                    key={tag}
                    className="px-2 py-0.5 rounded bg-cyan-950/80 border border-cyan-800/60 text-cyan-300 font-mono text-[11px]"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>

            <div className="pt-2 border-t border-slate-800/60 flex items-center justify-between">
              <button
                type="button"
                className="text-xs font-semibold text-cyan-400 hover:text-cyan-300 flex items-center gap-1.5 transition-colors font-mono"
                onClick={handleCopySample}
              >
                <span>{copied ? "Copied Mock Test Payload!" : "Inspect Sample Slack Payload →"}</span>
              </button>
              <span className="text-[11px] text-slate-500 font-mono">Format: Block Kit JSON</span>
            </div>
          </div>
        </div>

        {/* 3 Value Pillars */}
        <div className="grid md:grid-cols-3 gap-8 text-left mb-24">
          <div className="p-8 rounded-2xl bg-[#09131d]/60 border border-slate-800/80 hover:border-cyan-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-cyan-500/10 text-cyan-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-cyan-500/20">
              01
            </div>
            <h3 className="text-xl font-bold text-white mb-3">Zero-Latency REST Ingestion</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              Our automated .NET 9 workers monitor career APIs in real-time. Signals hit your channel minutes after jobs post—before competitors even know they exist.
            </p>
          </div>

          <div className="p-8 rounded-2xl bg-[#09131d]/60 border border-slate-800/80 hover:border-emerald-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-emerald-500/20">
              02
            </div>
            <h3 className="text-xl font-bold text-white mb-3">Native Slack Block Kit</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              Leads are pushed directly into your reps' existing workflow via high-fidelity Block Kit payloads with clickable ATS source links and decision-maker URLs.
            </p>
          </div>

          <div className="p-8 rounded-2xl bg-[#09131d]/60 border border-slate-800/80 hover:border-purple-500/40 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-purple-500/10 text-purple-400 flex items-center justify-center font-mono font-bold text-lg mb-6 border border-purple-500/20">
              03
            </div>
            <h3 className="text-xl font-bold text-white mb-3">Zero Dashboard Fatigue</h3>
            <p className="text-sm text-slate-400 leading-relaxed">
              No complex $20k/year enterprise logins to manage. Pure, high-intent data stream delivered straight to your team with zero operational friction.
            </p>
          </div>
        </div>

        {/* Supported Technology Matrix */}
        <div className="border border-slate-800/80 rounded-2xl bg-[#08121d]/40 p-8 text-center mb-16">
          <span className="text-xs uppercase font-mono tracking-widest text-slate-500 block mb-4">
            Monitoring Enterprise Tech Migrations In Real-Time
          </span>
          <div className="flex flex-wrap justify-center items-center gap-3">
            {[
              'Azure',
              'AWS',
              'Google Cloud (GCP)',
              'Kubernetes',
              'Terraform',
              'SOC2 Compliance',
              'Zero Trust',
              'CrowdStrike',
              '.NET 9',
              'Datadog',
              'FinOps'
            ].map((tech) => (
              <span
                key={tech}
                className="px-3.5 py-1.5 rounded-xl bg-slate-900 border border-slate-800 text-xs font-mono text-slate-300"
              >
                {tech}
              </span>
            ))}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800/80 py-12 text-center text-xs text-slate-500 bg-[#070e17]">
        <div className="max-w-7xl mx-auto px-6 flex flex-col sm:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-3">
            <Logo size="sm" />
          </div>

          <div className="text-slate-500">
            &copy; {new Date().getFullYear()} StackTrigger Inc. (stacktrigger.com). All rights reserved.
          </div>

          <div className="flex gap-6 items-center">
            <button
              onClick={() => setView('admin')}
              className="text-slate-400 hover:text-cyan-400 transition-colors font-mono"
            >
              Founder Console
            </button>
            <a href="#privacy" className="hover:text-slate-400 transition-colors">Privacy</a>
            <a href="#terms" className="hover:text-slate-400 transition-colors">Terms</a>
            <a href="mailto:support@stacktrigger.com" className="hover:text-slate-400 transition-colors">Support</a>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
