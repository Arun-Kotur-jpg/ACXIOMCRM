const FollowUp = require('../models/FollowUp');
const { getRecordScopeFilter, canUserAccessRecord } = require('../middleware/recordScope');
const { logAudit } = require('../middleware/audit');

// @desc    Get followups with scope filters
// @route   GET /api/followups
// @access  Private
exports.getFollowUps = async (req, res, next) => {
  try {
    const { scope = 'All', status, priority, page = 1, limit = 10, sort = 'scheduledDate' } = req.query;

    const scopeFilter = await getRecordScopeFilter(req.user, 'assignedTo');
    const query = { ...scopeFilter };

    const now = new Date();

    if (scope === 'Overdue') {
      query.status = 'Planned';
      query.scheduledDate = { $lt: now };
    } else if (scope === 'Upcoming') {
      query.status = 'Planned';
      query.scheduledDate = { $gte: now };
    } else if (scope === 'Completed') {
      query.status = 'Completed';
    }

    if (status && scope === 'All') query.status = status;
    if (priority) query.priority = priority;

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await FollowUp.countDocuments(query);
    const followUps = await FollowUp.find(query)
      .populate('customerId', 'customerCode customerName')
      .populate('leadId', 'leadCode leadName')
      .populate('opportunityId', 'opportunityCode title')
      .populate('assignedTo', 'fullName email')
      .sort(sort)
      .skip(skip)
      .limit(limitNum);

    // Global counts for badges / alerts
    const overdueCount = await FollowUp.countDocuments({
      ...scopeFilter,
      status: 'Planned',
      scheduledDate: { $lt: now }
    });

    const upcomingCount = await FollowUp.countDocuments({
      ...scopeFilter,
      status: 'Planned',
      scheduledDate: { $gte: now }
    });

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      overdueCount,
      upcomingCount,
      count: followUps.length,
      data: followUps
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Create followup
// @route   POST /api/followups
// @access  Private
exports.createFollowUp = async (req, res, next) => {
  try {
    const { subject, scheduledDate, leadId, customerId, opportunityId, priority = 'Medium', assignedTo } = req.body;

    const assignee = (req.user.role === 'Admin' || req.user.role === 'Manager') && assignedTo
      ? assignedTo
      : req.user._id;

    const followUp = await FollowUp.create({
      subject,
      scheduledDate,
      leadId: leadId || null,
      customerId: customerId || null,
      opportunityId: opportunityId || null,
      priority,
      status: 'Planned',
      assignedTo: assignee,
      createdBy: req.user.fullName
    });

    await logAudit({
      req,
      action: 'Create Follow-Up',
      entityName: 'FollowUp',
      recordId: followUp._id,
      newValue: followUp.subject,
      details: `Scheduled follow-up for ${new Date(scheduledDate).toLocaleDateString()}`
    });

    res.status(201).json({ success: true, data: followUp });
  } catch (err) {
    next(err);
  }
};

// @desc    Complete followup
// @route   POST /api/followups/:id/complete
// @access  Private
exports.completeFollowUp = async (req, res, next) => {
  try {
    const followUp = await FollowUp.findById(req.params.id);
    if (!followUp) {
      return res.status(404).json({ success: false, message: 'Follow-up not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, followUp.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to update this follow-up.'
      });
    }

    followUp.status = 'Completed';
    followUp.completionDate = new Date();
    followUp.outcomeNotes = req.body.outcomeNotes || 'Completed as scheduled';
    await followUp.save();

    await logAudit({
      req,
      action: 'Complete Follow-Up',
      entityName: 'FollowUp',
      recordId: followUp._id,
      newValue: 'Completed',
      details: `Marked follow-up ${followUp.subject} as completed.`
    });

    res.status(200).json({ success: true, data: followUp });
  } catch (err) {
    next(err);
  }
};

