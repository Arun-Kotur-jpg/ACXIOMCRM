import React, { useState, useEffect } from 'react';
import api from '../services/api';

export default function Activities() {
  const [activities, setActivities] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [type, setType] = useState('');
  const [loading, setLoading] = useState(true);

  // Modal
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({
    type: 'Call',
    subject: '',
    durationMinutes: 15,
    notes: ''
  });
  const [modalError, setModalError] = useState('');

  const fetchActivities = async () => {
    setLoading(true);
    try {
      const res = await api.get('/activities', {
        params: { type, page, limit: 10 }
      });
      setActivities(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
    } catch (err) {
      console.error('Failed to load activities:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchActivities();
  }, [page, type]);

  const openCreateModal = () => {
    setFormData({
      type: 'Call',
      subject: '',
      durationMinutes: 15,
      notes: ''
    });
    setModalError('');
    setShowModal(true);
  };

  const handleFormSubmit = async (e) => {
    e.preventDefault();
    setModalError('');

    try {
      await api.post('/activities', formData);
      setShowModal(false);
      fetchActivities();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to log activity.');
    }
  };

  const getActivityIcon = (actType) => {
    switch (actType) {
      case 'Call': return <i className="bi bi-telephone text-primary"></i>;
      case 'Meeting': return <i className="bi bi-people text-success"></i>;
      case 'Email': return <i className="bi bi-envelope text-info"></i>;
      case 'Task': return <i className="bi bi-check2-square text-warning"></i>;
      default: return <i className="bi bi-journal-text text-secondary"></i>;
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Sales Activity Tracking</h1>
          <p className="page-subtitle">Historical audit of phone calls, client meetings, product demos, emails, and tasks</p>
        </div>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <i className="bi bi-plus-circle"></i> Log Activity
        </button>
      </div>

      {/* Filter Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <div className="row g-2 align-items-center">
            <div className="col-12 col-md-4">
              <select
                className="form-select"
                value={type}
                onChange={(e) => { setType(e.target.value); setPage(1); }}
              >
                <option value="">All Activity Types</option>
                <option value="Call">Phone Calls</option>
                <option value="Meeting">Meetings & Demos</option>
                <option value="Email">Emails</option>
                <option value="Task">Tasks</option>
                <option value="Note">Notes</option>
              </select>
            </div>
            <div className="col-12 col-md-8 text-md-end text-muted small">
              Total Recorded Activities: <strong>{total}</strong>
            </div>
          </div>
        </div>
      </div>

      {/* Activities Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Subject</th>
                <th>Date & Time</th>
                <th>Duration</th>
                <th>Performed By</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="6" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading activity history...
                  </td>
                </tr>
              ) : activities.length > 0 ? (
                activities.map(a => (
                  <tr key={a._id}>
                    <td>
                      <span className="d-inline-flex align-items-center gap-2 fw-semibold">
                        {getActivityIcon(a.type)}
                        {a.type}
                      </span>
                    </td>
                    <td className="fw-semibold text-dark">{a.subject}</td>
                    <td className="small text-muted">{new Date(a.activityDate).toLocaleString()}</td>
                    <td>{a.durationMinutes > 0 ? `${a.durationMinutes} mins` : '—'}</td>
                    <td>
                      <span className="badge bg-light text-dark border">
                        <i className="bi bi-person me-1"></i>{a.performedBy?.fullName || 'Unassigned'}
                      </span>
                    </td>
                    <td className="small text-secondary">{a.notes || '—'}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="6" className="text-center py-4 text-muted">
                    No sales activities logged yet.
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

      {/* Log Activity Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">Log New Sales Activity</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>

              <form onSubmit={handleFormSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Type</label>
                      <select
                        className="form-select"
                        value={formData.type}
                        onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                      >
                        <option value="Call">Call</option>
                        <option value="Meeting">Meeting</option>
                        <option value="Email">Email</option>
                        <option value="Task">Task</option>
                        <option value="Note">Note</option>
                      </select>
                    </div>
                    <div className="col-6">
                      <label className="form-label">Duration (Minutes)</label>
                      <input
                        type="number"
                        className="form-control"
                        value={formData.durationMinutes}
                        onChange={(e) => setFormData({ ...formData, durationMinutes: Number(e.target.value) })}
                        min="0"
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Subject *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.subject}
                      onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                      required
                      placeholder="e.g. Discussed proposal pricing with CIO"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Discussion Notes</label>
                    <textarea
                      className="form-control"
                      rows="3"
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      placeholder="Add conversation summary, action items, next steps..."
                    ></textarea>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-check2 me-1"></i> Save Activity
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

