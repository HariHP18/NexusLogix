import React, { useState, useEffect } from "react";

interface Driver {
  id?: string;
  firstName: string;
  lastName: string;
  licenseNumber: string;
  status: string;
}

export function App() {
  // Auth states
  const [token, setToken] = useState<string | null>(
    localStorage.getItem("nexus_token")
  );
  const [user, setUser] = useState<{
    username: string;
    tenantId: string;
  } | null>(
    localStorage.getItem("nexus_user")
      ? JSON.parse(localStorage.getItem("nexus_user")!)
      : null
  );

  // Login form states
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loginError, setLoginError] = useState("");

  // Driver form states
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const status = "Active";

  async function fetchDrivers() {
    try {
      const res = await fetch("/api/drivers", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (res.status === 401) {
        handleLogout(); // Token expired or unauthorized
        return;
      }

      if (res.ok) {
        const data = await res.json();
        setDrivers(data);
      }
    } catch (err) {
      console.error("Failed to fetch drivers:", err);
    }
  }

  // Load drivers whenever we have a valid token
  useEffect(() => {
    if (token) {
      fetchDrivers();
    }
  }, [token]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoginError("");

    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (!res.ok) throw new Error("Invalid username or password");

      const data = await res.json();

      // Save session
      localStorage.setItem("nexus_token", data.token);
      localStorage.setItem(
        "nexus_user",
        JSON.stringify({ username: data.username, tenantId: data.tenantId })
      );

      setToken(data.token);
      setUser({ username: data.username, tenantId: data.tenantId });
    } catch (err) {
      if (err instanceof Error) {
        setLoginError(err.message);
      } else {
        setLoginError("An unexpected error occurred.");
      }
    }
  };

  function handleLogout() {
    localStorage.removeItem("nexus_token");
    localStorage.removeItem("nexus_user");
    setToken(null);
    setUser(null);
    setDrivers([]);
  }

  const handleCreateDriver = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await fetch("/api/drivers", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ firstName, lastName, licenseNumber, status }),
      });

      if (res.ok) {
        setFirstName("");
        setLastName("");
        setLicenseNumber("");
        fetchDrivers(); // Reload drivers for active tenant
      }
    } catch (err) {
      console.error("Failed to create driver:", err);
    }
  };

  // 1. Render Login Form if NOT logged in
  if (!token) {
    return (
      <div
        style={{
          maxWidth: "360px",
          margin: "80px auto",
          padding: "24px",
          border: "1px solid #ccc",
          borderRadius: "8px",
          fontFamily: "sans-serif",
        }}
      >
        <h2>NexusLogix Portal</h2>
        <p style={{ fontSize: "14px", color: "#666" }}>
          Sign in to access your tenant roster
        </p>

        {loginError && (
          <p style={{ color: "red", fontSize: "14px" }}>{loginError}</p>
        )}

        <form onSubmit={handleLogin}>
          <div style={{ marginBottom: "12px" }}>
            <label
              style={{ display: "block", fontSize: "14px", fontWeight: "bold" }}
            >
              Username
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="user1 or user2"
              required
              style={{ width: "100%", padding: "8px", boxSizing: "border-box" }}
            />
          </div>

          <div style={{ marginBottom: "16px" }}>
            <label
              style={{ display: "block", fontSize: "14px", fontWeight: "bold" }}
            >
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="user1 or user2"
              required
              style={{ width: "100%", padding: "8px", boxSizing: "border-box" }}
            />
          </div>

          <button
            type="submit"
            style={{
              width: "100%",
              padding: "10px",
              backgroundColor: "#0066cc",
              color: "#fff",
              border: "none",
              borderRadius: "4px",
              cursor: "pointer",
              fontWeight: "bold",
            }}
          >
            Log In
          </button>
        </form>

        <div
          style={{
            marginTop: "16px",
            fontSize: "12px",
            color: "#777",
            background: "#f5f5f5",
            padding: "8px",
            borderRadius: "4px",
          }}
        >
          <strong>Demo Login:</strong>
          <br />
          • user1 / user1 (Tenant01)
          <br />• user2 / user2 (Tenant02)
        </div>
      </div>
    );
  }

  // 2. Render Main Fleet Dashboard if logged in
  return (
    <div
      style={{
        padding: "24px",
        fontFamily: "sans-serif",
        maxWidth: "800px",
        margin: "0 auto",
      }}
    >
      {/* Header with Logged-in Tenant details & Logout button */}
      <header
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexWrap: "wrap",
          gap: "16px",
          borderBottom: "2px solid #eee",
          paddingBottom: "12px",
          marginBottom: "24px",
        }}
      >
        <div style={{ flex: 1, minWidth: "250px" }}>
          <h1 style={{ margin: 0, fontSize: "28px" }}>
            NexusLogix Fleet Manager
          </h1>

          <span
            style={{
              fontSize: "14px",
              color: "#0066cc",
              display: "block",
              marginTop: "8px",
            }}
          >
            Logged in as: <strong>{user?.username}</strong>
            <br />
            Active Sandbox: <strong>{user?.tenantId}</strong>
          </span>
        </div>

        <button
          onClick={handleLogout}
          style={{
            padding: "8px 16px",
            backgroundColor: "#dc3545",
            color: "#fff",
            border: "none",
            borderRadius: "4px",
            cursor: "pointer",
            whiteSpace: "nowrap",
          }}
        >
          Log Out
        </button>
      </header>

      {/* Driver Registration Form */}
      <section
        style={{
          background: "#f9f9f9",
          padding: "16px",
          borderRadius: "6px",
          marginBottom: "24px",
        }}
      >
        <h3>Register New Driver</h3>
        <form
          onSubmit={handleCreateDriver}
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "12px",
          }}
        >
          <input
            type="text"
            placeholder="First Name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            required
            style={{ padding: "8px" }}
          />
          <input
            type="text"
            placeholder="Last Name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            required
            style={{ padding: "8px" }}
          />
          <input
            type="text"
            placeholder="License Number"
            value={licenseNumber}
            onChange={(e) => setLicenseNumber(e.target.value)}
            required
            style={{ padding: "8px" }}
          />
          <button
            type="submit"
            style={{
              padding: "8px",
              backgroundColor: "#28a745",
              color: "#fff",
              border: "none",
              borderRadius: "4px",
              cursor: "pointer",
              fontWeight: "bold",
            }}
          >
            Add Driver
          </button>
        </form>
      </section>

      {/* Driver Roster */}
      <section>
        <h3>Driver Roster ({drivers.length})</h3>
        {drivers.length === 0 ? (
          <p style={{ color: "#777" }}>
            No drivers found for this tenant. Register one above!
          </p>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ background: "#eee", textAlign: "left" }}>
                <th style={{ padding: "8px" }}>First Name</th>
                <th style={{ padding: "8px" }}>Last Name</th>
                <th style={{ padding: "8px" }}>License</th>
                <th style={{ padding: "8px" }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {drivers.map((d, i) => (
                <tr key={d.id || i} style={{ borderBottom: "1px solid #ddd" }}>
                  <td style={{ padding: "8px" }}>{d.firstName}</td>
                  <td style={{ padding: "8px" }}>{d.lastName}</td>
                  <td style={{ padding: "8px" }}>{d.licenseNumber}</td>
                  <td style={{ padding: "8px" }}>{d.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}

export default App;
