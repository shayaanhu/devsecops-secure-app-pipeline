import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import '../styles/PassengerProfile.css';


export default function PassengerProfile() {
    const [upcomingRides, setUpcomingRides] = useState([]);
    const [pastRides, setPastRides] = useState([]);
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem('token');
        const role = localStorage.getItem('role');

        if (!token || role !== 'passenger') {
            navigate('/');
            return;
        }

        const fetchAcceptedRides = async () => {
            try {
                const res = await axios.get('/api/passengerprofile/accepted-rides', {
                    headers: { Authorization: `Bearer ${token}` }
                });

                const now = new Date();

                const upcoming = res.data
                    .filter(ride => new Date(ride.departureTime) >= now)
                    .sort((a, b) => new Date(a.departureTime) - new Date(b.departureTime));

                const past = res.data
                    .filter(ride => new Date(ride.departureTime) < now)
                    .sort((a, b) => new Date(b.departureTime) - new Date(a.departureTime));


                setUpcomingRides(upcoming);
                setPastRides(past);
            } catch (err) {
                console.error('Error fetching accepted rides:', err);
            } finally {
                setLoading(false);
            }
        };

        fetchAcceptedRides();
    }, [navigate]);
    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("role");
        navigate("/");
    };


    if (loading) return <p>Loading your profile...</p>;

    const renderRideCard = (ride, index) => (
        <div key={index} className="ride-history-card"
            
        >
            <div className="ride-header">
                <div className="driver-name">{ride.driverName}</div>
                <span className="driver-badge">Driver</span>
            </div>

            <div className="route-path-box">
                <div><strong>Route:</strong></div>
                <div className="route-visual">
                    {/* Origin */}
                    <div className="stop-node">
                        <div className="dot start-dot"></div>
                        <div className="stop-label">{ride.origin}</div>
                    </div>

                    {/* Route Stops */}
                    {ride.routeStops && ride.routeStops.map((stop, idx) => (
                        <React.Fragment key={idx}>
                            <div className="line"></div>
                            <div className="stop-node">
                                <div className="dot stop-dot"></div>
                                <div className="stop-label">{stop}</div>
                            </div>
                        </React.Fragment>
                    ))}

                    {/* Destination */}
                    <div className="line"></div>
                    <div className="stop-node">
                        <div className="dot end-dot"></div>
                        <div className="stop-label">{ride.destination}</div>
                    </div>
                </div>
            </div>

            <p><strong>Departure:</strong> {new Date(ride.departureTime + 'Z').toLocaleString()}</p>
            <p><strong>Vehicle:</strong> {ride.vehicle}</p>
            <p><strong>Driver:</strong> {ride.driverName}</p>
            <p><strong>Your Pickup:</strong> {ride.pickupLocation}</p>
            <p><strong>Your Dropoff:</strong> {ride.dropoffLocation}</p>
            <button
                className="request-button"
                onClick={() => navigate(`/chat/${ride.rideId}`)}
            >
                Go to Messages
            </button>

        </div>
    );
    return (
        <div className="passenger-profile-wrapper">
            <div className="dashboard-header">
                <h2>Passenger Profile</h2>
                <button className="logout-button" onClick={handleLogout}>Logout</button>
            </div>


            <h3 className="section-title">Upcoming Rides</h3>
            {upcomingRides.length === 0 ? (
                <p className="no-history"><i>No upcoming accepted rides.</i></p>
            ) : (
                upcomingRides.map(renderRideCard)
            )}

            <h3 className="section-title">Past Rides</h3>
            {pastRides.length === 0 ? (
                <p className="no-history"><i>No past rides yet.</i></p>
            ) : (
                pastRides.map(renderRideCard)
            )}
        </div>
    );

}
