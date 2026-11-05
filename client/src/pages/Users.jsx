import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function Users() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [managers, setManagers] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [roleFilter, setRoleFilter] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  // Modal
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    role: 'Sales Executive',
    managerId: ''
  });
  const [modalError, setModalError] = useState('');

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await api.get('/users', {
        params: { role: roleFilter, search, page, limit: 10 }
      });
      setUsers(res.data.data);
      setManagers(res.data.managers || []);
      setTotal(res.data.total);
      setPages(res.data.pages);
    } catch (err) {
      console.error('Failed to load users:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, [page, roleFilter]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchUsers();
  };

  const openCreateModal = () => {
    setFormData({
      fullName: '',
      email: '',
      password: '',
      role: 'Sales Executive',
      managerId: managers.length > 0 ? managers[0]._id : ''
    });
    setModalError('');
    setShowModal(true);
  };

  const handleCreateSubmit = async (e) => {
    e.preventDefault();
    setModalError('');

    try {
      await api.post('/users', formData);
      setShowModal(false);
      fetchUsers();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to create user.');
    }
  };

  const handleToggleStatus = async (user) => {
    if (user._id === currentUser.id || user._id === currentUser._id) {
      return alert('You cannot deactivate your own account.');
    }

    try {
      await api.patch(`/users/${user._id}/toggle-status`);
      fetchUsers();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update account status.');
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">User & Role Management</h1>
          <p className="page-subtitle">Provision corporate CRM accounts, assign hierarchy roles, and manage access privileges</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <i className="bi bi-person-plus"></i> Add User
        </button>
      </div>

      {/* Filter Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <form className="row g-2 align-items-center" onSubmit={handleSearchSubmit}>
            <div className="col-12 col-md-5">
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-search"></i></span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Search user name or email..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>

            <div className="col-6 col-md-3">
              <select
                className="form-select"
                value={roleFilter}
                onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
              >
                <option value="">All Roles</option>
                <option value="Admin">Admin</option>
                <option value="Manager">Manager</option>
                <option value="Sales Executive">Sales Executive</option>
              </select>
            </div>

            <div className="col-6 col-md-2">
              <button type="submit" className="btn btn-outline-primary w-100">Filter</button>
            </div>

            <div className="col-12 col-md-2 text-md-end text-muted small">
              Total Accounts: <strong>{total}</strong>
            </div>
          </form>
        </div>
      </div>

      {/* Users Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>User Details</th>
                <th>Role</th>
                <th>Reporting Manager</th>
                <th>Status</th>
                <th>Last Login</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="6" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading users...
                  </td>
                </tr>
              ) : users.length > 0 ? (
                users.map(u => (
                  <tr key={u._id}>
                    <td>
                      <div className="fw-semibold text-dark">{u.fullName}</div>
                      <div className="small text-muted">{u.email}</div>
                    </td>
                    <td>
                      <span className={`badge ${
                        u.role === 'Admin' ? 'bg-danger-subtle text-danger border border-danger' :
                        u.role === 'Manager' ? 'bg-warning-subtle text-warning-emphasis border border-warning' :
                        'bg-info-subtle text-info-emphasis border border-info'
                      }`}>
                        {u.role}
                      </span>
                    </td>
                    <td>{u.managerId?.fullName || (u.role === 'Admin' ? 'Top Organization' : 'None')}</td>
                    <td>
                      <span className={`badge ${u.isActive ? 'bg-success' : 'bg-secondary'}`}>
                        {u.isActive ? 'Active' : 'Deactivated'}
                      </span>
                    </td>
                    <td className="small text-muted">
                      {u.lastLoginDate ? new Date(u.lastLoginDate).toLocaleString() : 'Never'}
                    </td>
                    <td className="text-end">
                      <button
                        className={`btn btn-sm ${u.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                        onClick={() => handleToggleStatus(u)}
                        disabled={u._id === currentUser.id || u._id === currentUser._id}
                        title={u.isActive ? 'Deactivate Account' : 'Activate Account'}
                      >
                        <i className={`bi ${u.isActive ? 'bi-lock' : 'bi-unlock'} me-1`}></i>
                        {u.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="6" className="text-center py-4 text-muted">
                    No application users found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {pages > 1 && (
          <div className="d-flex justify-content-between align-items-center p-3 border-top">
            <span className="small text-muted">Page {page} of {pages}</span>
            <div className="btn-group btn-group-sm">
              <button
                className="btn btn-outline-secondary"
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
              >
                Previous
              </button>
              <button
                className="btn btn-outline-secondary"
                disabled={page >= pages}
                onClick={() => setPage(p => Math.min(pages, p + 1))}
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Add User Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">Provision New CRM User</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              <form onSubmit={handleCreateSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="mb-3">
                    <label className="form-label">Full Name *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.fullName}
                      onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                      required
                      placeholder="e.g. Ramesh Kulkarni"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Email Address *</label>
                    <input
                      type="email"
                      className="form-control"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      required
                      placeholder="ramesh@acxiomcrm.local"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Initial Password *</label>
                    <input
                      type="password"
                      className="form-control"
                      value={formData.password}
                      onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                      required
                      placeholder="Min. 6 characters"
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Role</label>
                      <select
                        className="form-select"
                        value={formData.role}
                        onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                      >
                        <option value="Sales Executive">Sales Executive</option>
                        <option value="Manager">Manager</option>
                        <option value="Admin">Admin</option>
                      </select>
                    </div>

                    <div className="col-6">
                      <label className="form-label">Reporting Manager</label>
                      <select
                        className="form-select"
                        value={formData.managerId}
                        onChange={(e) => setFormData({ ...formData, managerId: e.target.value })}
                        disabled={formData.role === 'Admin'}
                      >
                        <option value="">None (Direct)</option>
                        {managers.map(m => (
                          <option key={m._id} value={m._id}>{m.fullName}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-person-check me-1"></i> Create User
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

