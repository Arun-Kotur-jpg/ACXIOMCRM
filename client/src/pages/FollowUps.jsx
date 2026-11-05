import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import api from '../services/api';

export default function FollowUps() {
  const location = useLocation();
  const searchParams = new URLSearchParams(location.search);
  const initialScope = searchParams.get('scope') || 'All';

  const [followUps, setFollowUps] = useState([]);
  const [scope, setScope] = useState(initialScope);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [counts, setCounts] = useState({ overdue: 0, upcoming: 0 });
  const [loading, setLoading] = useState(true);

  // Modals
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showCompleteModal, setShowCompleteModal] = useState(false);
  const [selectedItem, setSelectedItem] = useState(null);
  const [outcomeNotes, setOutcomeNotes] = useState('');

  const [formData, setFormData] = useState({
    subject: '',
    scheduledDate: '',
    priority: 'Medium'
  });
  const [modalError, setModalError] = useState('');

  const fetchFollowUps = async () => {
    setLoading(true);
    try {
      const res = await api.get('/followups', {
        params: { scope, page, limit: 10 }
      });
      setFollowUps(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
      setCounts({
        overdue: res.data.overdueCount || 0,
        upcoming: res.data.upcomingCount || 0
      });
    } catch (err) {
      console.error('Failed to load follow-ups:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFollowUps();
  }, [page, scope]);

  const openScheduleDialog = () => {
    const d = new Date();
    d.setDate(d.getDate() + 2);
    setFormData({
      subject: '',
      scheduledDate: d.toISOString().split('T')[0],
      priority: 'Medium'
    });
    setModalError('');
    setShowScheduleModal(true);
  };

  const handleScheduleSubmit = async (e) => {
    e.preventDefault();
    setModalError('');
    try {
      await api.post('/followups', formData);
      setShowScheduleModal(false);
      fetchFollowUps();
    } catch (err) {
      setModalError(err.response?.data?.message || 'Failed to schedule follow-up.');
    }
  };

  const openCompleteDialog = (item) => {
    setSelectedItem(item);
    setOutcomeNotes('');
    setShowCompleteModal(true);
  };

  const handleCompleteSubmit = async (e) => {
    e.preventDefault();
    try {
      await api.post(`/followups/${selectedItem._id}/complete`, { outcomeNotes });
      setShowCompleteModal(false);
      fetchFollowUps();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to complete follow-up.');
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Follow-Up Reminders & Tasks</h1>
          <p className="page-subtitle">Schedule, track, and complete sales follow-up calls, meetings, and client reviews</p>
        </div>
        <button className="btn btn-primary" onClick={openScheduleDialog}>
          <i className="bi bi-calendar-plus"></i> Schedule Follow-Up
        </button>
      </div>

      {/* Scope Navigation Tabs */}
      <ul className="nav nav-pills mb-3 bg-white p-2 border rounded">
        <li className="nav-item">
          <button
            className={`nav-link btn-sm ${scope === 'All' ? 'active' : ''}`}
            onClick={() => { setScope('All'); setPage(1); }}
          >
            All Follow-Ups
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link btn-sm ${scope === 'Overdue' ? 'active bg-danger' : 'text-danger'}`}
            onClick={() => { setScope('Overdue'); setPage(1); }}
          >
            <i className="bi bi-exclamation-circle me-1"></i>
            Overdue ({counts.overdue})
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link btn-sm ${scope === 'Upcoming' ? 'active' : ''}`}
            onClick={() => { setScope('Upcoming'); setPage(1); }}
          >
            <i className="bi bi-clock me-1"></i>
            Upcoming ({counts.upcoming})
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link btn-sm ${scope === 'Completed' ? 'active' : ''}`}
            onClick={() => { setScope('Completed'); setPage(1); }}
          >
            <i className="bi bi-check-circle me-1"></i>
            Completed
          </button>
        </li>
      </ul>

      {/* Follow-up Tasks Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Subject</th>
                <th>Linked Record</th>
                <th>Due Date</th>
                <th>Priority</th>
                <th>Status</th>
                <th>Assigned Rep</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="7" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading tasks...
                  </td>
                </tr>
              ) : followUps.length > 0 ? (
                followUps.map(f => {
                  const isOverdue = f.status === 'Planned' && new Date(f.scheduledDate) < new Date();
                  return (
                    <tr key={f._id} className={isOverdue ? 'table-danger-subtle' : ''}>
                      <td>
                        <div className="fw-semibold text-dark">{f.subject}</div>
                        {f.outcomeNotes && <div className="small text-muted">{f.outcomeNotes}</div>}
                      </td>
                      <td>
                        {f.customerId ? (
                          <span className="badge bg-light text-dark border">
                            <i className="bi bi-building me-1"></i>{f.customerId.customerName}
                          </span>
                        ) : f.leadId ? (
                          <span className="badge bg-light text-dark border">
                            <i className="bi bi-funnel me-1"></i>{f.leadId.leadName}
                          </span>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td>
                        <div className={isOverdue ? 'text-danger fw-bold' : ''}>
                          {new Date(f.scheduledDate).toLocaleDateString()}
                          {isOverdue && <span className="badge bg-danger ms-2">OVERDUE</span>}
                        </div>
                      </td>
                      <td>
                        <span className={`badge ${
                          f.priority === 'Urgent' ? 'bg-danger' :
                          f.priority === 'High' ? 'bg-warning text-dark' :
                          f.priority === 'Medium' ? 'bg-primary' : 'bg-secondary'
                        }`}>
                          {f.priority}
                        </span>
                      </td>
                      <td>
                        <span className={`badge ${f.status === 'Completed' ? 'bg-success' : 'bg-warning text-dark'}`}>
                          {f.status}
                        </span>
                      </td>
                      <td>{f.assignedTo?.fullName || 'Unassigned'}</td>
                      <td className="text-end">
                        {f.status !== 'Completed' && (
                          <button
                            className="btn btn-sm btn-outline-success"
                            onClick={() => openCompleteDialog(f)}
                            title="Mark Completed"
                          >
                            <i className="bi bi-check2"></i> Complete
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })
              ) : (
                <tr>
                  <td colSpan="7" className="text-center py-4 text-muted">
                    No follow-up items found in this scope.
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

      {/* Schedule Follow-up Modal */}
      {showScheduleModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom">
                <h5 className="modal-title fw-bold">Schedule Follow-Up Task</h5>
                <button type="button" className="btn-close" onClick={() => setShowScheduleModal(false)}></button>
              </div>

              <form onSubmit={handleScheduleSubmit}>
                <div className="modal-body">
                  {modalError && <div className="alert alert-danger small p-2 mb-3">{modalError}</div>}

                  <div className="mb-3">
                    <label className="form-label">Subject / Action *</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.subject}
                      onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                      required
                      placeholder="e.g. Call client regarding proposal feedback"
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-6">
                      <label className="form-label">Due Date *</label>
                      <input
                        type="date"
                        className="form-control"
                        value={formData.scheduledDate}
                        onChange={(e) => setFormData({ ...formData, scheduledDate: e.target.value })}
                        required
                      />
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
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowScheduleModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary">
                    <i className="bi bi-calendar-check me-1"></i> Schedule
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Complete Follow-up Modal */}
      {showCompleteModal && selectedItem && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} tabIndex="-1">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow">
              <div className="modal-header border-bottom bg-light">
                <h5 className="modal-title fw-bold text-success">
                  <i className="bi bi-check-circle me-2"></i>Complete Follow-Up
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowCompleteModal(false)}></button>
              </div>

              <form onSubmit={handleCompleteSubmit}>
                <div className="modal-body">
                  <div className="mb-3">
                    <strong>Task:</strong> {selectedItem.subject}
                  </div>

                  <div className="mb-3">
                    <label className="form-label">Outcome & Meeting Notes</label>
                    <textarea
                      className="form-control"
                      rows="3"
                      value={outcomeNotes}
                      onChange={(e) => setOutcomeNotes(e.target.value)}
                      placeholder="Discussed requirements, customer requested updated quotation..."
                      required
                    ></textarea>
                  </div>
                </div>

                <div className="modal-footer border-top">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowCompleteModal(false)}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-success">
                    Mark as Completed
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

