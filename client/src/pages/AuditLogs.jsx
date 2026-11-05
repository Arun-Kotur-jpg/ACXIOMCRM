import React, { useState, useEffect } from 'react';
import api from '../services/api';

export default function AuditLogs() {
  const [logs, setLogs] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [action, setAction] = useState('');
  const [entityName, setEntityName] = useState('');
  const [result, setResult] = useState('');
  const [loading, setLoading] = useState(true);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const res = await api.get('/audit', {
        params: { action, entityName, result, page, limit: 15 }
      });
      setLogs(res.data.data);
      setTotal(res.data.total);
      setPages(res.data.pages);
    } catch (err) {
      console.error('Failed to load audit logs:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, [page, entityName, result]);

  const handleFilterSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchLogs();
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Security & Business Audit Logs</h1>
          <p className="page-subtitle">Immutable audit trail of authentication events, entity modifications, and business conversions</p>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="crm-card mb-3">
        <div className="crm-card-body p-3">
          <form className="row g-2 align-items-center" onSubmit={handleFilterSubmit}>
            <div className="col-12 col-md-4">
              <input
                type="text"
                className="form-control"
                placeholder="Search action name (e.g. Login, Converted)..."
                value={action}
                onChange={(e) => setAction(e.target.value)}
              />
            </div>

            <div className="col-6 col-md-3">
              <select
                className="form-select"
                value={entityName}
                onChange={(e) => { setEntityName(e.target.value); setPage(1); }}
              >
                <option value="">All Entities</option>
                <option value="Authentication">Authentication</option>
                <option value="Customer">Customer</option>
                <option value="Lead">Lead</option>
                <option value="Opportunity">Opportunity</option>
                <option value="FollowUp">FollowUp</option>
                <option value="Activity">Activity</option>
                <option value="User">User</option>
                <option value="System">System</option>
              </select>
            </div>

            <div className="col-6 col-md-3">
              <select
                className="form-select"
                value={result}
                onChange={(e) => { setResult(e.target.value); setPage(1); }}
              >
                <option value="">All Results</option>
                <option value="Success">Success</option>
                <option value="Failed">Failed</option>
              </select>
            </div>

            <div className="col-12 col-md-2">
              <button type="submit" className="btn btn-outline-primary w-100">Filter</button>
            </div>
          </form>
        </div>
      </div>

      {/* Logs Table */}
      <div className="crm-card">
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Actor / User</th>
                <th>Action</th>
                <th>Target Entity</th>
                <th>Outcome</th>
                <th>Client IP</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="7" className="text-center py-4 text-muted">
                    <div className="spinner-border spinner-border-sm me-2 text-primary" role="status"></div>
                    Loading audit trail...
                  </td>
                </tr>
              ) : logs.length > 0 ? (
                logs.map(log => (
                  <tr key={log._id}>
                    <td className="small text-muted">{new Date(log.timestamp).toLocaleString()}</td>
                    <td className="fw-semibold text-dark">{log.userName}</td>
                    <td>
                      <span className="fw-semibold text-dark">{log.action}</span>
                    </td>
                    <td>
                      <span className="badge bg-light text-secondary border">{log.entityName}</span>
                    </td>
                    <td>
                      <span className={`badge ${log.result === 'Success' ? 'bg-success' : 'bg-danger'}`}>
                        {log.result}
                      </span>
                    </td>
                    <td className="small text-muted font-monospace">{log.ipAddress}</td>
                    <td className="small text-secondary">{log.details || '—'}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="7" className="text-center py-4 text-muted">
                    No audit logs recorded for this criteria.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {pages > 1 && (
          <div className="d-flex justify-content-between align-items-center p-3 border-top">
            <span className="small text-muted">Page {page} of {pages} (Total: {total})</span>
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
    </div>
  );
}

