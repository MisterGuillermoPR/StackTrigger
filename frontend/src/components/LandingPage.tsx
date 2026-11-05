import React, { useMemo, useState } from 'react';

const leads = [
  { company: 'Example Company A', role: 'Azure Platform Engineer', tags: ['Azure Migration', 'SOC2'] },
  { company: 'Example Company B', role: 'Cloud Security Lead', tags: ['AWS', 'CrowdStrike'] },
  { company: 'Example Company C', role: '.NET Platform Architect', tags: ['.NET 9', 'Kubernetes'] },
];
const csv = [
  'Company,Role,Intent signals,Source,Posted',
  ...Array.from({ length: 10 }, (_, i) => `Example Company ${String.fromCharCode(65 + i)},Cloud/Security role,"Azure; SOC2",https://example.com/sample-${i + 1},Example only`),
].join('\n');
const pilotLink = 'mailto:support@stacktrigger.com?subject=StackTrigger%20pilot%20request&body=Hi%2C%20I%E2%80%99d%20like%20to%20discuss%20a%20StackTrigger%20pilot.%0A%0ACompany%3A%0ATopics%20I%20care%20about%3A';

export const LandingPage: React.FC = () => {
  const [dealValue, setDealValue] = useState(40000);
  const ratio = useMemo(() => Math.round(dealValue / 400), [dealValue]);
  const downloadSample = () => {
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
    const link = document.createElement('a'); link.href = url; link.download = 'stacktrigger-synthetic-sample.csv'; link.click(); URL.revokeObjectURL(url);
  };
  return <div className="min-h-screen bg-[#071018] text-slate-100 selection:bg-cyan-300 selection:text-slate-950">
    <header className="sticky top-0 z-20 border-b border-white/5 bg-[#071018]/85 backdrop-blur-xl"><div className="mx-auto flex h-[76px] max-w-6xl items-center justify-between px-5">
      <a href="/" className="flex items-center gap-3"><span className="grid h-9 w-9 place-items-center rounded-xl bg-cyan-300 font-black text-slate-950">S</span><span className="font-semibold tracking-tight">stack<span className="text-cyan-300">trigger</span></span></a>
      <nav className="hidden items-center gap-8 text-sm text-slate-400 md:flex"><a href="#how" className="hover:text-white">How the pilot works</a><a href="#signals" className="hover:text-white">Sample format</a><a href="#roi" className="hover:text-white">ROI estimate</a></nav>
      <a href={pilotLink} className="rounded-lg bg-white px-4 py-2.5 text-sm font-semibold text-slate-950 hover:bg-cyan-200">Request a pilot</a>
    </div></header>
    <main>
      <section className="relative mx-auto grid max-w-6xl gap-14 px-5 pb-24 pt-20 md:grid-cols-[1.05fr_.95fr] md:items-center md:pt-28">
        <div className="pointer-events-none absolute -left-32 top-10 h-96 w-96 rounded-full bg-cyan-500/10 blur-[120px]" />
        <div className="relative"><div className="mb-7 inline-flex items-center gap-2 rounded-full border border-emerald-300/20 bg-emerald-300/5 px-3 py-1.5 text-xs text-emerald-200"><span className="h-1.5 w-1.5 rounded-full bg-emerald-300"/>Small beta pilot · Limited onboarding</div>
          <h1 className="max-w-2xl text-5xl font-semibold leading-[1.07] tracking-[-.055em] sm:text-7xl">See real hiring signals<br/>before you <span className="text-cyan-300">commit.</span></h1>
          <p className="mt-7 max-w-xl text-lg leading-8 text-slate-400">Review real job postings with us, choose the cloud or security topics that matter to your team, and try Slack delivery during a guided pilot.</p>
          <div className="mt-9 flex flex-col gap-3 sm:flex-row"><a href={pilotLink} className="rounded-xl bg-cyan-300 px-6 py-3.5 text-center font-semibold text-slate-950 hover:bg-cyan-200">Discuss a guided pilot <span className="ml-2">→</span></a><a href="#signals" className="rounded-xl border border-white/10 px-6 py-3.5 text-center font-medium text-white hover:bg-white/5">Explore sample format</a></div>
          <p className="mt-4 text-xs text-slate-500">For cloud MSPs, cybersecurity firms, and engineering partners. Pilot terms are agreed before setup.</p>
        </div>
        <div id="signals" className="relative rounded-2xl border border-white/10 bg-[#0d1923] p-5 shadow-2xl shadow-black/30 sm:p-6">
          <div className="mb-5 flex items-center justify-between border-b border-white/8 pb-4"><div><div className="text-sm font-semibold">Signal preview</div><div className="mt-1 text-xs text-slate-500">Illustrative examples · Not live customer data</div></div><span className="rounded-full bg-slate-300/10 px-2.5 py-1 text-[11px] text-slate-300">SAMPLE</span></div>
          <div className="space-y-3">{leads.map(lead => <article key={lead.company} className="rounded-xl border border-white/7 bg-[#101f2a] p-4"><div className="text-sm font-semibold">{lead.company}</div><div className="mt-1 text-xs text-slate-400">Example role · {lead.role}</div><div className="mt-3 flex flex-wrap gap-1.5">{lead.tags.map(tag => <span key={tag} className="rounded-md border border-cyan-200/10 bg-cyan-200/5 px-2 py-1 text-[10px] text-cyan-100">{tag}</span>)}</div><div className="mt-3 text-[11px] text-slate-500">Example source link (placeholder)</div></article>)}</div>
          <div className="mt-4 rounded-lg border border-dashed border-white/10 px-4 py-3 text-center text-xs text-slate-500">During a pilot, we’ll review source links and Slack delivery together.</div>
        </div>
      </section>
      <section id="how" className="border-y border-white/5 bg-white/[.02]"><div className="mx-auto grid max-w-6xl gap-8 px-5 py-14 md:grid-cols-3">{[['01','Pick a focus','Tell us which cloud or security hiring signals matter to your team.'],['02','Review real examples','We walk through source postings and explain why they match.'],['03','Try Slack delivery','We help set up a test and agree what a useful pilot looks like.']].map(([n,title,desc])=><div key={n} className="flex gap-4"><span className="font-mono text-sm text-cyan-300">{n}</span><div><h3 className="font-semibold">{title}</h3><p className="mt-2 text-sm leading-6 text-slate-400">{desc}</p></div></div>)}</div></section>
      <section id="roi" className="mx-auto grid max-w-6xl gap-10 px-5 py-20 md:grid-cols-2 md:items-center"><div><p className="text-xs font-semibold uppercase tracking-[.2em] text-cyan-300">Simple economics</p><h2 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">Understand the deal economics.</h2><p className="mt-4 max-w-lg leading-7 text-slate-400">Compare one potential closed deal’s value with the planned $400 monthly subscription. This is a comparison, not a forecast.</p></div><div className="rounded-2xl border border-white/10 bg-[#0d1923] p-6 sm:p-8"><label htmlFor="deal-value" className="text-sm text-slate-300">Value of one potential closed deal</label><div className="mt-3 flex items-center rounded-xl border border-white/10 bg-[#071018] px-4"><span className="text-slate-500">$</span><input id="deal-value" type="number" min="0" step="1000" value={dealValue} onChange={e=>setDealValue(Math.max(0,Number(e.target.value)))} className="w-full bg-transparent px-2 py-3 text-xl font-semibold outline-none"/></div><div className="mt-6 flex items-end justify-between border-t border-white/8 pt-5"><div><div className="text-xs text-slate-500">MONTHLY FEE</div><div className="mt-1 text-lg font-semibold">$400</div></div><div className="text-right"><div className="text-xs text-slate-500">DEAL VALUE / MONTHLY FEE</div><div className="mt-1 text-3xl font-semibold text-emerald-300">{ratio}×</div></div></div><p className="mt-4 text-[11px] leading-5 text-slate-500">Illustrative comparison only. It does not account for conversion or delivery results.</p></div></section>
      <section className="mx-auto max-w-6xl px-5 pb-12"><div className="flex flex-col justify-between gap-6 rounded-2xl border border-white/10 bg-gradient-to-r from-[#10202a] to-[#0c1720] p-7 sm:flex-row sm:items-center sm:p-9"><div><h2 className="text-xl font-semibold">Preview the signal format</h2><p className="mt-2 text-sm text-slate-400">Download 10 synthetic example rows. These are not live leads.</p></div><button onClick={downloadSample} className="shrink-0 rounded-lg border border-white/15 px-5 py-3 text-sm font-semibold hover:bg-white/5">Download synthetic sample CSV ↓</button></div></section>
      <section className="mx-auto max-w-6xl px-5 pb-20"><div className="flex flex-col justify-between gap-6 rounded-2xl border border-cyan-200/15 bg-cyan-200/[.04] p-7 sm:flex-row sm:items-center sm:p-9"><div><p className="text-xs font-semibold uppercase tracking-[.2em] text-cyan-200">Start with proof</p><h2 className="mt-3 text-2xl font-semibold">See actual sources before choosing a plan.</h2><p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">We’ll agree on your target topics and what a useful pilot means before setup. No automated checkout or delivery-volume promise at this stage.</p></div><a href={pilotLink} className="shrink-0 rounded-lg bg-cyan-300 px-5 py-3 text-center text-sm font-semibold text-slate-950 hover:bg-cyan-200">Request a guided pilot →</a></div></section>
    </main>
    <footer className="border-t border-white/5 py-8 text-center text-xs text-slate-500">© {new Date().getFullYear()} StackTrigger · Interested in a pilot? Email support@stacktrigger.com.</footer>
  </div>;
};
export default LandingPage;
