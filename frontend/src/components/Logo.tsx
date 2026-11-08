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
  const sizeConfig = {
    sm: { icon: 20, font: 'text-sm', sub: 'text-[9px]' },
    md: { icon: 26, font: 'text-base', sub: 'text-[10px]' },
    lg: { icon: 34, font: 'text-xl', sub: 'text-[11px]' },
    xl: { icon: 44, font: 'text-2xl', sub: 'text-xs' },
  };

  const { icon, font, sub } = sizeConfig[size];

  return (
    <div className={`inline-flex items-center gap-2.5 select-none ${className}`}>
      {/* Precision Engineered Enterprise Icon */}
      <svg
        width={icon}
        height={icon}
        viewBox="0 0 32 32"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className="flex-shrink-0"
      >
        {/* Dark Slate Base Background */}
        <rect width="32" height="32" rx="7" fill="#0F172A" stroke="#1E293B" strokeWidth="1" />

        {/* Structural Stack Plinth (Enterprise Ingestion Foundation) */}
        <path
          d="M8 20L16 23.5L24 20"
          stroke="#475569"
          strokeWidth="1.75"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M8 16L16 19.5L24 16"
          stroke="#3B82F6"
          strokeWidth="1.75"
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        {/* Precision Trigger Prism (Direct Signal Vector) */}
        <path
          d="M16 8L23 12L16 16L9 12L16 8Z"
          fill="#1E40AF"
          fillOpacity="0.4"
          stroke="#60A5FA"
          strokeWidth="1.75"
          strokeLinejoin="round"
        />

        {/* Direct Focal Signal Beam */}
        <circle cx="16" cy="12" r="1.75" fill="#FFFFFF" />
      </svg>

      {/* Corporate Wordmark */}
      {showWordmark && (
        <div className="flex flex-col leading-tight">
          <div className={`font-semibold tracking-[-0.02em] text-white flex items-center ${font}`}>
            <span>Stack</span>
            <span className="text-blue-500 font-bold ml-0.5">Trigger</span>
          </div>
          <span className={`text-slate-400 font-mono tracking-wider uppercase font-medium ${sub}`}>
            Enterprise Intent Engine
          </span>
        </div>
      )}
    </div>
  );
};

export default Logo;
