import React from 'react';

export const Onboarding: React.FC = () => (
  <div className="min-h-screen bg-[#071018] text-slate-100">
    <header className="border-b border-white/5">
      <div className="mx-auto flex h-[76px] max-w-3xl items-center justify-between px-5">
        <a href="/" className="font-semibold">stack<span className="text-cyan-300">trigger</span></a>
        <a href="/" className="text-sm text-slate-400 hover:text-white">Back to site</a>
      </div>
    </header>
    <main className="mx-auto max-w-3xl px-5 py-12 sm:py-16">
      <p className="text-xs font-semibold uppercase tracking-[.2em] text-cyan-300">Guided pilot setup</p>
      <h1 className="mt-3 text-3xl font-semibold tracking-tight sm:text-4xl">We’ll set up your feed together.</h1>
      <p className="mt-3 leading-7 text-slate-400">Client setup is currently handled with the StackTrigger team. We’ll confirm your signal topics, review sample source links, and coordinate a Slack test after agreeing on the pilot.</p>
      <div className="mt-8 rounded-2xl border border-white/10 bg-[#0d1923] p-6">
        <h2 className="font-semibold">Keep your Slack webhook private</h2>
        <p className="mt-2 text-sm leading-6 text-slate-400">This page does not collect Slack credentials. Don’t paste a webhook URL into a browser form or email. We’ll provide a secure setup path before connecting a production channel.</p>
      </div>
      <a href="/#contact" className="mt-8 inline-flex rounded-lg bg-cyan-300 px-5 py-3 text-sm font-semibold text-slate-950 hover:bg-cyan-200">Request a guided pilot</a>
    </main>
  </div>
);

export default Onboarding;
