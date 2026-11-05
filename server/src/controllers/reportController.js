const Customer = require('../models/Customer');
const Lead = require('../models/Lead');
const Opportunity = require('../models/Opportunity');
const FollowUp = require('../models/FollowUp');
const User = require('../models/User');
const { getRecordScopeFilter } = require('../middleware/recordScope');

// @desc    Get dashboard metrics & summary
// @route   GET /api/reports/dashboard
// @access  Private
exports.getDashboardMetrics = async (req, res, next) => {
  try {
    const custScope = await getRecordScopeFilter(req.user, 'ownerId');
    const leadScope = await getRecordScopeFilter(req.user, 'assignedTo');
    const oppScope = await getRecordScopeFilter(req.user, 'assignedTo');
    const fuScope = await getRecordScopeFilter(req.user, 'assignedTo');

    const totalCustomers = await Customer.countDocuments({ ...custScope, status: 'Active' });
    const totalLeads = await Lead.countDocuments({ ...leadScope });
    const openLeads = await Lead.countDocuments({ ...leadScope, status: { $nin: ['Converted', 'Unqualified'] } });

    const opps = await Opportunity.find({ ...oppScope });
    const totalPipelineValue = opps.reduce((acc, curr) => acc + (curr.amount || 0), 0);
    const weightedPipelineValue = opps.reduce((acc, curr) => acc + ((curr.amount * curr.probability) / 100), 0);

    const wonCount = opps.filter(o => o.stage === 'ClosedWon').length;
    const closedCount = opps.filter(o => o.stage === 'ClosedWon' || o.stage === 'ClosedLost').length;
    const winRate = closedCount > 0 ? Math.round((wonCount / closedCount) * 100) : 0;

    const now = new Date();
    const overdueFollowUps = await FollowUp.countDocuments({
      ...fuScope,
      status: 'Planned',
      scheduledDate: { $lt: now }
    });

    const upcomingFollowUps = await FollowUp.countDocuments({
      ...fuScope,
      status: 'Planned',
      scheduledDate: { $gte: now }
    });

    // Recent 5 leads
    const recentLeads = await Lead.find({ ...leadScope })
      .populate('assignedTo', 'fullName')
      .sort('-createdAt')
      .limit(5);

    // Recent 5 opportunities
    const recentOpportunities = await Opportunity.find({ ...oppScope })
      .populate('customerId', 'customerName')
      .populate('assignedTo', 'fullName')
      .sort('-createdAt')
      .limit(5);

    // Pipeline by Stage
    const stages = ['Prospecting', 'Qualification', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost'];
    const pipelineByStage = stages.map(st => {
      const stageOpps = opps.filter(o => o.stage === st);
      const totalAmount = stageOpps.reduce((sum, o) => sum + o.amount, 0);
      return { stage: st, count: stageOpps.length, totalAmount };
    });

    // Leads by Status
    const leadStatuses = ['New', 'Contacted', 'Qualified', 'Unqualified', 'Converted'];
    const allLeads = await Lead.find({ ...leadScope });
    const leadsByStatus = leadStatuses.map(st => ({
      status: st,
      count: allLeads.filter(l => l.status === st).length
    }));

    res.status(200).json({
      success: true,
      kpis: {
        totalCustomers,
        totalLeads,
        openLeads,
        totalPipelineValue,
        weightedPipelineValue,
        winRate,
        overdueFollowUps,
        upcomingFollowUps
      },
      charts: {
        pipelineByStage,
        leadsByStatus
      },
      recentLeads,
      recentOpportunities
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Get detailed pipeline and analytics reports
// @route   GET /api/reports/analytics
// @access  Private
exports.getAnalytics = async (req, res, next) => {
  try {
    const oppScope = await getRecordScopeFilter(req.user, 'assignedTo');
    const leadScope = await getRecordScopeFilter(req.user, 'assignedTo');

    const opps = await Opportunity.find({ ...oppScope }).populate('assignedTo', 'fullName');
    const leads = await Lead.find({ ...leadScope }).populate('assignedTo', 'fullName');

    // Win/Loss metrics
    const wonOpps = opps.filter(o => o.stage === 'ClosedWon');
    const lostOpps = opps.filter(o => o.stage === 'ClosedLost');
    const totalWonAmount = wonOpps.reduce((sum, o) => sum + o.amount, 0);
    const totalLostAmount = lostOpps.reduce((sum, o) => sum + o.amount, 0);

    // Performance by Sales Representative
    const repMap = {};
    opps.forEach(o => {
      const repName = o.assignedTo?.fullName || 'Unassigned';
      if (!repMap[repName]) {
        repMap[repName] = { repName, totalDeals: 0, wonDeals: 0, totalValue: 0, wonValue: 0 };
      }
      repMap[repName].totalDeals += 1;
      repMap[repName].totalValue += o.amount;
      if (o.stage === 'ClosedWon') {
        repMap[repName].wonDeals += 1;
        repMap[repName].wonValue += o.amount;
      }
    });

    const repPerformance = Object.values(repMap);

    res.status(200).json({
      success: true,
      summary: {
        totalDeals: opps.length,
        wonDealsCount: wonOpps.length,
        lostDealsCount: lostOpps.length,
        totalWonAmount,
        totalLostAmount
      },
      repPerformance
    });
  } catch (err) {
    next(err);
  }
};

