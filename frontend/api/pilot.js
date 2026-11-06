export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');

  if (req.method !== 'POST') {
    res.setHeader('Allow', 'POST');
    return res.status(405).json({ error: 'Method not allowed.' });
  }

  const origin = req.headers.origin;
  const allowedOrigin = process.env.SITE_ORIGIN;
  if (!allowedOrigin) {
    return res.status(503).json({ error: 'Pilot requests are not configured yet.' });
  }
  if (origin !== allowedOrigin) {
    return res.status(403).json({ error: 'Request origin is not allowed.' });
  }

  const body = typeof req.body === 'string' ? safeParse(req.body) : req.body;
  if (!body || typeof body !== 'object' || Array.isArray(body)) {
    return res.status(400).json({ error: 'Invalid request.' });
  }

  if (typeof body.website === 'string' && body.website.trim()) {
    return res.status(200).json({ ok: true });
  }

  const name = clean(body.name, 100);
  const email = clean(body.email, 254);
  const company = clean(body.company, 120);
  const topics = clean(body.topics, 300);
  if (!name || !company || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return res.status(400).json({ error: 'Enter your name, company, and a valid email.' });
  }

  const apiKey = process.env.RESEND_API_KEY;
  const inbox = process.env.PILOT_INBOX;
  const from = process.env.PILOT_FROM_EMAIL;
  if (!apiKey || !inbox || !from) {
    return res.status(503).json({ error: 'Pilot requests are not configured yet.' });
  }

  try {
    const emailResponse = await fetch('https://api.resend.com/emails', {
      method: 'POST',
      headers: { Authorization: 'Bearer ' + apiKey, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        from,
        to: [inbox],
        reply_to: email,
        subject: 'StackTrigger pilot request from ' + company,
        text: ['Name: ' + name, 'Email: ' + email, 'Company: ' + company, 'Topics: ' + (topics || 'Not specified')].join('\n')
      })
    });
    if (!emailResponse.ok) {
      console.error('Pilot request email provider returned status', emailResponse.status);
      return res.status(502).json({ error: 'Could not deliver the request.' });
    }
    return res.status(200).json({ ok: true });
  } catch {
    console.error('Pilot request delivery failed.');
    return res.status(502).json({ error: 'Could not deliver the request.' });
  }
}

function clean(value, maxLength) {
  if (typeof value !== 'string') return '';
  return value.trim().replace(/[\u0000-\u001f\u007f]/g, ' ').slice(0, maxLength);
}

function safeParse(value) {
  try { return JSON.parse(value); } catch { return null; }
}
