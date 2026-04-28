import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import '../styles/CreateRidePage.css';

export default function CreateRidePage() {
    const [origin, setOrigin] = useState('');
    const [destination, setDestination] = useState('');
    const [routeStops, setRouteStops] = useState(['']);
    const [departureTime, setDepartureTime] = useState('');
    const [vehicleId, setVehicleId] = useState('');
    const [availableSeats, setAvailableSeats] = useState(1);
    const [pricePerSeat, setPricePerSeat] = useState('');
    const [vehicles, setVehicles] = useState([]);
    const [loadingVehicles, setLoadingVehicles] = useState(true);
    const [error, setError] = useState('');

    const navigate = useNavigate();
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    useEffect(() => {
        if (!token || role !== 'driver') {
            navigate('/');
            return;
        }

        fetchVehicles();
    }, [navigate]);

    const fetchVehicles = async () => {
        try {
            const res = await axios.get('https://localhost:7161/api/driver/profile/vehicles', {
                headers: { Authorization: `Bearer ${token}` }
            });
            setVehicles(res.data || []);
        } catch (err) {
            console.error('Error fetching vehicles:', err);
            setError('Failed to load your vehicles.');
        } finally {
            setLoadingVehicles(false);
        }
    };

    const handleRouteStopChange = (index, value) => {
        const updated = [...routeStops];
        updated[index] = value;
        setRouteStops(updated);
    };

    const addRouteStop = () => setRouteStops([...routeStops, '']);
    const removeRouteStop = (index) => {
        const updated = routeStops.filter((_, i) => i !== index);
        setRouteStops(updated);
    };

    const handleSubmit = async () => {
        setError('');

        if (!origin || !destination || !vehicleId) {
            setError('Please fill in all required fields.');
            return;
        }

        // Validation: One of origin or destination must include "Habib University"
        const universityKeyword = 'habib university';
        const isValid = origin.toLowerCase().includes(universityKeyword) || destination.toLowerCase().includes(universityKeyword);
        if (!isValid) {
            setError('Either the Origin or Destination must include "Habib University".');
            return;
        }

        try {
            const payload = {
                origin,
                destination,
                routeStops: routeStops.filter(stop => stop.trim() !== ''),
                departureTime: new Date(departureTime).toISOString(),
                vehicleId: parseInt(vehicleId),
                availableSeats: parseInt(availableSeats),
                pricePerSeat: parseInt(pricePerSeat)
            };

            await axios.post('https://localhost:7161/api/ridemanagement/create', payload, {
                headers: { Authorization: `Bearer ${token}` }
            });

            alert('Ride created successfully!');
            navigate('/driver-dashboard');
        } catch (err) {
            console.error('Failed to create ride:', err);
            setError(err.response?.data?.message || 'Ride creation failed.');
        }
    };

    return (
        <div className="create-ride-wrapper">
            <h2 className="blue-heading">Create Ride</h2>


            {error && <div className="error-message">{error}</div>}

            <div className="create-ride-form">
                <input placeholder="Origin" value={origin} onChange={(e) => setOrigin(e.target.value)} />
                <input placeholder="Destination" value={destination} onChange={(e) => setDestination(e.target.value)} />

                <label>Route Stops (Optional):</label>
                {routeStops.map((stop, index) => (
                    <div key={index} className="route-stop-group">
                        <input value={stop} onChange={(e) => handleRouteStopChange(index, e.target.value)} placeholder={`Stop ${index + 1}`} />
                        <button type="button" className="add-remove-stop-btn" onClick={() => removeRouteStop(index)}>Remove</button>
                    </div>
                ))}
                <button type="button" className="add-route-stop" onClick={addRouteStop}>Add Stop</button>

                <label>Departure Date & Time:</label>
                <input type="datetime-local" value={departureTime} onChange={(e) => setDepartureTime(e.target.value)} />

                <label>Select Vehicle:</label>
                {loadingVehicles ? (
                    <p>Loading vehicles...</p>
                ) : (
                    <select value={vehicleId} onChange={(e) => setVehicleId(e.target.value)}>
                        <option value="">-- Select Vehicle --</option>
                        {vehicles.map((v) => (
                            <option key={v.vehicleId} value={v.vehicleId}>
                                {`${v.make} ${v.model} - ${v.numberPlate}`}
                            </option>
                        ))}
                    </select>
                )}

                <input type="number" placeholder="Available Seats" value={availableSeats} min={1} max={10} onChange={(e) => setAvailableSeats(e.target.value)} />
                <input type="number" placeholder="Price Per Seat (PKR)" value={pricePerSeat} onChange={(e) => setPricePerSeat(e.target.value)} />

                <button className="create-ride-button" onClick={handleSubmit}>Create Ride</button>
            </div>
        </div>
    );
}
