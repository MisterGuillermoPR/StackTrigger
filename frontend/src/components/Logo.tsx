import React from 'react';

interface LogoProps {
  className?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  showWordmark?: boolean;
}

export const Logo: React.FC<LogoProps> = ({
  className = '',
  size = 'md',
  showWordmark = true,
}) => {
  const sizeMap = {
    sm: { icon: 24, text: 'text-base', sub: 'text-[9px]' },
    md: { icon: 32, text: 'text-xl', sub: 'text-[10px]' },
    lg: { icon: 40, text: 'text-2xl', sub: 'text-xs' },
    xl: { icon: 52, text: 'text-3xl', sub: 'text-sm' },
  };

  const { icon, text, sub } = sizeMap[size];

  return (
    <div className={`inline-flex items-center gap-3 select-none ${className}`}>
      {/* Precision Geometric SVG Icon */}
      <div className="relative flex items-center justify-center">
        {/* Ambient Glow */}
        <div
          className="absolute inset-0 rounded-xl bg-gradient-to-tr from-cyan-500/30 to-emerald-400/30 blur-md pointer-events-none"
          style={{ width: icon, height: icon }}
        />
        <svg
          width={icon}
          height={icon}
          viewBox="0 0 48 48"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="relative drop-shadow-md transition-transform hover:scale-105 duration-300"
        >
          <defs>
            <linearGradient id="stGradient1" x1="4" y1="4" x2="44" y2="44" gradientUnits="userSpaceOnUse">
              <stop offset="0%" stopColor="#06b6d4" />
              <stop offset="50%" stopColor="#14b8a6" />
              <stop offset="100%" stopColor="#10b981" />
            </linearGradient>
            <linearGradient id="stGradient2" x1="8" y1="12" x2="40" y2="36" gradientUnits="userSpaceOnUse">
              <stop offset="0%" stopColor="#38bdf8" />
              <stop offset="100%" stopColor="#06b6d4" />
            </linearGradient>
            <linearGradient id="pulseGlow" x1="0" y1="0" x2="48" y2="48" gradientUnits="userSpaceOnUse">
              <stop offset="0%" stopColor="#ffffff" stopOpacity="0.8" />
              <stop offset="100%" stopColor="#38bdf8" stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* Rounded Squircle Backing */}
          <rect
            x="3"
            y="3"
            width="42"
            height="42"
            rx="12"
            fill="#09131d"
            stroke="url(#stGradient1)"
            strokeWidth="1.5"
          />

          {/* Stack Tier 1 (Base Layer) */}
          <path
            d="M12 32L24 37L36 32"
            stroke="url(#stGradient1)"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            opacity="0.5"
          />

          {/* Stack Tier 2 (Middle Layer) */}
          <path
            d="M12 26L24 31L36 26"
            stroke="url(#stGradient1)"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            opacity="0.8"
          />

          {/* Stack Tier 3 (Top Surface) */}
          <path
            d="M24 13L36 19L24 25L12 19L24 13Z"
            fill="url(#stGradient2)"
            fillOpacity="0.25"
            stroke="url(#stGradient1)"
            strokeWidth="2"
            strokeLinejoin="round"
          />

          {/* Dynamic Lightning / Signal Trigger Node */}
          <path
            d="M26 9L18 22H25L22 31L31 18H24L26 9Z"
            fill="url(#stGradient1)"
            stroke="#ffffff"
            strokeWidth="0.75"
            strokeLinejoin="round"
            className="filter drop-shadow-[0_0_6px_rgba(6,182,212,0.8)]"
          />

          {/* Top highlight glint */}
          <circle cx="24" cy="13" r="1.5" fill="#ffffff" />
        </svg>
      </div>

      {/* Modern Wordmark */}
      {showWordmark && (
        <div className="flex flex-col">
          <div className={`font-extrabold tracking-tight text-white flex items-center leading-none ${text}`}>
            <span>Stack</span>
            <span className="bg-gradient-to-r from-cyan-400 via-teal-300 to-emerald-400 bg-clip-text text-transparent">
              Trigger
            </span>
          </div>
          <span className={`text-slate-400 font-mono tracking-wider uppercase mt-1 ${sub}`}>
            Autonomous DaaS Engine
          </span>
        </div>
      )}
    </div>
  );
};

export default Logo;
