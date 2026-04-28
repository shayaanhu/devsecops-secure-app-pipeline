import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import '../styles/LoginPage.css';

export default function LoginPage() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [role, setRole] = useState('driver');
    const [error, setError] = useState('');
    const navigate = useNavigate();

    const handleLogin = async () => {
        setError('');

        if (!email || !password) {
            setError('Please fill in all fields.');
            return;
        }

        try {
            const res = await axios.post('https://localhost:7161/api/auth/login', {
                universityEmail: email,
                password,
                role
            });

            localStorage.setItem('userId', res.data.userId);
            localStorage.setItem('role', res.data.role);
            localStorage.setItem('token', res.data.token);

            navigate(res.data.role === 'driver' ? '/driver-dashboard' : '/passenger-dashboard');
        } catch (err) {
            console.error('Login error:', err);
            setError(err.response?.data?.message || 'Login failed. Please try again.');
        }
    };

    return (
        <div className="login-wrapper">
       
            <div className="login-screen">
                <div className="login-screen-body">
                    <div className="login-header">
                        <div className="login-app-logo" />
                        <h2>UniRide</h2>
                        <p className="text-gray">Your university carpooling solution</p>
                    </div>

                    {error && <p style={{ color: 'red', textAlign: 'center' }}>{error}</p>}

                    <div className="login-form-group">
                        <label className="login-form-label">University Email</label>
                        <input
                            className="login-input-field"
                            type="email"
                            placeholder="@st.habib.edu.pk"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            style={{ color: '#000' }}
                        />
                    </div>

                    <div className="login-form-group">
                        <label className="login-form-label">Password</label>
                        <input
                            className="login-input-field"
                            type="password"
                            placeholder="Enter your password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            style={{ color: '#000' }}
                        />
                    </div>

                    <div className="login-form-group">
                        <label className="login-form-label">Login as</label>
                        <div className="login-role-container">
                            <button
                                type="button"
                                className={`login-role-button ${role === 'driver' ? 'active' : ''}`}
                                onClick={() => setRole('driver')}
                                style={{ color: role === 'driver' ? '#fff' : '#000' }}
                            >
                                Driver
                            </button>
                            <button
                                type="button"
                                className={`login-role-button ${role === 'passenger' ? 'active' : ''}`}
                                onClick={() => setRole('passenger')}
                                style={{ color: role === 'passenger' ? '#fff' : '#000' }}
                            >
                                Passenger
                            </button>
                        </div>
                    </div>

                    <div className="login-action-buttons">
                        <button className="login-button" onClick={handleLogin}>Login</button>
                        <button className="signup-button" onClick={() => navigate('/register')}>Sign Up</button>
                    </div>

                    <div className="login-forgot-password">
                        <a href="#">Forgot Password?</a>
                    </div>
                </div>
                </div>
                
        </div>
    );
}
