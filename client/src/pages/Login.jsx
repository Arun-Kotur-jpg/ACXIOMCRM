import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const [emailOrUsername, setEmailOrUsername] = useState('admin@acxiomcrm.local');
  const [password, setPassword] = useState('Admin@123456!');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(emailOrUsername, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.message || 'Invalid login attempt.');
    } finally {
      setLoading(false);
    }
  };

  const quickFill = (email, pass) => {
    setEmailOrUsername(email);
    setPassword(pass);
    setError('');
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light py-5">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-sm-11 col-md-9 col-lg-6 col-xl-5" style={{ maxWidth: '480px' }}>
            <div className="text-center mb-4">
              <div className="d-inline-flex align-items-center gap-2 mb-1">
                <i className="bi bi-pie-chart-fill text-primary fs-2"></i>
                <span className="fs-3 fw-bold text-dark">AcxiomCRM</span>
              </div>
              <p className="text-secondary small mb-0">MERN Stack Customer Relationship Management</p>
            </div>

            {error && (
              <div className="alert alert-danger alert-dismissible fade show small d-flex align-items-center" role="alert">
                <i className="bi bi-exclamation-triangle-fill me-2 fs-6"></i>
                <div>{error}</div>
                <button type="button" className="btn-close" onClick={() => setError('')}></button>
              </div>
            )}

            <div className="card shadow-sm border rounded-3 bg-white">
              <div className="card-body p-4">
                <h2 className="h5 fw-bold mb-3 text-dark text-center">Sign In to Your Account</h2>

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">Email or Username</label>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-person"></i></span>
                      <input
                        type="text"
                        className="form-control"
                        value={emailOrUsername}
                        onChange={(e) => setEmailOrUsername(e.target.value)}
                        placeholder="admin@acxiomcrm.local"
                        required
                        autoFocus
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">Password</label>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-lock"></i></span>
                      <input
                        type="password"
                        className="form-control"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="••••••••"
                        required
                      />
                    </div>
                  </div>

                  <button
                    type="submit"
                    className="btn btn-primary w-100 py-2 fw-semibold"
                    disabled={loading}
                  >
                    {loading ? (
                      <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                    ) : (
                      <i className="bi bi-box-arrow-in-right me-1"></i>
                    )}
                    Sign In
                  </button>
                </form>

                <div className="text-center mt-3 pt-3 border-top">
                  <span className="small text-secondary">New user? </span>
                  <Link to="/register" className="small text-decoration-none fw-semibold">
                    Create an account
                  </Link>
                </div>

                <div className="mt-4 p-3 bg-light rounded border text-muted small text-start">
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="fw-semibold text-dark">
                      <i className="bi bi-key-fill text-primary me-1"></i> Quick Fill Demo Accounts:
                    </span>
                  </div>
                  <div className="d-grid gap-2 mb-2">
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-secondary text-start d-flex justify-content-between align-items-center py-1 px-2"
                      onClick={() => quickFill('admin@acxiomcrm.local', 'Admin@123456!')}
                    >
                      <span><strong>Admin</strong> <span className="text-muted">(Full Access)</span></span>
                      <span className="badge bg-danger-subtle text-danger border">Admin@123456!</span>
                    </button>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-secondary text-start d-flex justify-content-between align-items-center py-1 px-2"
                      onClick={() => quickFill('manager@acxiomcrm.local', 'Demo@123456!')}
                    >
                      <span><strong>Manager</strong> <span className="text-muted">(Team Scope)</span></span>
                      <span className="badge bg-warning-subtle text-warning-emphasis border">Demo@123456!</span>
                    </button>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-secondary text-start d-flex justify-content-between align-items-center py-1 px-2"
                      onClick={() => quickFill('sales1@acxiomcrm.local', 'Demo@123456!')}
                    >
                      <span><strong>Sales Exec</strong> <span className="text-muted">(Own Records)</span></span>
                      <span className="badge bg-info-subtle text-info-emphasis border">Demo@123456!</span>
                    </button>
                  </div>
                  <div className="text-muted text-center" style={{ fontSize: '0.72rem' }}>
                    Click any role above to automatically load credentials
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

