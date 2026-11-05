import React, { useState, useEffect } from 'react';
import api from '../services/api';

const STAGES = [
  'Prospecting',
  'Qualification',
  'Proposal',
  'Negotiation',
  'ClosedWon',
  'ClosedLost'
];

export default function Opportunities() {
  const [opportunities, setOpportunities] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [total, setTotal] = useState(0);
  const [totalValue, setTotalValue] = useState(0);
  const [weightedValue, setWeightedValue] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [search, setSearch] = useState('');
  const [stage, setStage] = useState('');
  const [loading, setLoading] = useState(true);

  // Modal State
  const [showModal, setShowModal] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [currentId, setCurrentId] = useState(null);
  const [formData, setFormData] = useState({
    title: '',
    customerId: '',
    amount: 100000,
    stage: 'Prospecting',
    probability: 10,
    closeDate: '',
    notes: '',
    lostReason: ''
  });
  const [modalError, setModalError] = useState('');

  const fetchOpportunities = async () => {
    setLoading(true);
    try {
      const res = await api.get('/opportunities', {
        params: { search, stage, page, limit: 10 }
      });
      setOpportunities(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
      setTotalValue(res.data.totalPipelineValue || 0);
      setWeightedValue(res.data.weightedPipelineValue || 0);
    } catch (err) {
      console.error('Failed to load opportunities:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchCustomersList = async () => {
    try {
      const res = await api.get('/customers?limit=100');
      setCustomers(res.data.data);
    } catch (err) {
      console.error('Failed to load customers for dropdown:', err);
    }
  };

  useEffect(() => {
    fetchOpportunities();
  }, [page, stage]);

  useEffect(() => {
    fetchCustomersList();
  }, []);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchOpportunities();
  };

  const openCreateModal = () => {
    setIsEditing(false);
    setCurrentId(null);
    const d = new Date();
    d.setDate(d.getDate() + 30);
    setFormData({
      title: '',
      customerId: customers.length > 0 ? customers[0]._id : '',
      amount: 250000,
      stage: 'Prospecting',
      probability: 10,
      closeDate: d.toISOString().split('T')[0],
      notes: '',
      lostReason: ''
    });
    setModalError('');
    setShowModal(true);
  };

  const openEditModal = (opp) => {
    setIsEditing(true);
    setCurrentId(opp._id);
    setFormData({
      title: opp.title,
      customerId: opp.customerId?._id || opp.customerId,
      amount: opp.amount,
      stage: opp.stage,
      probability: opp.probability,
      closeDate: opp.closeDate ? new Date(opp.closeDate).toISOString().split('T')[0] : '',
      notes: opp.notes || '',
      lostReason: opp.lostReason || ''
    });
    setModalError('');
    setShowModal(true);
  };

  const handleFormSubmit = async (e) => {
    e.preventDefault();
    setModalError('');

    try {
      if (isEditing) {
        await api.put(`/opportunities/${currentId}`, formData);
      } else {
        await api.post('/opportunities', formData);
      }
      setShowModal(false);
      fetchOpportunities();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to save opportunity.');
    }
  };

  const handleStageQuickChange = async (oppId, newStage) => {
    try {
      await api.put(`/opportunities/${oppId}`, { stage: newStage });
      fetchOpportunities();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to update stage.');
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Sales Pipeline & Opportunities</h1>
          <p className="page-subtitle">Track deal progression across sales stages, forecasted revenue, and win probability</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <i className="bi bi-briefcase"></i> New Opportunity
        </button>
      </div>

      {/* Revenue Header Summary */}
      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <div className="p-3 bg-white border rounded d-flex justify-content-between align-items-center">
            <div>
              <div className="small text-muted text-uppercase fw-semibold">Gross Pipeline Value</div>
              <div className="fs-4 fw-bold text-dark">₹{totalValue.toLocaleString('en-IN')}</div>
            </div>
            <span className="badge bg-primary-subtle text-primary fs-5 p-2 rounded-circle">
              <i className="bi bi-currency-rupee"></i>
            </span>
          </div>
        </div>

        <div className="col-12 col-md-6">
          <div className="p-3 bg-white border rounded d-flex justify-content-between align-items-center">
            <div>
              <div className="small text-muted text-uppercase fw-semibold">Weighted Pipeline Forecast</div>
              <div className="fs-4 fw-bold text-success">₹{Math.round(weightedValue).toLocaleString('en-IN')}</div>
            </div>
            <span className="badge bg-success-subtle text-success fs-5 p-2 rounded-circle">
              <i className="bi bi-graph-up-arrow"></i>
            </span>
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <form className="row g-2 align-items-center" onSubmit={handleSearchSubmit}>
            <div className="col-12 col-md-6">
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-search"></i></span>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Search deal code or title..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>

            <div className="col-6 col-md-4">
              <select
                className="form-select"
                value={stage}
                onChange={(e) => { setStage(e.target.value); setPage(1); }}
              >
                <option value="">All Pipeline Stages</option>
                {STAGES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>

            <div className="col-6 col-md-2">
              <button type="submit" className="btn btn-outline-primary w-100">Filter</button>
            </div>
          </form>
        </div>
      </div>

      {/* Opportunities Data Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Opportunity Title</th>
                <th>Customer</th>
                <th>Amount</th>
                <th>Stage Progression</th>
                <th>Probability</th>
                <th>Close Date</th>
                <th>Assigned Rep</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="9" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading opportunities...
                  </td>
                </tr>
              ) : opportunities.length > 0 ? (
                opportunities.map(o => (
                  <tr key={o._id}>
                    <td className="fw-semibold text-primary">{o.opportunityCode}</td>
                    <td>
                      <div className="fw-semibold text-dark">{o.title}</div>
                      {o.lostReason && <div className="small text-danger">Lost Reason: {o.lostReason}</div>}
                    </td>
                    <td>{o.customerId?.customerName || 'N/A'}</td>
                    <td className="fw-bold">₹{o.amount.toLocaleString('en-IN')}</td>
                    <td>
                      <select
                        className="form-select form-select-sm"
                        value={o.stage}
                        style={{ width: '140px' }}
                        onChange={(e) => handleStageQuickChange(o._id, e.target.value)}
                      >
                        {STAGES.map(s => <option key={s} value={s}>{s}</option>)}
                      </select>
                    </td>
                    <td>
                      <div className="d-flex align-items-center gap-2">
                        <span className="small fw-semibold">{o.probability}%</span>
                        <div className="progress flex-grow-1" style={{ height: '6px', minWidth: '40px' }}>
                          <div
                            className={`progress-bar ${o.stage === 'ClosedWon' ? 'bg-success' : o.stage === 'ClosedLost' ? 'bg-danger' : 'bg-primary'}`}
                            style={{ width: `${o.probability}%` }}
                          ></div>
                        </div>
                      </div>
                    </td>
                    <td className="small text-muted">
                      {o.closeDate ? new Date(o.closeDate).toLocaleDateString() : '—'}
                    </td>
                    <td>
                      <span className="badge bg-light text-dark border">
                        <i className="bi bi-person me-1"></i>{o.assignedTo?.fullName || 'Unassigned'}
                      </span>
                    </td>
                    <td className="text-end">
                      <button
                        className="btn btn-sm btn-outline-secondary"
                        onClick={() => openEditModal(o)}
                        title="Edit Opportunity"
                      >
                        <i className="bi bi-pencil"></i>
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="9" className="text-center py-4 text-muted">
                    No sales opportunities found.
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

      {/* Create / Edit Opportunity Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">
                  {isEditing ? 'Edit Opportunity' : 'Create New Opportunity'}
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              <form onSubmit={handleFormSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="mb-3">
                    <label className="form-label">Opportunity Title *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.title}
                      onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                      required
                      placeholder="e.g. Enterprise Cloud Deployment Phase 2"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Customer Account *</label>
                    <select
                      className="form-select"
                      value={formData.customerId}
                      onChange={(e) => setFormData({ ...formData, customerId: e.target.value })}
                      required
                    >
                      <option value="">Select Customer</option>
                      {customers.map(c => (
                        <option key={c._id} value={c._id}>
                          {c.customerCode} - {c.customerName}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Deal Amount (₹) *</label>
                      <input
                        type="number"
                        className="form-control"
                        value={formData.amount}
                        onChange={(e) => setFormData({ ...formData, amount: Number(e.target.value) })}
                        required
                        min="0"
                      />
                    </div>
                    <div className="col-6">
                      <label className="form-label">Close Date *</label>
                      <input
                        type="date"
                        className="form-control"
                        value={formData.closeDate}
                        onChange={(e) => setFormData({ ...formData, closeDate: e.target.value })}
                        required
                      />
                    </div>
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Stage</label>
                      <select
                        className="form-select"
                        value={formData.stage}
                        onChange={(e) => setFormData({ ...formData, stage: e.target.value })}
                      >
                        {STAGES.map(s => <option key={s} value={s}>{s}</option>)}
                      </select>
                    </div>
                    <div className="col-6">
                      <label className="form-label">Probability (%)</label>
                      <input
                        type="number"
                        className="form-control"
                        value={formData.probability}
                        onChange={(e) => setFormData({ ...formData, probability: Number(e.target.value) })}
                        min="0"
                        max="100"
                      />
                    </div>
                  </div>

                  {formData.stage === 'ClosedLost' && (
                    <div className="mb-3">
                      <label className="form-label text-danger">Lost Reason</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.lostReason}
                        onChange={(e) => setFormData({ ...formData, lostReason: e.target.value })}
                        placeholder="e.g. Competitor pricing, budget cancelled"
                      />
                    </div>
                  )}

                  <div className="mb-3">
                    <label className="form-label">Notes</label>
                    <textarea
                      className="form-control"
                      rows="2"
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      placeholder="Next steps, requirements, proposal details"
                    ></textarea>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-check2 me-1"></i> {isEditing ? 'Save Changes' : 'Create Opportunity'}
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

