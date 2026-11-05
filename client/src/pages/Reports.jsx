import React, { useState, useEffect } from 'react';
import api from '../services/api';

export default function Reports() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchAnalytics = async () => {
      try {
        const res = await api.get('/reports/analytics');
        setData(res.data);
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to load sales analytics.');
      } finally {
        setLoading(false);
      }
    };

    fetchAnalytics();
  }, []);

  if (loading) {
    return (
      <div className="text-center py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading Analytics...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  const { summary, repPerformance } = data;

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Executive Sales Reports & Analytics</h1>
          <p className="page-subtitle">Revenue realization, win/loss conversion ratios, and sales team performance</p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="row g-3 mb-4">
        <div className="col-12 col-md-3">
          <div className="kpi-card">
            <span className="kpi-label">Total Deals Closed</span>
            <div className="kpi-value">{summary?.totalDeals || 0}</div>
            <div className="kpi-subtext text-muted">Across all quarters</div>
          </div>
        </div>

        <div className="col-12 col-md-3">
          <div className="kpi-card">
            <span className="kpi-label">Won Deals</span>
            <div className="kpi-value text-success">{summary?.wonDealsCount || 0}</div>
            <div className="kpi-subtext text-muted">Successful conversions</div>
          </div>
        </div>

        <div className="col-12 col-md-3">
          <div className="kpi-card">
            <span className="kpi-label">Total Won Realized Revenue</span>
            <div className="kpi-value text-primary">₹{(summary?.totalWonAmount || 0).toLocaleString('en-IN')}</div>
            <div className="kpi-subtext text-muted">Closed-won contract value</div>
          </div>
        </div>

        <div className="col-12 col-md-3">
          <div className="kpi-card">
            <span className="kpi-label">Lost Deal Value</span>
            <div className="kpi-value text-danger">₹{(summary?.totalLostAmount || 0).toLocaleString('en-IN')}</div>
            <div className="kpi-subtext text-muted">Budget/competitor churn</div>
          </div>
        </div>
      </div>

      {/* Sales Representative Performance Leaderboard */}
      <div className="crm-card">
        <div className="crm-card-header">
          <span><i className="bi bi-trophy text-warning me-2"></i>Sales Representative Performance Leaderboard</span>
          <span className="text-muted small fw-normal">Team productivity & realized revenue</span>
        </div>
        <div className="table-responsive">
          <table className="crm-table">
            <thead>
              <tr>
                <th>Representative Name</th>
                <th>Total Deals Managed</th>
                <th>Won Deals</th>
                <th>Win Rate %</th>
                <th>Total Value Managed</th>
                <th>Won Revenue Realized</th>
              </tr>
            </thead>
            <tbody>
              {repPerformance?.length > 0 ? (
                repPerformance.map((rep, idx) => {
                  const rate = rep.totalDeals > 0 ? Math.round((rep.wonDeals / rep.totalDeals) * 100) : 0;
                  return (
                    <tr key={idx}>
                      <td className="fw-semibold text-dark">
                        <i className="bi bi-person-badge text-primary me-2"></i>{rep.repName}
                      </td>
                      <td>{rep.totalDeals}</td>
                      <td><span className="badge bg-success-subtle text-success border">{rep.wonDeals}</span></td>
                      <td>
                        <div className="d-flex align-items-center gap-2">
                          <span className="small fw-semibold">{rate}%</span>
                          <div className="progress flex-grow-1" style={{ height: '6px', maxWidth: '80px' }}>
                            <div className="progress-bar bg-success" style={{ width: `${rate}%` }}></div>
                          </div>
                        </div>
                      </td>
                      <td>₹{rep.totalValue.toLocaleString('en-IN')}</td>
                      <td className="fw-bold text-success">₹{rep.wonValue.toLocaleString('en-IN')}</td>
                    </tr>
                  );
                })
              ) : (
                <tr>
                  <td colSpan="6" className="text-center py-4 text-muted">
                    No sales representative performance data recorded.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

