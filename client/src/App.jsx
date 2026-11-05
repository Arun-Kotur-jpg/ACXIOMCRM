import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';

import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Customers from './pages/Customers';
import Leads from './pages/Leads';
import Opportunities from './pages/Opportunities';
import FollowUps from './pages/FollowUps';
import Activities from './pages/Activities';
import Reports from './pages/Reports';
import Users from './pages/Users';
import AuditLogs from './pages/AuditLogs';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />

          {/* Protected CRM Application Routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/customers" element={<Customers />} />
              <Route path="/leads" element={<Leads />} />
              <Route path="/opportunities" element={<Opportunities />} />
              <Route path="/followups" element={<FollowUps />} />
              <Route path="/activities" element={<Activities />} />
              <Route path="/reports" element={<Reports />} />

              {/* Admin-only Routes */}
              <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
                <Route path="/users" element={<Users />} />
                <Route path="/audit" element={<AuditLogs />} />
              </Route>
            </Route>
          </Route>

          {/* Catch-all */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

