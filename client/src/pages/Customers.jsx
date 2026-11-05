import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function Customers() {
  const { user } = useAuth();
  const [customers, setCustomers] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);

  // Modal State
  const [showModal, setShowModal] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [currentId, setCurrentId] = useState(null);
  const [formData, setFormData] = useState({
    customerName: '',
    email: '',
    phone: '',
    companyName: '',
    address: '',
    city: '',
    state: '',
    status: 'Active'
  });
  const [modalError, setModalError] = useState('');

  const fetchCustomers = async () => {
    setLoading(true);
    try {
      const res = await api.get('/customers', {
        params: { search, status, page, limit: 10 }
      });
      setCustomers(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
    } catch (err) {
      console.error('Failed to load customers:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, [page, status]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchCustomers();
  };

  const openCreateModal = () => {
    setIsEditing(false);
    setCurrentId(null);
    setFormData({
      customerName: '',
      email: '',
      phone: '',
      companyName: '',
      address: '',
      city: '',
      state: '',
      status: 'Active'
    });
    setModalError('');
    setShowModal(true);
  };

  const openEditModal = (c) => {
    setIsEditing(true);
    setCurrentId(c._id);
    setFormData({
      customerName: c.customerName,
      email: c.email,
      phone: c.phone,
      companyName: c.companyName || '',
      address: c.address || '',
      city: c.city || '',
      state: c.state || '',
      status: c.status || 'Active'
    });
    setModalError('');
    setShowModal(true);
  };

  const handleFormSubmit = async (e) => {
    e.preventDefault();
    setModalError('');

    try {
      if (isEditing) {
        await api.put(`/customers/${currentId}`, formData);
      } else {
        await api.post('/customers', formData);
      }
      setShowModal(false);
      fetchCustomers();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to save customer.');
    }
  };

  const handleDelete = async (id, name) => {
    if (window.confirm(`Are you sure you want to delete customer "${name}"? This action cannot be undone.`)) {
      try {
        await api.delete(`/customers/${id}`);
        fetchCustomers();
      } catch (err) {
        alert(err.response?.data?.message || 'Failed to delete customer.');
      }
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Customer Management</h1>
          <p className="page-subtitle">Track accounts, client contacts, corporate relationships, and account owners</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <i className="bi bi-person-plus"></i> Add Customer
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <form className="row g-2 align-items-center" onSubmit={handleSearchSubmit}>
            <div className="col-12 col-md-5">
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-search"></i></span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Search by code, name, company, email, city..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>

            <div className="col-6 col-md-3">
              <select
                className="form-select"
                value={status}
                onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              >
                <option value="">All Statuses</option>
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="Archived">Archived</option>
              </select>
            </div>

            <div className="col-6 col-md-2">
              <button type="submit" className="btn btn-outline-primary w-100">
                Filter
              </button>
            </div>

            <div className="col-12 col-md-2 text-md-end text-muted small">
              Total: <strong>{total}</strong>
            </div>
          </form>
        </div>
      </div>

      {/* Customers Data Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Customer Name</th>
                <th>Company</th>
                <th>Contact</th>
                <th>City / State</th>
                <th>Account Owner</th>
                <th>Status</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="8" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading customers...
                  </td>
                </tr>
              ) : customers.length > 0 ? (
                customers.map(c => (
                  <tr key={c._id}>
                    <td className="fw-semibold text-primary">{c.customerCode}</td>
                    <td>
                      <div className="fw-semibold text-dark">{c.customerName}</div>
                      <div className="small text-muted">{c.email}</div>
                    </td>
                    <td>{c.companyName || '—'}</td>
                    <td>{c.phone}</td>
                    <td>{c.city ? `${c.city}, ${c.state || ''}` : '—'}</td>
                    <td>
                      <span className="badge bg-light text-dark border">
                        <i className="bi bi-person me-1"></i>{c.ownerId?.fullName || 'Unassigned'}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${
                        c.status === 'Active' ? 'bg-success-subtle text-success border border-success' :
                        c.status === 'Inactive' ? 'bg-warning-subtle text-warning-emphasis border border-warning' :
                        'bg-secondary-subtle text-secondary border'
                      }`}>
                        {c.status}
                      </span>
                    </td>
                    <td className="text-end">
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-secondary"
                          title="Edit Customer"
                          onClick={() => openEditModal(c)}
                        >
                          <i className="bi bi-pencil"></i>
                        </button>
                        {user?.role === 'Admin' && (
                          <button
                            className="btn btn-outline-danger"
                            title="Delete Customer"
                            onClick={() => handleDelete(c._id, c.customerName)}
                          >
                            <i className="bi bi-trash"></i>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="8" className="text-center py-4 text-muted">
                    No customer accounts found matching criteria.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Footer */}
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

      {/* Add / Edit Customer Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">
                  {isEditing ? 'Edit Customer' : 'Add New Customer'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              <form onSubmit={handleFormSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="mb-3">
                    <label className="form-label">Customer Name *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.customerName}
                      onChange={(e) => setFormData({ ...formData, customerName: e.target.value })}
                      required
                      placeholder="e.g. Tata Consultancy Services"
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-12 col-md-6">
                      <label className="form-label">Email *</label>
                      <input
                        type="email"
                        className="form-control"
                        value={formData.email}
                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                        required
                        placeholder="procurement@company.com"
                      />
                    </div>
                    <div className="col-12 col-md-6">
                      <label className="form-label">Phone *</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.phone}
                        onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                        required
                        placeholder="9820012345"
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Company Name</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.companyName}
                      onChange={(e) => setFormData({ ...formData, companyName: e.target.value })}
                      placeholder="Company / Legal Entity"
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-12 col-md-6">
                      <label className="form-label">City</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.city}
                        onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                        placeholder="Mumbai"
                      />
                    </div>
                    <div className="col-12 col-md-6">
                      <label className="form-label">State</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.state}
                        onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                        placeholder="Maharashtra"
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Status</label>
                    <select
                      className="form-select"
                      value={formData.status}
                      onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    >
                      <option value="Active">Active</option>
                      <option value="Inactive">Inactive</option>
                      <option value="Archived">Archived</option>
                    </select>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-check2 me-1"></i> {isEditing ? 'Save Changes' : 'Create Customer'}
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

