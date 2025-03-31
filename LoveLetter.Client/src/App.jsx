import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from "@microsoft/signalr";
import './App.css';

// --- Configuration ---
// Base URL
const backendBaseUrl = "http://localhost:5000"; 
const lobbyHubUrl = `${backendBaseUrl}/lobbyhub`;
const lobbiesApiUrl = `${backendBaseUrl}/lobbies`; // For initial fetch

function App() {
    // State
    const [lobbies, setLobbies] = useState([]); // Array to hold LobbyDto objects
    const [userName, setUserName] = useState('');
    const [newHobbyName, setNewLobbyName] = useState('');
    const [currentUser, setCurrentUser] = useState(null); // To store { connectionId, userName }
    const [isConnected, setIsConnected] = useState(false);
    const [error, setError] = useState(null); // To display errors

    // Ref for the connection object
    const connectionRef = useRef(null);
    const isMountedRef = useRef(true); // Track if component is mounted for async operations

    // --- SignalR Event Handlers (using useCallback to memoize) ---

    const handleNewLobby = useCallback((lobbyDto) => {
        console.log("New Lobby Received:", lobbyDto);
        setLobbies(prevLobbies => {
            // Avoid adding duplicates if already fetched
            if (prevLobbies.some(l => l.id === lobbyDto.id)) {
                return prevLobbies;
            }
            return [...prevLobbies, lobbyDto];
        });
    }, []); // No dependencies, function is stable

    const handleUserConnected = useCallback((user) => {
        console.log("User Connected (SignalR):", user);
        // Store initial user data (mainly connectionId)
        // Username will likely be default until SetUserName is called
        if (isMountedRef.current) {
            setCurrentUser(prev => ({ ...prev, connectionId: user.connectionId, userName: user.userName || prev?.userName }));
        }
    }, []);

    const handleUserNameSet = useCallback((user) => {
        console.log("Username Set (SignalR):", user);
        if (isMountedRef.current) {
            // If this event is for the current user, update their state
            if (connectionRef.current?.connectionId === user.connectionId) {
                setCurrentUser(user);
            }
            //update lobby host names if applicable
            setLobbies(prevLobbies => prevLobbies.map(lobby =>
                lobby.host?.connectionId === user.connectionId
                    ? { ...lobby, host: { ...lobby.host, userName: user.userName } } // Update host name
                    : lobby.hostName === user.userName 
                        ? { ...lobby, hostName: user.userName }
                        : lobby
            ));
        }
    }, []);

    // --- Effect for Connection Setup & Teardown ---
    useEffect(() => {
        isMountedRef.current = true;

        // Function to fetch initial lobbies
        const fetchInitialLobbies = async () => {
            try {
                // Use relative path if proxy is set up correctly in vite.config.js for '/lobbies'
                // const response = await fetch('/lobbies');
                // Or use full URL:
                const response = await fetch(lobbiesApiUrl);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const data = await response.json();
                if (isMountedRef.current) {
                    console.log("Fetched initial lobbies:", data);
                    setLobbies(data);
                }
            } catch (err) {
                console.error("Failed to fetch initial lobbies:", err);
                if (isMountedRef.current) {
                    setError("Failed to load initial lobbies. Please refresh.");
                }
            }
        };

        // Fetch lobbies when component mounts
        fetchInitialLobbies();

        // Create SignalR connection instance
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(lobbyHubUrl)
            .withAutomaticReconnect([0, 2000, 5000, 10000]) // Example reconnect delays
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connectionRef.current = connection;

        // --- Register SignalR Event Listeners ---
        connection.on("OnNewLobby", handleNewLobby);
        connection.on("OnUserConnected", handleUserConnected);
        connection.on("OnUserNameSet", handleUserNameSet);

       
        connection.on("OnUserJoined", (user, lobbyId) => { 
            console.log(`${user.userName} joined lobby ${lobbyId}`);
           
        });
        connection.on("OnUserLeft", (user, lobbyId) => { 
            console.log(`${user.userName} left lobby ${lobbyId}`);
            
        });
        connection.on("OnUserDisconnected", (user) => {
            console.log("User disconnected:", user);
            // Potentially refresh lobby list or update user counts if needed & possible
        });


        // --- Connection Lifecycle Handlers ---
        connection.onreconnecting(error => {
            console.warn(`Connection lost: "${error}". Reconnecting.`);
            if (isMountedRef.current) setIsConnected(false);
        });

        connection.onreconnected(connectionId => {
            console.log(`Connection reestablished: "${connectionId}".`);
            if (isMountedRef.current) {
                setIsConnected(true);
                setError(null); // Clear previous errors on successful reconnect
                // Re-fetch lobbies after reconnecting to ensure consistency? Optional.
                fetchInitialLobbies();
            }
        });

        connection.onclose(error => {
            console.error(`Connection closed: "${error}". Will not automatically retry beyond configured attempts.`);
            if (isMountedRef.current) {
                setIsConnected(false);
                setError("Connection lost. Please refresh the page.");
                setCurrentUser(null); // Clear user on final close
            }
        });

        // --- Start Connection ---
        const startConnection = async () => {
            try {
                await connection.start();
                console.log("SignalR Connected to LobbyHub.");
                if (isMountedRef.current) {
                    setIsConnected(true);
                    setError(null);
                    // Store connection ID if needed, though OnUserConnected might be better
                    // setCurrentUser(prev => ({ ...prev, connectionId: connection.connectionId }));
                }
            } catch (err) {
                console.error("SignalR Connection Error: ", err);
                if (isMountedRef.current) {
                    setIsConnected(false);
                    setError("Failed to connect to the lobby. Retrying...");
                    // Retry logic is handled by withAutomaticReconnect, but initial failure needs message
                    // setTimeout(startConnection, 5000); // Avoid manual retry loop if using auto reconnect
                }
            }
        };

        startConnection();

        // --- Cleanup Function ---
        return () => {
            isMountedRef.current = false; // Mark as unmounted
            console.log("Stopping SignalR connection (LobbyHub)");
            connectionRef.current?.stop()
                .then(() => console.log("LobbyHub Connection stopped."))
                .catch(err => console.error("Error stopping LobbyHub connection: ", err));
        };
    }, [handleNewLobby, handleUserConnected, handleUserNameSet]); // Add memoized handlers as dependencies

    // --- Client Action Handlers ---

    const handleSetUsernameSubmit = async (event) => {
        event.preventDefault();
        if (!userName.trim() || !connectionRef.current || !isConnected) return;
        setError(null); // Clear previous errors
        try {
            await connectionRef.current.invoke("SetUserName", userName.trim());
            console.log("SetUserName invoked");
            // State update happens via OnUserNameSet listener
        } catch (err) {
            console.error("Error invoking SetUserName:", err);
            setError(`Failed to set username: ${err.message}`);
        }
    };

    const handleCreateLobbySubmit = async (event) => {
        event.preventDefault();
        if (!newHobbyName.trim() || !connectionRef.current || !isConnected) return;
        setError(null);
        try {
            await connectionRef.current.invoke("CreateLobby", newHobbyName.trim());
            console.log("CreateLobby invoked");
            setNewLobbyName(''); // Clear input on successful invocation
            // State update (adding lobby) happens via OnNewLobby listener
        } catch (err) {
            console.error("Error invoking CreateLobby:", err);
            setError(`Failed to create lobby: ${err.message}`);
        }
    };

    const handleJoinLobbyClick = async (lobbyId) => {
        if (!lobbyId || !connectionRef.current || !isConnected) return;
        setError(null);
        console.log(`Attempting to join lobby: ${lobbyId}`);
        try {
            await connectionRef.current.invoke("JoinLobby", lobbyId);
            console.log("JoinLobby invoked for", lobbyId);
            // TODO: Add logic here after joining (e.g., navigate to lobby view, update UI state)
            alert(`Successfully requested to join lobby: ${lobbyId}. Implement next step UI.`);
        } catch (err) {
            console.error(`Error invoking JoinLobby for ${lobbyId}:`, err);
            setError(`Failed to join lobby ${lobbyId}: ${err.message}`);
        }
    };

    // --- Render ---
    return (
        <div className="App">
            <h1>Love Letter Lobby</h1>
            <div className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
                Status: {isConnected ? 'Connected' : 'Disconnected'}
                {currentUser && isConnected && ` | User: ${currentUser.userName} (ID: ${currentUser.connectionId?.substring(0, 6)}...)`}
            </div>

            {error && <div className="error-message">Error: {error}</div>}

            <hr />

            {isConnected && currentUser?.userName && ` | User: ${currentUser.userName} (ID: ${currentUser.connectionId?.substring(0, 6)}...)` && /* Show only if connected and name likely set */ (
                <div>Current User: <strong>{currentUser.userName}</strong></div>
            )}

            {/* Set Username Form */}
            <form onSubmit={handleSetUsernameSubmit}>
                <label htmlFor="userNameInput">Set Your Name:</label>
                <input
                    type="text"
                    id="userNameInput"
                    value={userName}
                    onChange={(e) => setUserName(e.target.value)}
                    placeholder="Enter your username"
                    disabled={!isConnected} // || (currentUser && currentUser.userName !== 'defaultUsername') // Optional: disable after setting?
                    required
                />
                <button type="submit" disabled={!isConnected || !userName.trim()}>
                    Set Name
                </button>
            </form>

            <hr />

            {/* Create Lobby Form */}
            <form onSubmit={handleCreateLobbySubmit}>
                <label htmlFor="lobbyNameInput">Create New Lobby:</label>
                <input
                    type="text"
                    id="lobbyNameInput"
                    value={newHobbyName}
                    onChange={(e) => setNewLobbyName(e.target.value)}
                    placeholder="Enter lobby name"
                    disabled={!isConnected || !currentUser} // Need to be connected and identified
                    required
                />
                <button type="submit" disabled={!isConnected || !newHobbyName.trim() || !currentUser}>
                    Create Lobby
                </button>
            </form>

            <hr />

            {/* Lobby List */}
            <h2>Available Lobbies</h2>
            {lobbies.length === 0 && isConnected && <div>No lobbies available. Create one!</div>}
            {lobbies.length === 0 && !isConnected && <div>Connecting or disconnected...</div>}
            <ul>
                {lobbies.map((lobby) => (
                    <li key={lobby.id}>
                        <span>{lobby.name}</span>
                        {/* Add more details like host or user count if available in LobbyDto */}
                        {/* {lobby.hostName && <span> (Host: {lobby.hostName})</span>} */}
                        {/* {lobby.userCount && <span> ({lobby.userCount} users)</span>} */}
                        <button
                            onClick={() => handleJoinLobbyClick(lobby.id)}
                            disabled={!isConnected || !currentUser} // Need to be connected and identified
                            style={{ marginLeft: '10px' }}
                        >
                            Join
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
}

export default App;