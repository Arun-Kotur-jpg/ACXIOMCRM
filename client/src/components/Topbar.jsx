import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../services/api';

export default function Topbar({ onToggleSidebar }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [counts, setCounts] = useState({ overdue: 0, upcoming: 0 });

  useEffect(() => {
    const fetchCounts = async () => {
      try {
        const res = await api.get('/followups?limit=1');
        setCounts({
          overdue: res.data.overdueCount || 0,
          upcoming: res.data.upcomingCount || 0
        });
      } catch (err) {
        // Silently fail if not loaded yet
      }
    };

    fetchCounts();
    const interval = setInterval(fetchCounts, 60000);
    return () => clearInterval(interval);
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const roleClass = user?.role === 'Admin'
    ? 'admin'
    : user?.role === 'Manager'
      ? 'manager'
      : 'sales';

  return (
    <header className="app-topbar">
      <div className="topbar-left">
        <button
          className="btn btn-sm btn-outline-secondary d-lg-none me-2"
          type="button"
          onClick={onToggleSidebar}
          aria-label="Toggle navigation"
        >
          <i className="bi bi-list fs-5"></i>
        </button>
        <span className="text-secondary small fw-medium">
          <i className="bi bi-geo-alt me-1"></i>Asia/Kolkata (IST) &bull; {new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
        </span>
      </div>

      <div className="topbar-right">
        {/* Follow-up notification bell */}
        <div className="dropdown">
          <button
            className="btn btn-outline-secondary btn-sm position-relative"
            type="button"
            data-bs-toggle="dropdown"
            aria-expanded="false"
          >
            <i className="bi bi-bell"></i>
            {(counts.overdue > 0 || counts.upcoming > 0) && (
              <span
                className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
                style={{ fontSize: '0.65rem' }}
              >
                {counts.overdue + counts.upcoming}
              </span>
            )}
          </button>
          <ul className="dropdown-menu dropdown-menu-end shadow-sm" style={{ minWidth: '250px' }}>
            <li className="dropdown-header fw-bold">Follow-Up Notifications</li>
            <li>
              <Link
                className="dropdown-item d-flex justify-content-between align-items-center"
                to="/followups?scope=Overdue"
              >
                <span><i className="bi bi-exclamation-circle text-danger me-2"></i>Overdue Tasks</span>
                <span className="badge bg-danger">{counts.overdue}</span>
              </Link>
            </li>
            <li>
              <Link
                className="dropdown-item d-flex justify-content-between align-items-center"
                to="/followups?scope=Upcoming"
              >
                <span><i className="bi bi-clock text-primary me-2"></i>Upcoming Planned</span>
                <span className="badge bg-primary">{counts.upcoming}</span>
              </Link>
            </li>
            <li><hr className="dropdown-divider" /></li>
            <li>
              <Link className="dropdown-item text-center small text-primary" to="/followups">
                View All Follow-Ups
              </Link>
            </li>
          </ul>
        </div>

        {/* User Profile Pill & Dropdown */}
        <div className="d-flex align-items-center gap-2">
          <span className={`role-badge ${roleClass}`}>
            {user?.role}
          </span>

          <div className="dropdown">
            <button
              className="btn btn-light btn-sm dropdown-toggle fw-semibold text-dark"
              type="button"
              data-bs-toggle="dropdown"
              aria-expanded="false"
            >
              <i className="bi bi-person-circle me-1 text-secondary"></i>
              {user?.fullName || user?.email}
            </button>
            <ul className="dropdown-menu dropdown-menu-end shadow-sm">
              <li className="dropdown-item-text">
                <div className="small fw-bold">{user?.fullName}</div>
                <div className="small text-muted">{user?.email}</div>
              </li>
              <li><hr className="dropdown-divider" /></li>
              <li>
                <button
                  type="button"
                  className="dropdown-item text-danger d-flex align-items-center"
                  onClick={handleLogout}
                >
                  <i className="bi bi-box-arrow-right me-2"></i>Logout
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </header>
  );
}

