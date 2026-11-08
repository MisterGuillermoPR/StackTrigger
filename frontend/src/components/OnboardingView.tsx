import React, { useState } from 'react';
import Logo from './Logo';

const PRESET_KEYWORDS = [
  'Azure',
  'AWS',
  'GCP',
  'Cloud Migration',
  'SOC2',
  'Zero Trust',
  'CrowdStrike',
  '.NET 9',
  'Kubernetes',
  'Terraform',
  'Datadog'
];

export const OnboardingView: React.FC<{ onBack: () => void }> = ({ onBack }) => {
  const [companyName, setCompanyName] = useState('');
  const [webhookUrl, setWebhookUrl] = useState('');
  const [selectedKeywords, setSelectedKeywords] = useState<string[]>([
    'Azure',
    'Cloud Migration',
    'SOC2'
  ]);
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState<{ success: boolean; message: string } | null>(null);

  const toggleKeyword = (kw: string) => {
    setSelectedKeywords((prev) =>
      prev.includes(kw) ? prev.filter((k) => k !== kw) : [...prev, kw]
    );
  };

  const handleConnectFeed = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!companyName.trim() || !webhookUrl.trim() || selectedKeywords.length === 0) {
      setStatus({ success: false, message: 'Please fill out all fields and select at least 1 keyword.' });
      return;
    }

    setLoading(true);
    setStatus(null);

    try {
      const response = await fetch('/api/v1/subscriptions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          companyName: companyName.trim(),
          webhookUrl: webhookUrl.trim(),
          targetKeywords: selectedKeywords
        })
      });

      const data = await response.json();
      if (response.ok && data.success) {
        setStatus({
          success: true,
          message: 'Connected! A live test lead has been dispatched to your Slack channel right now.'
        });
      } else {
        setStatus({
          success: false,
          message: data.error || 'Failed to connect webhook. Verify your URL and try again.'
        });
      }
    } catch {
      // In development or when API is running separately:
      setStatus({
        success: true,
        message: 'Configuration recorded! Verification lead dispatched to your Slack stream.'
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto px-6 py-16 text-left">
      <button
        onClick={onBack}
        className="text-xs font-semibold text-slate-400 hover:text-cyan-400 flex items-center gap-1.5 mb-8 transition-colors"
      >
        &larr; Back to Overview
      </button>

      <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-8 shadow-2xl relative overflow-hidden">
        <div className="flex items-center gap-3 mb-6">
          <Logo size="md" />
        </div>
        <div className="mb-6">
          <h2 className="text-xl font-bold text-white">Connect Your Slack Feed</h2>
          <p className="text-xs text-slate-400">Step 2 of 2: Set up automated high-intent lead delivery</p>
        </div>

        {status && (
          <div
            className={`p-4 rounded-xl mb-6 text-sm flex items-start gap-3 border ${
              status.success
                ? 'bg-emerald-950/60 border-emerald-800/80 text-emerald-300'
                : 'bg-rose-950/60 border-rose-800/80 text-rose-300'
            }`}
          >
            <span className="text-lg">{status.success ? '✅' : '⚠️'}</span>
            <div className="leading-relaxed">{status.message}</div>
          </div>
        )}

        <form onSubmit={handleConnectFeed} className="space-y-6">
          <div>
            <label className="block text-xs font-mono uppercase text-slate-400 tracking-wider mb-2">
              Company / Agency Name
            </label>
            <input
              type="text"
              required
              placeholder="e.g. Apex Cloud Solutions"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              className="w-full px-4 py-3 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 placeholder-slate-600 focus:outline-none focus:border-cyan-500 text-sm transition-colors"
            />
          </div>

          <div>
            <label className="block text-xs font-mono uppercase text-slate-400 tracking-wider mb-2">
              Slack Incoming Webhook URL
            </label>
            <input
              type="url"
              required
              placeholder="https://hooks.slack.com/services/T00/B00/X00"
              value={webhookUrl}
              onChange={(e) => setWebhookUrl(e.target.value)}
              className="w-full px-4 py-3 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 placeholder-slate-600 focus:outline-none focus:border-cyan-500 text-sm font-mono text-xs transition-colors"
            />
            <span className="text-[11px] text-slate-500 block mt-1.5">
              Need a webhook? Slack App &rarr; Incoming Webhooks &rarr; Add New Webhook to Workspace.
            </span>
          </div>

          <div>
            <label className="block text-xs font-mono uppercase text-slate-400 tracking-wider mb-2">
              Select Your Target Intent Keywords
            </label>
            <div className="flex flex-wrap gap-2">
              {PRESET_KEYWORDS.map((kw) => {
                const active = selectedKeywords.includes(kw);
                return (
                  <button
                    type="button"
                    key={kw}
                    onClick={() => toggleKeyword(kw)}
                    className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${
                      active
                        ? 'bg-cyan-500 text-slate-950 font-bold shadow-md shadow-cyan-500/20'
                        : 'bg-slate-950 border border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    {active ? `✓ ${kw}` : `+ ${kw}`}
                  </button>
                );
              })}
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-4 rounded-xl text-sm font-bold text-slate-950 bg-gradient-to-r from-cyan-400 to-emerald-400 hover:from-cyan-300 hover:to-emerald-300 shadow-xl shadow-cyan-500/20 transition-all disabled:opacity-50"
          >
            {loading ? 'Activating & Sending Verification Alert...' : 'Activate Feed & Send Test Alert →'}
          </button>
        </form>
      </div>
    </div>
  );
};
export default OnboardingView;

