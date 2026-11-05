import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement
} from 'chart.js';
import { Bar, Doughnut } from 'react-chartjs-2';
import api from '../services/api';

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement
);

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const res = await api.get('/reports/dashboard');
        setData(res.data);
      } catch (err) {
        setError(err.response?.data?.message || 'Failed to load dashboard metrics.');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  if (loading) {
    return (
      <div className="text-center py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading Dashboard...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  const { kpis, charts, recentLeads, recentOpportunities } = data;

  // Chart 1: Pipeline by Stage Bar Chart
  const barChartData = {
    labels: charts?.pipelineByStage?.map(s => s.stage) || [],
    datasets: [
      {
        label: 'Pipeline Value (₹)',
        data: charts?.pipelineByStage?.map(s => s.totalAmount) || [],
        backgroundColor: '#0284c7',
        borderRadius: 4
      }
    ]
  };

  const barChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => '₹' + (value >= 100000 ? (value / 100000).toFixed(1) + 'L' : value)
        }
      }
    }
  };

  // Chart 2: Lead Status Doughnut Chart
  const doughnutData = {
    labels: charts?.leadsByStatus?.map(s => s.status) || [],
    datasets: [
      {
        data: charts?.leadsByStatus?.map(s => s.count) || [],
        backgroundColor: [
          '#38bdf8',
          '#0284c7',
          '#10b981',
          '#f59e0b',
          '#64748b'
        ],
        borderWidth: 1
      }
    ]
  };

  const doughnutOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom' }
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Executive Sales Dashboard</h1>
          <p className="page-subtitle">Real-time metrics, pipeline health, and sales operational status</p>
        </div>
        <div className="d-flex gap-2">
          <Link to="/leads" className="btn btn-primary">
            <i className="bi bi-funnel-plus"></i> New Lead
          </Link>
          <Link to="/opportunities" className="btn btn-outline-primary">
            <i className="bi bi-briefcase"></i> View Pipeline
          </Link>
        </div>
      </div>

      {/* Overdue Alert Banner */}
      {kpis?.overdueFollowUps > 0 && (
        <div className="alert alert-warning alert-dismissible fade show d-flex align-items-center mb-4" role="alert">
          <i className="bi bi-exclamation-triangle-fill text-danger fs-5 me-2"></i>
          <div>
            <strong>Action Required:</strong> You have <strong>{kpis.overdueFollowUps} overdue follow-up task(s)</strong> that require immediate attention.
          </div>
          <Link to="/followups?scope=Overdue" className="btn btn-sm btn-outline-dark ms-auto">
            View Overdue Tasks
          </Link>
        </div>
      )}

      {/* KPI Cards Row */}
      <div className="row g-3 mb-4">
        <div className="col-12 col-sm-6 col-xl-3">
          <div className="kpi-card">
            <div className="d-flex justify-content-between align-items-center">
              <span className="kpi-label">Active Customers</span>
              <span className="badge bg-primary-subtle text-primary border"><i className="bi bi-people"></i></span>
            </div>
            <div className="kpi-value">{kpis?.totalCustomers || 0}</div>
            <div className="kpi-subtext text-muted">Managed corporate accounts</div>
          </div>
        </div>

        <div className="col-12 col-sm-6 col-xl-3">
          <div className="kpi-card">
            <div className="d-flex justify-content-between align-items-center">
              <span className="kpi-label">Open Leads</span>
              <span className="badge bg-info-subtle text-info border"><i className="bi bi-funnel"></i></span>
            </div>
            <div className="kpi-value">{kpis?.openLeads || 0}</div>
            <div className="kpi-subtext text-muted">Of {kpis?.totalLeads || 0} total leads registered</div>
          </div>
        </div>

        <div className="col-12 col-sm-6 col-xl-3">
          <div className="kpi-card">
            <div className="d-flex justify-content-between align-items-center">
              <span className="kpi-label">Total Pipeline</span>
              <span className="badge bg-success-subtle text-success border"><i className="bi bi-currency-rupee"></i></span>
            </div>
            <div className="kpi-value">₹{(kpis?.totalPipelineValue || 0).toLocaleString('en-IN')}</div>
            <div className="kpi-subtext text-muted">Weighted: ₹{(kpis?.weightedPipelineValue || 0).toLocaleString('en-IN')}</div>
          </div>
        </div>

        <div className="col-12 col-sm-6 col-xl-3">
          <div className="kpi-card">
            <div className="d-flex justify-content-between align-items-center">
              <span className="kpi-label">Deal Win Rate</span>
              <span className="badge bg-warning-subtle text-warning border"><i className="bi bi-trophy"></i></span>
            </div>
            <div className="kpi-value">{kpis?.winRate || 0}%</div>
            <div className="kpi-subtext text-muted">Won vs closed lost deals</div>
          </div>
        </div>
      </div>

      {/* Charts Row */}
      <div className="row g-3 mb-4">
        <div className="col-12 col-lg-8">
          <div className="crm-card h-100">
            <div className="crm-card-header">
              <span><i className="bi bi-bar-chart-line text-primary me-2"></i>Sales Pipeline by Stage</span>
              <span className="text-muted small fw-normal">Gross Value in INR</span>
            </div>
            <div className="crm-card-body" style={{ minHeight: '300px' }}>
              <Bar data={barChartData} options={barChartOptions} />
            </div>
          </div>
        </div>

        <div className="col-12 col-lg-4">
          <div className="crm-card h-100">
            <div className="crm-card-header">
              <span><i className="bi bi-pie-chart text-info me-2"></i>Leads by Status</span>
              <span className="text-muted small fw-normal">Distribution</span>
            </div>
            <div className="crm-card-body" style={{ minHeight: '300px' }}>
              <Doughnut data={doughnutData} options={doughnutOptions} />
            </div>
          </div>
        </div>
      </div>

      {/* Recent Records Row */}
      <div className="row g-3">
        <div className="col-12 col-lg-6">
          <div className="crm-card">
            <div className="crm-card-header">
              <span><i className="bi bi-funnel text-primary me-2"></i>Recent Leads</span>
              <Link to="/leads" className="small text-decoration-none">View All</Link>
            </div>
            <div className="table-responsive">
              <table className="crm-table">
                <thead>
                  <tr>
                    <th>Lead Name</th>
                    <th>Status</th>
                    <th>Value</th>
                    <th>Owner</th>
                  </tr>
                </thead>
                <tbody>
                  {recentLeads?.length > 0 ? (
                    recentLeads.map(l => (
                      <tr key={l._id}>
                        <td>
                          <div className="fw-semibold text-dark">{l.leadName}</div>
                          <div className="small text-muted">{l.companyName}</div>
                        </td>
                        <td>
                          <span className={`badge ${
                            l.status === 'Converted' ? 'bg-success' :
                            l.status === 'Qualified' ? 'bg-primary' :
                            l.status === 'Contacted' ? 'bg-info' : 'bg-secondary'
                          }`}>
                            {l.status}
                          </span>
                        </td>
                        <td>₹{(l.expectedValue || 0).toLocaleString('en-IN')}</td>
                        <td>{l.assignedTo?.fullName || 'Unassigned'}</td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="4" className="text-center text-muted py-3">No leads recorded.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div className="col-12 col-lg-6">
          <div className="crm-card">
            <div className="crm-card-header">
              <span><i className="bi bi-briefcase text-success me-2"></i>Recent Opportunities</span>
              <Link to="/opportunities" className="small text-decoration-none">View All</Link>
            </div>
            <div className="table-responsive">
              <table className="crm-table">
                <thead>
                  <tr>
                    <th>Title</th>
                    <th>Customer</th>
                    <th>Stage</th>
                    <th>Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {recentOpportunities?.length > 0 ? (
                    recentOpportunities.map(o => (
                      <tr key={o._id}>
                        <td>
                          <div className="fw-semibold text-dark">{o.title}</div>
                          <div className="small text-muted">{o.opportunityCode}</div>
                        </td>
                        <td>{o.customerId?.customerName || 'N/A'}</td>
                        <td>
                          <span className={`badge ${
                            o.stage === 'ClosedWon' ? 'bg-success' :
                            o.stage === 'ClosedLost' ? 'bg-danger' :
                            o.stage === 'Negotiation' ? 'bg-warning text-dark' : 'bg-primary'
                          }`}>
                            {o.stage}
                          </span>
                        </td>
                        <td className="fw-semibold">₹{(o.amount || 0).toLocaleString('en-IN')}</td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="4" className="text-center text-muted py-3">No opportunities in pipeline.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

