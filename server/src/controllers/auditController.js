const AuditLog = require('../models/AuditLog');

// @desc    Get audit logs
// @route   GET /api/audit
// @access  Private (Admin only)
exports.getAuditLogs = async (req, res, next) => {
  try {
    const { action, entityName, result, page = 1, limit = 20 } = req.query;
    const query = {};

    if (action) query.action = { $regex: action, $options: 'i' };
    if (entityName) query.entityName = entityName;
    if (result) query.result = result;

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await AuditLog.countDocuments(query);
    const logs = await AuditLog.find(query)
      .sort('-timestamp')
      .skip(skip)
      .limit(limitNum);

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: logs.length,
      data: logs
    });
  } catch (err) {
    next(err);
  }
};

