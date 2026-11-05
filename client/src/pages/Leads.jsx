import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function Leads() {
  const { user } = useAuth();
  const [leads, setLeads] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [priority, setPriority] = useState('');
  const [loading, setLoading] = useState(true);

  // Create / Edit Modal State
  const [showModal, setShowModal] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [currentId, setCurrentId] = useState(null);
  const [formData, setFormData] = useState({
    leadName: '',
    email: '',
    phone: '',
    companyName: '',
    source: 'Website',
    status: 'New',
    priority: 'Medium',
    expectedValue: 100000,
    notes: ''
  });
  const [modalError, setModalError] = useState('');

  // Conversion Modal State
  const [showConvertModal, setShowConvertModal] = useState(false);
  const [convertLeadData, setConvertLeadData] = useState(null);
  const [convertForm, setConvertForm] = useState({
    createOpportunity: true,
    opportunityTitle: '',
    dealAmount: 100000,
    closeDate: ''
  });
  const [convertError, setConvertError] = useState('');

  const fetchLeads = async () => {
    setLoading(true);
    try {
      const res = await api.get('/leads', {
        params: { search, status, priority, page, limit: 10 }
      });
      setLeads(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
    } catch (err) {
      console.error('Failed to load leads:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLeads();
  }, [page, status, priority]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchLeads();
  };

  const openCreateModal = () => {
    setIsEditing(false);
    setCurrentId(null);
    setFormData({
      leadName: '',
      email: '',
      phone: '',
      companyName: '',
      source: 'Website',
      status: 'New',
      priority: 'Medium',
      expectedValue: 100000,
      notes: ''
    });
    setModalError('');
    setShowModal(true);
  };

  const openEditModal = (l) => {
    setIsEditing(true);
    setCurrentId(l._id);
    setFormData({
      leadName: l.leadName,
      email: l.email,
      phone: l.phone,
      companyName: l.companyName || '',
      source: l.source || 'Website',
      status: l.status || 'New',
      priority: l.priority || 'Medium',
      expectedValue: l.expectedValue || 0,
      notes: l.notes || ''
    });
    setModalError('');
    setShowModal(true);
  };

  const handleFormSubmit = async (e) => {
    e.preventDefault();
    setModalError('');

    try {
      if (isEditing) {
        await api.put(`/leads/${currentId}`, formData);
      } else {
        await api.post('/leads', formData);
      }
      setShowModal(false);
      fetchLeads();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to save lead.');
    }
  };

  const openConvertDialog = (lead) => {
    setConvertLeadData(lead);
    const d = new Date();
    d.setDate(d.getDate() + 30);
    setConvertForm({
      createOpportunity: true,
      opportunityTitle: `${lead.leadName} - Initial Deal`,
      dealAmount: lead.expectedValue || 150000,
      closeDate: d.toISOString().split('T')[0]
    });
    setConvertError('');
    setShowConvertModal(true);
  };

  const handleConvertSubmit = async (e) => {
    e.preventDefault();
    setConvertError('');

    try {
      await api.post(`/leads/${convertLeadData._id}/convert`, convertForm);
      setShowConvertModal(false);
      fetchLeads();
    } catch (err) {
      setConvertError(err.response?.data?.message || 'Failed to convert lead.');
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Sales Leads Management</h1>
          <p className="page-subtitle">Prospect acquisition, lead qualification, and customer conversion pipeline</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <i className="bi bi-funnel-plus"></i> New Lead
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <form className="row g-2 align-items-center" onSubmit={handleSearchSubmit}>
            <div className="col-12 col-md-4">
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-search"></i></span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Search lead code, name, company, email..."
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
                <option value="New">New</option>
                <option value="Contacted">Contacted</option>
                <option value="Qualified">Qualified</option>
                <option value="Unqualified">Unqualified</option>
                <option value="Converted">Converted</option>
              </select>
            </div>

            <div className="col-6 col-md-2">
              <select
                className="form-select"
                value={priority}
                onChange={(e) => { setPriority(e.target.value); setPage(1); }}
              >
                <option value="">All Priorities</option>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>

            <div className="col-6 col-md-1">
              <button type="submit" className="btn btn-outline-primary w-100">
                Filter
              </button>
            </div>

            <div className="col-6 col-md-2 text-md-end text-muted small">
              Total: <strong>{total}</strong>
            </div>
          </form>
        </div>
      </div>

      {/* Leads Data Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Lead Name</th>
                <th>Company</th>
                <th>Source</th>
                <th>Expected Value</th>
                <th>Status</th>
                <th>Assigned Rep</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="8" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading leads...
                  </td>
                </tr>
              ) : leads.length > 0 ? (
                leads.map(l => (
                  <tr key={l._id}>
                    <td className="fw-semibold text-primary">{l.leadCode}</td>
                    <td>
                      <div className="fw-semibold text-dark">{l.leadName}</div>
                      <div className="small text-muted">{l.email} &bull; {l.phone}</div>
                    </td>
                    <td>{l.companyName || '—'}</td>
                    <td><span className="badge bg-light text-secondary border">{l.source}</span></td>
                    <td className="fw-semibold">₹{(l.expectedValue || 0).toLocaleString('en-IN')}</td>
                    <td>
                      <span className={`badge ${
                        l.status === 'Converted' ? 'bg-success-subtle text-success border border-success' :
                        l.status === 'Qualified' ? 'bg-primary-subtle text-primary border border-primary' :
                        l.status === 'Contacted' ? 'bg-info-subtle text-info border border-info' :
                        l.status === 'Unqualified' ? 'bg-danger-subtle text-danger border border-danger' :
                        'bg-secondary-subtle text-secondary border'
                      }`}>
                        {l.status}
                      </span>
                    </td>
                    <td>
                      <span className="badge bg-light text-dark border">
                        <i className="bi bi-person me-1"></i>{l.assignedTo?.fullName || 'Unassigned'}
                      </span>
                    </td>
                    <td className="text-end">
                      <div className="btn-group btn-group-sm">
                        {l.status !== 'Converted' && (
                          <button
                            className="btn btn-outline-success"
                            title="Convert to Customer"
                            onClick={() => openConvertDialog(l)}
                          >
                            <i className="bi bi-arrow-repeat me-1"></i> Convert
                          </button>
                        )}
                        <button
                          className="btn btn-outline-secondary"
                          title="Edit Lead"
                          onClick={() => openEditModal(l)}
                        >
                          <i className="bi bi-pencil"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="8" className="text-center py-4 text-muted">
                    No leads found matching your search.
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

      {/* Create / Edit Lead Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">{isEditing ? 'Edit Lead' : 'Create New Lead'}</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              <form onSubmit={handleFormSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="mb-3">
                    <label className="form-label">Lead Name *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.leadName}
                      onChange={(e) => setFormData({ ...formData, leadName: e.target.value })}
                      required
                      placeholder="e.g. Adani Logistics Solutions"
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
                        placeholder="contact@company.com"
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
                        placeholder="9879012349"
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
                      placeholder="Organization / Firm"
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Source</label>
                      <select
                        className="form-select"
                        value={formData.source}
                        onChange={(e) => setFormData({ ...formData, source: e.target.value })}
                      >
                        <option value="Website">Website</option>
                        <option value="Referral">Referral</option>
                        <option value="ColdCall">Cold Call</option>
                        <option value="TradeShow">Trade Show</option>
                        <option value="Advertisement">Advertisement</option>
                        <option value="EmailCampaign">Email Campaign</option>
                        <option value="Other">Other</option>
                      </select>
                    </div>

                    <div className="col-6">
                      <label className="form-label">Priority</label>
                      <select
                        className="form-select"
                        value={formData.priority}
                        onChange={(e) => setFormData({ ...formData, priority: e.target.value })}
                      >
                        <option value="Low">Low</option>
                        <option value="Medium">Medium</option>
                        <option value="High">High</option>
                        <option value="Urgent">Urgent</option>
                      </select>
                    </div>
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Status</label>
                      <select
                        className="form-select"
                        value={formData.status}
                        onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                        disabled={isEditing && formData.status === 'Converted'}
                      >
                        <option value="New">New</option>
                        <option value="Contacted">Contacted</option>
                        <option value="Qualified">Qualified</option>
                        <option value="Unqualified">Unqualified</option>
                        {isEditing && formData.status === 'Converted' && (
                          <option value="Converted">Converted</option>
                        )}
                      </select>
                    </div>

                    <div className="col-6">
                      <label className="form-label">Expected Value (₹)</label>
                      <input
                        type="number"
                        className="form-control"
                        value={formData.expectedValue}
                        onChange={(e) => setFormData({ ...formData, expectedValue: Number(e.target.value) })}
                        min="0"
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Notes</label>
                    <textarea
                      className="form-control"
                      rows="2"
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      placeholder="Add requirements, qualification details, etc."
                    ></textarea>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-check2 me-1"></i> {isEditing ? 'Save Changes' : 'Create Lead'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Convert Lead to Customer Modal */}
      {showConvertModal && convertLeadData && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom bg-light">
                <h5 className="modal-title fw-bold text-success">
                  <i className="bi bi-arrow-repeat me-2"></i>Convert Lead to Customer
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowConvertModal(false)}></button>
              </div>

              <form onSubmit={handleConvertSubmit}>
                <div className="modal-body">
                  {convertError && <div className="alert alert-danger small p-2 mb-3">{convertError}</div>}

                  <div className="p-3 bg-light rounded border mb-3">
                    <div className="fw-semibold text-dark">{convertLeadData.leadName}</div>
                    <div className="small text-muted">{convertLeadData.companyName || 'Independent'} &bull; {convertLeadData.email}</div>
                  </div>

                  <p className="small text-muted">
                    Converting this lead will automatically create an active <strong>Customer Account</strong>.
                  </p>

                  <div className="form-check form-switch mb-3">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="createOppCheck"
                      checked={convertForm.createOpportunity}
                      onChange={(e) => setConvertForm({ ...convertForm, createOpportunity: e.target.checked })}
                    />
                    <label className="form-check-label fw-semibold small" htmlFor="createOppCheck">
                      Also open a Sales Opportunity deal
                    </label>
                  </div>

                  {convertForm.createOpportunity && (
                    <div className="p-3 border rounded bg-white mb-3">
                      <div className="mb-2">
                        <label className="form-label small">Opportunity Title</label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          value={convertForm.opportunityTitle}
                          onChange={(e) => setConvertForm({ ...convertForm, opportunityTitle: e.target.value })}
                          required
                        />
                      </div>

                      <div className="row g-2">
                        <div className="col-6">
                          <label className="form-label small">Deal Amount (₹)</label>
                          <input
                            type="number"
                            className="form-control form-control-sm"
                            value={convertForm.dealAmount}
                            onChange={(e) => setConvertForm({ ...convertForm, dealAmount: Number(e.target.value) })}
                            required
                            min="0"
                          />
                        </div>
                        <div className="col-6">
                          <label className="form-label small">Expected Close Date</label>
                          <input
                            type="date"
                            className="form-control form-control-sm"
                            value={convertForm.closeDate}
                            onChange={(e) => setConvertForm({ ...convertForm, closeDate: e.target.value })}
                            required
                          />
                        </div>
                      </div>
                    </div>
                  )}
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowConvertModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-success">
                    <i className="bi bi-check-circle me-1"></i> Confirm Conversion
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

