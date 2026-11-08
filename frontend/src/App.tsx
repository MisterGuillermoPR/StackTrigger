import React, { useState, useEffect } from 'react';
import LandingPage from './components/LandingPage';
import OnboardingView from './components/OnboardingView';
import AdminDashboard from './components/AdminDashboard';

export const App: React.FC = () => {
  const [currentPath, setCurrentPath] = useState(window.location.pathname.replace(/\/$/, ''));

  useEffect(() => {
    const handlePopState = () => {
      setCurrentPath(window.location.pathname.replace(/\/$/, ''));
    };
    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  const navigateTo = (path: string) => {
    window.history.pushState({}, '', path || '/');
    setCurrentPath(path.replace(/\/$/, ''));
  };

  if (currentPath === '/admin') {
    return <AdminDashboard onBack={() => navigateTo('/')} />;
  }

  if (currentPath === '/onboarding') {
    return <OnboardingView onBack={() => navigateTo('/')} />;
  }

  return <LandingPage />;
};

export default App;
