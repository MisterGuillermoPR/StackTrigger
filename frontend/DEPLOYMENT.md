# Frontend deployment setup

The public site is a guided-pilot landing page. The /onboarding route is informational only; production Slack webhooks are not accepted by the browser.

## Pilot request email

The Vercel function at /api/pilot sends submitted requests through Resend. Configure these Vercel project environment variables for Production and any preview environments you intentionally use:

- RESEND_API_KEY: Resend API key with permission to send email.
- PILOT_INBOX: inbox that should receive pilot requests.
- PILOT_FROM_EMAIL: sender address on a domain verified in Resend.
- SITE_ORIGIN: required exact site origin, such as https://www.stacktrigger.com. Requests without that Origin are rejected.

Deploy the project with frontend as the Vercel root directory so the api function and vercel.json are discovered. Submit a real request after configuration and confirm delivery and reply-to behavior before sending traffic. Without these settings, the form intentionally returns an error instead of claiming a request was received.

Do not put Slack webhook URLs or email-provider secrets in frontend code or VITE_ variables. Slack connection should be implemented only through a server-side authenticated onboarding flow. Rotate any webhook URL that has been shared publicly.


Before paid traffic, configure a Vercel Firewall rate limit for POST /api/pilot and verify the form from the canonical www origin. The function uses an origin check and a honeypot, but does not provide distributed rate limiting by itself.
