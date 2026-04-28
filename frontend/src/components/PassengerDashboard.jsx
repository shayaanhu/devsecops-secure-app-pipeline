import React, { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { ToastContainer, toast } from "react-toastify";
import 'react-toastify/dist/ReactToastify.css';
import "../styles/PassengerDashboard.css";

export default function PassengerDashboard() {
    const [availableRides, setAvailableRides] = useState([]);
    const [searchTerm, setSearchTerm] = useState("");
    const [loading, setLoading] = useState(true);
    const [timestamp, setTimestamp] = useState(""); // ✅ Timestamp state added
    const [selectedRideId, setSelectedRideId] = useState(null);
    const [selectedLocation, setSelectedLocation] = useState("");
    const [customLocation, setCustomLocation] = useState("");
    const [showModal, setShowModal] = useState(false);
    const [isPickupMode, setIsPickupMode] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem("token");
        const role = localStorage.getItem("role");

        if (!token || role !== "passenger") {
            navigate("/");
            return;
        }

        const fetchRides = async () => {
            try {
                const res = await axios.get("https://localhost:7161/api/passengerdashboard/available-rides", {
                    headers: { Authorization: `Bearer ${token}` }
                });
                setAvailableRides(res.data);
                setTimestamp(new Date().toISOString()); // ✅ Set timestamp when rides are fetched
            } catch (err) {
                console.error("Error fetching rides:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchRides();
    }, [navigate]);

    const openModal = (ride) => {
        setSelectedRideId(ride.rideId);
        setSelectedLocation("");
        setCustomLocation("");
        const goingToHabib = ride.destination.toLowerCase().includes("habib university");
        setIsPickupMode(goingToHabib);
        setShowModal(true);
    };

    const handleRequestRide = async () => {
        const final = selectedLocation === "custom" ? customLocation.trim() : selectedLocation;

        if (!final) {
            toast.error("Please select or enter a location.");
            return;
        }

        try {
            const token = localStorage.getItem("token");
            const ride = availableRides.find(r => r.rideId === selectedRideId);

            await axios.post("https://localhost:7161/api/passengerdashboard/request-ride", {
                rideId: selectedRideId,
                pickupLocation: isPickupMode ? final : "To be decided",
                dropoffLocation: isPickupMode ? ride?.destination : final
            }, {
                headers: { Authorization: `Bearer ${token}` }
            });

            toast.success(`Ride request sent with ${isPickupMode ? 'pickup' : 'dropoff'}: ${final}`);
            setTimeout(() => window.location.reload(), 2000);
        } catch (err) {
            console.error("Error requesting ride:", err.response?.data || err.message);
            toast.error(err.response?.data?.message || "Failed to request ride.");
        }
    };

    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("role");
        navigate("/");
    };

    if (loading) return <p>Loading available rides...</p>;

    return (
        <div className="create-ride-wrapper">
            <ToastContainer position="top-center" autoClose={2500} hideProgressBar />
            <div className="dashboard-header">
                <h2 className="blue-heading">Passenger Dashboard</h2>
                <button className="logout-button" onClick={handleLogout}>Logout</button>
            </div>

            {/* ✅ Timestamp UI */}
            <p><strong className="blue-label">Last Updated:</strong> {timestamp ? new Date(timestamp).toLocaleString() : "N/A"}</p>

            <div className="search-bar">
                <input
                    type="text"
                    placeholder="Search origin, destination, or route"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
                <button onClick={() => navigate("/passenger-profile")}>
                    View accepted/previous rides
                </button>
            </div>

            {availableRides.length === 0 ? (
                <p className="no-rides"><i>No rides available right now.</i></p>
            ) : (
                availableRides
                    .filter((ride) =>
                        ride.origin.toLowerCase().includes(searchTerm.toLowerCase()) ||
                        ride.destination.toLowerCase().includes(searchTerm.toLowerCase()) ||
                        (Array.isArray(ride.routeStops) && ride.routeStops.some(stop =>
                            stop.toLowerCase().includes(searchTerm.toLowerCase())
                        ))
                    ).map((ride) => {
                        const formattedTime = new Date(ride.departureTime + 'Z').toLocaleString();
                        return (
                            <div key={ride.rideId} className="ride-card-v2">
                                <div className="ride-header">
                                    <div className="driver-name">{ride.driverName}</div>
                                    <span className="driver-badge">Driver</span>
                                </div>

                                <div className="ride-path">
                                    <div className="path-label"><strong>Route:</strong></div>
                                    <div className="route-visual">
                                        <div className="stop-node">
                                            <div className="dot start-dot"></div>
                                            <div className="stop-label">{ride.origin}</div>
                                        </div>
                                        {ride.routeStops.map((stop, idx) => (
                                            <React.Fragment key={idx}>
                                                <div className="line"></div>
                                                <div className="stop-node">
                                                    <div className="dot stop-dot"></div>
                                                    <div className="stop-label">{stop}</div>
                                                </div>
                                            </React.Fragment>
                                        ))}
                                        <div className="line"></div>
                                        <div className="stop-node">
                                            <div className="dot end-dot"></div>
                                            <div className="stop-label">{ride.destination}</div>
                                        </div>
                                    </div>
                                </div>

                                <div className="ride-details">
                                    <p><strong>Departure:</strong> {formattedTime}</p>
                                    <p><strong>Vehicle:</strong> {ride.vehicle}</p>
                                    <p><strong>Available Seats:</strong> {ride.availableSeats}</p>
                                    <p><strong>Price per Seat:</strong> Rs. {ride.pricePerSeat}</p>
                                    <p><strong>Your Request Status:</strong> {ride.rideRequestStatus}</p>
                                </div>

                                {ride.availableSeats === 0 ? (
                                    <span className="no-rides-message">Ride is Full</span>
                                ) : ride.rideRequestStatus === "Not Requested" ? (
                                    <button className="request-button" onClick={() => openModal(ride)}>Request Ride</button>
                                ) : null}

                                {showModal && selectedRideId === ride.rideId && (
                                    <div className="form-group">
                                        <label>Select {isPickupMode ? "Pickup" : "Dropoff"} Location:</label>
                                        <select
                                            value={selectedLocation}
                                            onChange={(e) => {
                                                setSelectedLocation(e.target.value);
                                                if (e.target.value !== "custom") setCustomLocation("");
                                            }}
                                        >
                                            <option value="">-- Select --</option>
                                            {(isPickupMode
                                                ? [ride.origin, ...ride.routeStops]
                                                : [...ride.routeStops, ride.destination]
                                            ).map((stop, idx) => (
                                                <option key={idx} value={stop}>{stop}</option>
                                            ))}
                                            <option value="custom">Other (type manually)</option>
                                        </select>

                                        {selectedLocation === "custom" && (
                                            <input
                                                type="text"
                                                placeholder={`Enter custom ${isPickupMode ? "pickup" : "dropoff"} location`}
                                                value={customLocation}
                                                onChange={(e) => setCustomLocation(e.target.value)}
                                            />
                                        )}

                                        <button className="request-button" onClick={handleRequestRide}>
                                            Confirm Ride Request
                                        </button>
                                    </div>
                                )}
                            </div>
                        );
                    })
            )}
        </div>
    );
}
