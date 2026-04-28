import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import { toast, ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import '../styles/RegisterPage.css';

export default function RegisterPage() {
    const [formData, setFormData] = useState({
        fullName: '',
        email: '',
        phone: '',
        password: '',
        otp: '',
    });
    const [isOtpSent, setIsOtpSent] = useState(false);
    const [isOtpVerified, setIsOtpVerified] = useState(false);
    const [countdown, setCountdown] = useState(180);
    const [canResendOtp, setCanResendOtp] = useState(false);

    const navigate = useNavigate();

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    useEffect(() => {
        let timer;
        if (isOtpSent && countdown > 0) {
            timer = setInterval(() => {
                setCountdown(prev => {
                    if (prev <= 1) {
                        setCanResendOtp(true);
                        clearInterval(timer);
                        return 0;
                    }
                    return prev - 1;
                });
            }, 1000);
        }
        return () => clearInterval(timer);
    }, [isOtpSent, countdown]);

    const isValidUniversityEmail = (email) => {
        return email.toLowerCase().endsWith('@st.habib.edu.pk');
    };

    const handleSendOtp = async () => {
        setCanResendOtp(false);

        if (!formData.email || !formData.email.includes('@') || !isValidUniversityEmail(formData.email)) {
            toast.error('Please use a valid university email (e.g. yourname@st.habib.edu.pk).');
            return;
        }

        try {
            const response = await axios.post('/api/auth/send-otp', {
                universityEmail: formData.email
            });

            if (response.data.success) {
                setIsOtpSent(true);
                setCountdown(120);
                setCanResendOtp(false);
                toast.success('OTP sent successfully!');
            } else {
                toast.error(response.data.message || 'Failed to send OTP.');
            }
        } catch (err) {
            toast.error(err.response?.data?.message || 'Failed to send OTP.');
        }
    };

    const handleVerifyOtp = async () => {
        if (!formData.otp) {
            toast.error('Please enter the OTP.');
            return;
        }

        try {
            const response = await axios.post('/api/auth/verify-otp', {
                universityEmail: formData.email,
                otp: formData.otp
            });

            if (response.data.success) {
                setIsOtpVerified(true);
                toast.success('OTP verified successfully!');
            } else {
                toast.error('Invalid OTP. Please try again.');
            }
        } catch (err) {
            toast.error(err.response?.data?.message || 'OTP verification failed.');
        }
    };

    const handleRegister = async () => {
        if (!isOtpVerified) {
            toast.error('Please verify your OTP first.');
            return;
        }

        if (!isValidUniversityEmail(formData.email)) {
            toast.error('Only university emails (@st.habib.edu.pk) are allowed.');
            return;
        }

        try {
            const response = await axios.post('/api/auth/register', {
                fullName: formData.fullName,
                universityEmail: formData.email,
                phoneNumber: formData.phone,
                password: formData.password
            });

            if (response.data.success) {
                toast.success('Registration successful! Redirecting to login...');
                setTimeout(() => navigate('/'), 2000);
            } else {
                toast.error(response.data.message || 'Registration failed.');
            }
        } catch (err) {
            toast.error(err.response?.data?.message || 'Registration failed. Please try again.');
        }
    };

    const handleResendOtp = () => {
        setFormData(prev => ({ ...prev, otp: '' }));
        handleSendOtp();
    };

    return (
        <div className="register-wrapper">
            <ToastContainer position="top-center" autoClose={2500} hideProgressBar />
            <div className="register-form">
                <h2>Register to UniRide</h2>

                {!isOtpSent ? (
                    <div>
                        <input
                            name="fullName"
                            placeholder="Full Name"
                            value={formData.fullName}
                            onChange={handleChange}
                            required
                        />
                        <div className="form-group">
                            <div className="field-note">
                                Only emails ending in @st.habib.edu.pk are allowed
                          
                            </div>
                            <input
                                name="email"
                                type="email"
                                placeholder="University Email"
                                value={formData.email}
                                onChange={handleChange}
                                required
                            />
                           
                        </div>

                        <input
                            name="phone"
                            placeholder="Phone Number"
                            value={formData.phone}
                            onChange={handleChange}
                            required
                        />
                        <input
                            name="password"
                            type="password"
                            placeholder="Password"
                            value={formData.password}
                            onChange={handleChange}
                            required
                        />
                        <button
                            className="register-button"
                            onClick={handleSendOtp}
                            disabled={!formData.email || !formData.fullName || !formData.phone || !formData.password}
                        >
                            Send OTP
                        </button>
                    </div>
                ) : !isOtpVerified ? (
                    <div>
                        <h4>Enter OTP sent to {formData.email}</h4>
                        <input
                            name="otp"
                            placeholder="6-digit OTP"
                            value={formData.otp}
                            onChange={handleChange}
                            maxLength={6}
                        />
                        <button
                            className="otp-button"
                            onClick={handleVerifyOtp}
                            disabled={!formData.otp}
                        >
                            Verify OTP
                        </button>
                        {canResendOtp ? (
                            <button className="resend-button" onClick={handleResendOtp}>
                                Resend OTP
                            </button>
                        ) : (
                            <div className="otp-timer">Resend OTP in {countdown} seconds</div>
                        )}
                    </div>
                ) : (
                    <div>
                                <h4>OTP Verified Successfully!</h4>
                                <p>Click below to complete registration:</p>
                                <button className="register-button" onClick={handleRegister}>
                                    Complete Registration
                                </button>

                                <p style={{ marginTop: '16px' }}>
                                    Already registered?{' '}
                                    <button
                                        onClick={() => navigate('/login')}
                                        style={{
                                            background: 'none',
                                            border: 'none',
                                            color: '#3498db',
                                            fontWeight: 600,
                                            cursor: 'pointer',
                                            textDecoration: 'underline'
                                        }}
                                    >
                                        Go to Login
                                    </button>
                                </p>

                    </div>
                )}
            </div>
        </div>
    );
}
