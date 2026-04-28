import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import '../styles/DriverProfile.css';

export default function DriverProfile() {
    const [vehicles, setVehicles] = useState([]);
    const [make, setMake] = useState('');
    const [model, setModel] = useState('');
    const [numberPlate, setNumberPlate] = useState('');
    const [loading, setLoading] = useState(true);
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
            setError('Failed to load vehicles.');
        } finally {
            setLoading(false);
        }
    };

    const handleAddVehicle = async () => {
        if (!make.trim() || !model.trim() || !numberPlate.trim()) {
            alert('All fields are required!');
            return;
        }

        try {
            await axios.post('https://localhost:7161/api/driver/profile/vehicle', {
                make,
                model,
                numberPlate
            }, {
                headers: { Authorization: `Bearer ${token}` }
            });

            setMake('');
            setModel('');
            setNumberPlate('');
            fetchVehicles();
        } catch (err) {
            console.error('Error adding vehicle:', err.response?.data);
            alert(err.response?.data?.message || 'Failed to add vehicle.');
        }
    };

    return (
        <div className="driver-profile-wrapper">
            <h2>Driver Vehicles</h2>

            {error && <p className="error-message">{error}</p>}

            <h3>Your Vehicles</h3>
            {loading ? (
                <p>Loading vehicles...</p>
            ) : (
                <ul>
                    {vehicles.length > 0 ? (
                        vehicles.map((v) => (
                            <li key={v.vehicleId}>
                                {`${v.make} ${v.model} - ${v.numberPlate}`}
                            </li>
                        ))
                    ) : (
                        <li><i>No vehicles registered yet.</i></li>
                    )}
                </ul>
            )}

            <h3>Add New Vehicle</h3>
            <input
                className="input-field"
                placeholder="Make"
                value={make}
                onChange={(e) => setMake(e.target.value)}
            />
            <input
                className="input-field"
                placeholder="Model"
                value={model}
                onChange={(e) => setModel(e.target.value)}
            />
            <input
                className="input-field"
                placeholder="Number Plate"
                value={numberPlate}
                onChange={(e) => setNumberPlate(e.target.value)}
            />
            <button className="primary-button" onClick={handleAddVehicle}>Add Vehicle</button>
        </div>
    );
}
