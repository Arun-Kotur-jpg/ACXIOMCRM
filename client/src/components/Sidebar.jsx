import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Sidebar({ isOpen, onClose }) {
  const { user } = useAuth();
  const role = user?.role;

  return (
    <>
      <div
        className={`sidebar-backdrop ${isOpen ? 'show' : ''}`}
        onClick={onClose}
      />
      <aside className={`app-sidebar ${isOpen ? 'show' : ''}`}>
        <NavLink to="/dashboard" className="sidebar-brand">
          <i className="bi bi-pie-chart-fill"></i>
          <span>AcxiomCRM</span>
        </NavLink>

        <ul className="sidebar-nav">
          <li className="sidebar-section-title">Core CRM</li>
          <li>
            <NavLink
              to="/dashboard"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-speedometer2"></i>
              <span>Dashboard</span>
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/customers"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-people-fill"></i>
              <span>Customers</span>
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/leads"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-funnel-fill"></i>
              <span>Leads</span>
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/opportunities"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-briefcase-fill"></i>
              <span>Opportunities</span>
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/followups"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-calendar-event"></i>
              <span>Follow-Ups</span>
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/activities"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-check2-square"></i>
              <span>Activities</span>
            </NavLink>
          </li>

          <li className="sidebar-section-title">Analytics</li>
          <li>
            <NavLink
              to="/reports"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              onClick={onClose}
            >
              <i className="bi bi-file-earmark-bar-graph"></i>
              <span>Reports</span>
            </NavLink>
          </li>

          {role === 'Admin' && (
            <>
              <li className="sidebar-section-title">Administration</li>
              <li>
                <NavLink
                  to="/users"
                  className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
                  onClick={onClose}
                >
                  <i className="bi bi-person-gear"></i>
                  <span>Users & Roles</span>
                </NavLink>
              </li>
              <li>
                <NavLink
                  to="/audit"
                  className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
                  onClick={onClose}
                >
                  <i className="bi bi-shield-check"></i>
                  <span>Audit Logs</span>
                </NavLink>
              </li>
            </>
          )}
        </ul>

        <div className="sidebar-footer">
          <div className="d-flex align-items-center gap-2">
            <i className="bi bi-shield-lock text-info"></i>
            <div>
              <div className="fw-semibold text-white">AcxiomCRM Enterprise</div>
              <div style={{ fontSize: '0.75rem' }}>MERN Stack Edition</div>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
}

