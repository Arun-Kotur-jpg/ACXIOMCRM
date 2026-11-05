const Activity = require('../models/Activity');
const { getRecordScopeFilter, canUserAccessRecord } = require('../middleware/recordScope');
const { logAudit } = require('../middleware/audit');

// @desc    Get activities
// @route   GET /api/activities
// @access  Private
exports.getActivities = async (req, res, next) => {
  try {
    const { type, page = 1, limit = 10, sort = '-activityDate' } = req.query;

    const scopeFilter = await getRecordScopeFilter(req.user, 'performedBy');
    const query = { ...scopeFilter };

    if (type) query.type = type;

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await Activity.countDocuments(query);
    const activities = await Activity.find(query)
      .populate('customerId', 'customerCode customerName')
      .populate('leadId', 'leadCode leadName')
      .populate('opportunityId', 'opportunityCode title')
      .populate('performedBy', 'fullName email role')
      .sort(sort)
      .skip(skip)
      .limit(limitNum);

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: activities.length,
      data: activities
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Create activity
// @route   POST /api/activities
// @access  Private
exports.createActivity = async (req, res, next) => {
  try {
    const { type = 'Task', subject, activityDate, leadId, customerId, opportunityId, durationMinutes = 0, notes } = req.body;

    const activity = await Activity.create({
      type,
      subject,
      activityDate: activityDate || new Date(),
      leadId: leadId || null,
      customerId: customerId || null,
      opportunityId: opportunityId || null,
      performedBy: req.user._id,
      durationMinutes,
      notes: notes || '',
      createdBy: req.user.fullName
    });

    await logAudit({
      req,
      action: 'Log Activity',
      entityName: 'Activity',
      recordId: activity._id,
      newValue: `${type}: ${subject}`,
      details: `Logged ${type} activity: ${subject}`
    });

    res.status(201).json({ success: true, data: activity });
  } catch (err) {
    next(err);
  }
};

