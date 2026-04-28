import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

export default function AdminDashboard() {
    const [stats, setStats] = useState(null);
    const [users, setUsers] = useState([]);
    const [rides, setRides] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const navigate = useNavigate();

    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    useEffect(() => {
        if (!token || role !== 'admin') {
            navigate('/');
            return;
        }
        fetchAll();
    }, [navigate]);

    const fetchAll = async () => {
        try {
            const headers = { Authorization: `Bearer ${token}` };
            const [statsRes, usersRes, ridesRes] = await Promise.all([
                axios.get('/api/admin/stats', { headers }),
                axios.get('/api/admin/users', { headers }),
                axios.get('/api/admin/rides', { headers }),
            ]);
            setStats(statsRes.data);
            setUsers(usersRes.data);
            setRides(ridesRes.data);
        } catch (err) {
            console.error('Admin fetch error:', err);
            setError('Failed to load admin data.');
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteUser = async (userId) => {
        if (!window.confirm('Delete this user? This cannot be undone.')) return;
        try {
            await axios.delete(`/api/admin/users/${userId}`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            setUsers(prev => prev.filter(u => u.userId !== userId));
        } catch (err) {
            alert(err.response?.data?.message || 'Delete failed.');
        }
    };

    const handleCancelRide = async (rideId) => {
        if (!window.confirm('Force-cancel this ride?')) return;
        try {
            await axios.patch(`/api/admin/rides/${rideId}/cancel`, {}, {
                headers: { Authorization: `Bearer ${token}` }
            });
            setRides(prev => prev.map(r =>
                r.rideId === rideId ? { ...r, status: 'Canceled' } : r
            ));
        } catch (err) {
            alert(err.response?.data?.message || 'Cancel failed.');
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('role');
        localStorage.removeItem('userId');
        navigate('/');
    };

    if (loading) return <p style={{ padding: 40 }}>Loading admin panel...</p>;

    return (
        <div style={{ padding: '32px', fontFamily: 'Segoe UI, Arial, sans-serif', color: '#333', maxWidth: 1200, margin: '0 auto' }}>

            {/* Header */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 28 }}>
                <div>
                    <h2 style={{ color: '#3498db', margin: 0 }}>UniRide — Admin Panel</h2>
                    <p style={{ color: '#57606a', margin: '4px 0 0' }}>System Administrator</p>
                </div>
                <button onClick={handleLogout} style={btnStyle('#e74c3c')}>Logout</button>
            </div>

            {error && <p style={{ color: 'red' }}>{error}</p>}

            {/* Stats */}
            {stats && (
                <div style={{ display: 'flex', gap: 16, marginBottom: 36 }}>
                    {[
                        { label: 'Total Users', value: stats.totalUsers },
                        { label: 'Total Rides', value: stats.totalRides },
                        { label: 'Active Rides', value: stats.activeRides },
                    ].map(card => (
                        <div key={card.label} style={cardStyle}>
                            <div style={{ fontSize: 36, fontWeight: 700, color: '#3498db' }}>{card.value}</div>
                            <div style={{ color: '#57606a', marginTop: 6, fontSize: 14 }}>{card.label}</div>
                        </div>
                    ))}
                </div>
            )}

            {/* Users */}
            <h3 style={{ color: '#3498db', marginBottom: 12 }}>Users</h3>
            <div style={{ overflowX: 'auto', marginBottom: 40 }}>
                <table style={tableStyle}>
                    <thead>
                        <tr style={{ background: '#f4f6f8' }}>
                            <th style={thStyle}>ID</th>
                            <th style={thStyle}>Name</th>
                            <th style={thStyle}>Email</th>
                            <th style={thStyle}>Phone</th>
                            <th style={thStyle}>Roles</th>
                            <th style={thStyle}>Joined</th>
                            <th style={thStyle}>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(u => (
                            <tr key={u.userId} style={{ borderBottom: '1px solid #d0d7de' }}>
                                <td style={tdStyle}>{u.userId}</td>
                                <td style={tdStyle}>{u.fullName}</td>
                                <td style={tdStyle}>{u.universityEmail}</td>
                                <td style={tdStyle}>{u.phoneNumber}</td>
                                <td style={tdStyle}>
                                    {[u.isDriver && 'Driver', u.isPassenger && 'Passenger']
                                        .filter(Boolean).join(', ') || '—'}
                                </td>
                                <td style={tdStyle}>{new Date(u.createdAt).toLocaleDateString()}</td>
                                <td style={tdStyle}>
                                    <button
                                        onClick={() => handleDeleteUser(u.userId)}
                                        style={btnStyle('#e74c3c', true)}
                                    >
                                        Delete
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* Rides */}
            <h3 style={{ color: '#3498db', marginBottom: 12 }}>Rides</h3>
            <div style={{ overflowX: 'auto' }}>
                <table style={tableStyle}>
                    <thead>
                        <tr style={{ background: '#f4f6f8' }}>
                            <th style={thStyle}>ID</th>
                            <th style={thStyle}>Origin</th>
                            <th style={thStyle}>Destination</th>
                            <th style={thStyle}>Driver</th>
                            <th style={thStyle}>Vehicle</th>
                            <th style={thStyle}>Departure</th>
                            <th style={thStyle}>Status</th>
                            <th style={thStyle}>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {rides.map(r => (
                            <tr key={r.rideId} style={{ borderBottom: '1px solid #d0d7de' }}>
                                <td style={tdStyle}>{r.rideId}</td>
                                <td style={tdStyle}>{r.origin}</td>
                                <td style={tdStyle}>{r.destination}</td>
                                <td style={tdStyle}>{r.driverName}</td>
                                <td style={tdStyle}>{r.vehicle}</td>
                                <td style={tdStyle}>{new Date(r.departureTime + 'Z').toLocaleString()}</td>
                                <td style={tdStyle}>
                                    <span style={{ color: statusColor(r.status), fontWeight: 600 }}>
                                        {r.status}
                                    </span>
                                </td>
                                <td style={tdStyle}>
                                    {r.status !== 'Canceled' && (
                                        <button
                                            onClick={() => handleCancelRide(r.rideId)}
                                            style={btnStyle('#e67e22', true)}
                                        >
                                            Cancel
                                        </button>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

const statusColor = (status) => {
    if (status === 'Canceled') return '#e74c3c';
    if (status === 'Completed') return '#2ecc71';
    if (status === 'InProgress') return '#f39c12';
    return '#3498db';
};

const btnStyle = (bg, small = false) => ({
    background: bg,
    color: '#fff',
    border: 'none',
    padding: small ? '4px 10px' : '8px 18px',
    borderRadius: 6,
    cursor: 'pointer',
    fontSize: small ? 12 : 14,
    fontWeight: 600,
});

const cardStyle = {
    flex: 1,
    background: '#fff',
    border: '1px solid #d0d7de',
    borderRadius: 10,
    padding: '20px 24px',
    textAlign: 'center',
    boxShadow: '0 2px 6px rgba(0,0,0,0.05)',
};

const tableStyle = {
    width: '100%',
    borderCollapse: 'collapse',
    background: '#fff',
    border: '1px solid #d0d7de',
    borderRadius: 8,
    overflow: 'hidden',
};

const thStyle = {
    padding: '10px 14px',
    fontWeight: 600,
    fontSize: 13,
    borderBottom: '2px solid #d0d7de',
    textAlign: 'left',
    whiteSpace: 'nowrap',
};

const tdStyle = {
    padding: '10px 14px',
    fontSize: 13,
};
