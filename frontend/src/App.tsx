import React from 'react';
import LandingPage from './components/LandingPage';
import Onboarding from './components/Onboarding';
export const App: React.FC = () => window.location.pathname.replace(/\/$/, '') === '/onboarding' ? <Onboarding /> : <LandingPage />;
export default App;
