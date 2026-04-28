import React, { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import "../styles/DriverDashboardcss.css";

export default function DriverDashboard() {
    const [timestamp, setTimestamp] = useState("");
    const [ridesWithRequests, setRidesWithRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem("token");
        const role = localStorage.getItem("role");

        if (!token || role !== "driver") {
            navigate("/");
            return;
        }

        const fetchData = async () => {
            try {
                const res = await axios.get("https://localhost:7161/api/driver/dashboard/rides-with-requests", {
                    headers: { Authorization: `Bearer ${token}` },
                });

                const rides = Array.isArray(res.data.result) ? res.data.result : [];
                const fetchedTimestamp = res.data.timestamp || new Date().toISOString();

                setRidesWithRequests(rides);
                setTimestamp(fetchedTimestamp);
                setLoading(false);
            } catch (error) {
                console.error("Error loading dashboard:", error);
                setLoading(false);
            }
        };

        fetchData();
    }, [navigate]);

    const handleRideRequest = async (requestId, action) => {
        const token = localStorage.getItem("token");
        try {
            await axios.post(`https://localhost:7161/api/riderequest/${action}/${requestId}`, {}, {
                headers: { Authorization: `Bearer ${token}` },
            });

            setRidesWithRequests(prevRides =>
                prevRides.map(ride => {
                    const match = ride.requests.find(req => req.requestId === requestId);
                    if (!match) return ride;

                    const updatedRequests = ride.requests.filter(req => req.requestId !== requestId);

                    if (action === "accept") {
                        return {
                            ...ride,
                            requests: updatedRequests,
                            acceptedPassengers: [...(ride.acceptedPassengers || []), match]
                        };
                    } else {
                        return {
                            ...ride,
                            requests: updatedRequests
                        };
                    }
                })
            );
        } catch (err) {
            console.error(`Error ${action}ing request:`, err.response?.data || err.message);
        }
    };

    if (loading) return <p>Loading dashboard...</p>;
    if (!Array.isArray(ridesWithRequests)) return <p>Error loading rides. Please refresh.</p>;

    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("role");
        navigate("/");
    };


    return (
        <div className="driver-dashboard-wrapper">
            <div className="driver-dashboard-container">
                <div className="dashboard-header">
                    <h2>Driver Dashboard</h2>
                    <button className="logout-button" onClick={handleLogout}>Logout</button>
                </div>

               

                <p><strong className="blue-label">Last Updated:</strong> {timestamp ? new Date(timestamp).toLocaleString() : "N/A"}</p>


                <h3>Your Rides & Requests</h3>

                {ridesWithRequests.length === 0 ? (
                    <p><i>No rides offered yet.</i></p>
                ) : (
                    ridesWithRequests.map((ride) => {
                        const goingToHabib = ride.destination.toLowerCase().includes("habib university");

                        return (
                            <div key={ride.rideId} className="ride-card">
                                <h4>Ride ID: {ride.rideId}</h4>
                                <div className="route-path-box">
                                    <div><strong>Route:</strong></div>
                                    <div className="route-visual">
                                        {/* Origin */}
                                        <div className="stop-node">
                                            <div className="dot start-dot"></div>
                                            <div className="stop-label">{ride.origin}</div>
                                        </div>

                                        {/* Route Stops */}
                                        {ride.routeStops?.map((stop, idx) => (
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

                                <div className="ride-info">
                                    <p><strong>Departure:</strong> {new Date(ride.departureTime + 'Z').toLocaleString()}</p>
                                    <p><strong>Vehicle:</strong> {ride.vehicle}</p>
                                    <p><strong>Initial Seats Available:</strong> {ride.availableSeats}</p>
                                    <p><strong>Price per Seat:</strong> Rs. {ride.pricePerSeat}</p>
                                </div>


                                <button
                                    className="message-button"
                                    onClick={() => navigate(`/chat/${ride.rideId}`)}
                                >
                                    Go to Messages
                                </button>

                                <h5 style={{ marginTop: "20px" }}>Incoming Requests</h5>
                                {ride.requests?.length > 0 ? (
                                    ride.requests.map((req) => (
                                        <div key={req.requestId} className="passenger-card">
                                            <div className="passenger-info">
                                                <div className="passenger-icon"></div>
                                                <div className="passenger-details">
                                                    <strong>{req.passengerName}</strong>
                                                    <span>{goingToHabib ? "Pickup" : "Dropoff"}: {goingToHabib ? req.pickupLocation : req.dropoffLocation}</span>
                                                </div>
                                            </div>
                                            <div className="request-actions">
                                                <button className="accept-button" onClick={() => handleRideRequest(req.requestId, "accept")}>Accept</button>
                                                <button className="reject-button" onClick={() => handleRideRequest(req.requestId, "reject")}>Reject</button>
                                            </div>
                                        </div>
                                    ))

                                ) : (
                                    <p><i>No pending requests.</i></p>
                                )}

                                <h5 style={{ marginTop: "20px" }}>Upcoming Passengers</h5>
                                {ride.acceptedPassengers?.length > 0 ? (
                                    ride.acceptedPassengers.map((passenger, idx) => (
                                        <div key={idx} className="passenger-card">
                                            <div className="passenger-info">
                                                <div className="passenger-icon"></div>
                                                <div className="passenger-details">
                                                    <strong>{passenger.passengerName}</strong>
                                                    <span>{goingToHabib ? "Pickup" : "Dropoff"}: {goingToHabib ? passenger.pickupLocation : passenger.dropoffLocation}</span>
                                                </div>
                                            </div>
                                            {/*<button className="message-button-compact" onClick={() => navigate(`/chat/${ride.rideId}`)}>*/}
                                            {/*    Message*/}
                                            {/*</button>*/}
                                        </div>
                                    ))

                                ) : (
                                    <p><i>No confirmed passengers yet.</i></p>
                                )}
                            </div>
                        );
                    })
                )}

                <div className="dashboard-buttons">
                    <button className="primary-button" onClick={() => navigate("/create-ride")}>
                        Create Ride
                    </button>
                    <button className="secondary-button" onClick={() => navigate("/driver-profile")}>
                        View Profile / Manage Vehicles
                    </button>
                </div>
            </div>
        </div>
    );
}
