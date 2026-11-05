import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Register() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (password !== confirmPassword) {
      return setError('Passwords do not match.');
    }

    if (password.length < 6) {
      return setError('Password must be at least 6 characters long.');
    }

    setLoading(true);

    try {
      await register(fullName, email, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.message || 'Registration failed.');
    } finally {
      setLoading(false);
    }
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
              <p className="text-secondary small mb-0">Create New Sales Representative Account</p>
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
                <h2 className="h5 fw-bold mb-3 text-dark text-center">Create New Account</h2>

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">Full Name</label>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-person"></i></span>
                      <input
                        type="text"
                        className="form-control"
                        value={fullName}
                        onChange={(e) => setFullName(e.target.value)}
                        placeholder="e.g. John Doe"
                        required
                        autoFocus
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">Work Email</label>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-envelope"></i></span>
                      <input
                        type="email"
                        className="form-control"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        placeholder="john@company.com"
                        required
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
                        placeholder="Min. 6 characters"
                        required
                      />
                    </div>
                  </div>

                  <div className="mb-4">
                    <label className="form-label small fw-semibold text-secondary">Confirm Password</label>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-shield-lock"></i></span>
                      <input
                        type="password"
                        className="form-control"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        placeholder="Re-enter password"
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
                      <i className="bi bi-person-plus me-1"></i>
                    )}
                    Register Account
                  </button>
                </form>

                <div className="text-center mt-3 pt-3 border-top">
                  <span className="small text-secondary">Already have an account? </span>
                  <Link to="/login" className="small text-decoration-none fw-semibold">
                    Sign in
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

