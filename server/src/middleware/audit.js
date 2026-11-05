const AuditLog = require('../models/AuditLog');

async function logAudit({ req, action, entityName, recordId = '', oldValue = null, newValue = null, result = 'Success', details = '' }) {
  try {
    const userId = req.user ? req.user._id : null;
    const userName = req.user ? req.user.fullName : (req.body?.email || 'Anonymous');
    const ipAddress = req.ip || req.connection?.remoteAddress || '127.0.0.1';

    await AuditLog.create({
      userId,
      userName,
      action,
      entityName,
      recordId: recordId ? recordId.toString() : '',
      oldValue: oldValue ? (typeof oldValue === 'string' ? oldValue : JSON.stringify(oldValue)) : null,
      newValue: newValue ? (typeof newValue === 'string' ? newValue : JSON.stringify(newValue)) : null,
      result,
      ipAddress,
      details,
      timestamp: new Date()
    });
  } catch (err) {
    console.error('Audit logging error:', err.message);
  }
}

module.exports = { logAudit };

