import React from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/LandingPage.css';

export default function LandingPage() {
    const navigate = useNavigate();

    return (
        <div className="landing-wrapper">
            <div className="landing-card">
                <div className="landing-logo" />
                <h1 className="landing-title">UniRide</h1>
                <p className="landing-tagline">University carpooling made simple</p>
                <div className="landing-actions">
                    <button className="landing-btn-primary" onClick={() => navigate('/login')}>
                        Login
                    </button>
                    <button className="landing-btn-secondary" onClick={() => navigate('/register')}>
                        Register
                    </button>
                </div>
            </div>
        </div>
    );
}
