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
    <div className="min-h-screen bg-[#090D16] text-slate-100 selection:bg-blue-600 selection:text-white font-sans antialiased">
      {/* Header */}
      <header className="border-b border-slate-800/80 bg-[#0B0F19]/90 backdrop-blur-md sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-6 h-18 flex items-center justify-between">
          <div className="flex items-center gap-4 cursor-pointer" onClick={() => setView('landing')}>
            <Logo size="md" />
          </div>

          <div className="flex items-center gap-4 sm:gap-6">
            <span className="hidden md:inline-flex items-center gap-2 text-xs font-mono text-slate-400 bg-slate-900 border border-slate-800 px-3 py-1 rounded">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
              1,037 Enterprise Postings Parsed
            </span>

            <button
              onClick={() => setView('admin')}
              className="text-xs font-mono font-medium text-slate-400 hover:text-white border border-slate-800 hover:border-slate-700 bg-slate-900 px-3 py-1.5 rounded transition-colors"
            >
              Founder Console
            </button>

            <button
              onClick={() => setView('onboarding')}
              className="text-xs sm:text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 px-4 py-2 rounded transition-colors shadow-sm"
            >
              Setup Slack Feed
            </button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main className="max-w-5xl mx-auto px-6 pt-20 pb-20 text-center">
        {/* Top Badge */}
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded border border-slate-800 bg-[#0F172A] text-slate-300 text-xs font-mono mb-8">
          <span className="w-1.5 h-1.5 rounded-full bg-blue-500" />
          <span>Real-Time Corporate ATS Data Pipeline</span>
        </div>

        {/* Hero Title */}
        <h1 className="text-4xl sm:text-6xl font-bold tracking-tight text-white mb-6 leading-[1.15]">
          B2B Intent Signals for <br />
          <span className="text-blue-500">Cloud &amp; Security Consultancies</span>
        </h1>

        <p className="text-base sm:text-lg text-slate-300 max-w-3xl mx-auto leading-relaxed mb-10">
          We monitor corporate career boards across Greenhouse and Lever in real-time. Urgent cloud migrations,
          SOC2 deadlines, and infrastructure transformations are pushed directly into your sales team's Slack.
        </p>

        {/* CTA Buttons */}
        <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-20">
          <a
            href={STRIPE_CHECKOUT_URL}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-2.5 px-7 py-3.5 rounded text-sm font-semibold text-white bg-blue-600 hover:bg-blue-500 transition-colors shadow-md"
          >
            <span>Subscribe to Intent Stream &mdash; $400/mo</span>
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </a>

          <button
            onClick={() => setView('onboarding')}
            className="w-full sm:w-auto inline-flex items-center justify-center px-6 py-3.5 rounded text-sm font-medium text-slate-300 hover:text-white bg-[#0F172A] border border-slate-800 hover:border-slate-700 transition-colors"
          >
            Configure Webhook Preferences &rarr;
          </button>
        </div>

        {/* Slack Interactive Simulation - Clean Corporate Style */}
        <div className="text-left max-w-2xl mx-auto bg-[#0F172A] border border-slate-800 rounded-lg p-6 shadow-xl relative overflow-hidden mb-20">
          {/* Channel Header */}
          <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
            <div className="flex items-center gap-2.5">
              <span className="text-slate-400 font-bold text-sm">#</span>
              <span className="text-sm font-semibold text-slate-200">signals-cloud-leads</span>
              <span className="text-xs text-slate-500 font-mono">StackTrigger Bot</span>
            </div>
            <span className="text-xs text-emerald-400 bg-emerald-950/80 border border-emerald-800/80 px-2 py-0.5 rounded font-mono">
              Just now
            </span>
          </div>

          {/* Bot Card Details */}
          <div className="space-y-3.5">
            <div className="flex items-center gap-2">
              <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-blue-950 text-blue-300 border border-blue-800">
                INTENT SIGNAL
              </span>
              <span className="text-xs text-slate-400 font-mono">Datadog &bull; Greenhouse ATS</span>
            </div>

            <div>
              <h4 className="text-base font-semibold text-white">
                Datadog is hiring a <span className="text-blue-400">Partner Solutions Architect - Cloud Migration</span>
              </h4>
              <p className="text-xs text-slate-400 mt-1">
                Active hiring for Azure/AWS enterprise migrations indicates active consulting budget and migration timelines.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-3 text-xs bg-[#090D16] p-3 rounded border border-slate-800">
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Decision-Maker</span>
                <span className="text-slate-200 font-medium">VP of Cloud Architecture</span>
              </div>
              <div>
                <span className="text-slate-500 block uppercase font-mono text-[10px]">Ingestion Source</span>
                <span className="text-slate-200 font-medium">Greenhouse Direct REST API</span>
              </div>
            </div>

            <div className="text-xs">
              <span className="font-semibold text-slate-400 block mb-1 font-mono text-[11px]">Matched Tech Stack:</span>
              <div className="flex flex-wrap gap-1">
                {['Azure', 'Cloud Migration', 'Kubernetes', 'Terraform', 'SOC2'].map((tag) => (
                  <span
                    key={tag}
                    className="px-2 py-0.5 rounded bg-[#090D16] border border-slate-800 text-slate-300 font-mono text-[11px]"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>

            <div className="pt-2 border-t border-slate-800 flex items-center justify-between">
              <button
                type="button"
                className="text-xs font-semibold text-blue-400 hover:text-blue-300 flex items-center gap-1.5 transition-colors font-mono"
                onClick={handleCopySample}
              >
                <span>{copied ? "Copied Mock Test Payload!" : "Inspect Sample Block Kit Payload →"}</span>
              </button>
              <span className="text-[11px] text-slate-500 font-mono">Format: Block Kit JSON</span>
            </div>
          </div>
        </div>

        {/* 3 Core Pillars */}
        <div className="grid md:grid-cols-3 gap-6 text-left mb-20">
          <div className="p-6 rounded-lg bg-[#0F172A] border border-slate-800">
            <div className="text-xs font-mono font-bold text-blue-400 mb-3">01 / EXTRACTION</div>
            <h3 className="text-base font-semibold text-white mb-2">Direct REST API Ingestion</h3>
            <p className="text-xs text-slate-400 leading-relaxed">
              Our automated .NET 9 workers query corporate ATS REST endpoints directly. Leads are detected within minutes of publication.
            </p>
          </div>

          <div className="p-6 rounded-lg bg-[#0F172A] border border-slate-800">
            <div className="text-xs font-mono font-bold text-blue-400 mb-3">02 / DELIVERY</div>
            <h3 className="text-base font-semibold text-white mb-2">Slack Block Kit Integration</h3>
            <p className="text-xs text-slate-400 leading-relaxed">
              Signals land directly in your reps' existing workflow with clickable job links, cold pitch icebreakers, and LinkedIn decision-maker links.
            </p>
          </div>

          <div className="p-6 rounded-lg bg-[#0F172A] border border-slate-800">
            <div className="text-xs font-mono font-bold text-blue-400 mb-3">03 / SIMPLICITY</div>
            <h3 className="text-base font-semibold text-white mb-2">Zero Software Bloat</h3>
            <p className="text-xs text-slate-400 leading-relaxed">
              No complex $20k/year enterprise dashboards to log into. A dedicated data stream delivered straight to your Slack channel.
            </p>
          </div>
        </div>

        {/* Technology Matrix */}
        <div className="border border-slate-800 rounded-lg bg-[#0F172A] p-6 text-center mb-16">
          <span className="text-xs uppercase font-mono tracking-wider text-slate-400 block mb-3">
            Supported Technology Triggers
          </span>
          <div className="flex flex-wrap justify-center items-center gap-2">
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
                className="px-2.5 py-1 rounded bg-[#090D16] border border-slate-800 text-xs font-mono text-slate-300"
              >
                {tech}
              </span>
            ))}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800 py-10 text-xs text-slate-500 bg-[#0B0F19]">
        <div className="max-w-7xl mx-auto px-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <Logo size="sm" />

          <div>
            &copy; {new Date().getFullYear()} StackTrigger Inc. (stacktrigger.com). All rights reserved.
          </div>

          <div className="flex gap-5 items-center">
            <button
              onClick={() => setView('admin')}
              className="text-slate-400 hover:text-white transition-colors font-mono"
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
