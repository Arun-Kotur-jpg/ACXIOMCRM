const User = require('../models/User');

/**
 * Returns a MongoDB query filter object based on user's authorized record scope.
 * - Admin: sees all records ({})
 * - Manager: sees own records + records owned by reporting sales executives
 * - Sales Executive: sees only own records
 */
async function getRecordScopeFilter(user, fieldName = 'ownerId') {
  if (user.role === 'Admin') {
    return {};
  }

  if (user.role === 'Manager') {
    // Find all users who report to this manager
    const subordinates = await User.find({ managerId: user._id }).select('_id');
    const allowedUserIds = [user._id, ...subordinates.map(s => s._id)];
    return { [fieldName]: { $in: allowedUserIds } };
  }

  // Sales Executive: strictly own records
  return { [fieldName]: user._id };
}

/**
 * Verifies if user has permission to view, edit or delete a specific record.
 */
async function canUserAccessRecord(user, recordOwnerId) {
  if (user.role === 'Admin') return true;
  if (!recordOwnerId) return true;

  const ownerIdStr = recordOwnerId.toString();
  const userIdStr = user._id.toString();

  if (user.role === 'Sales Executive') {
    return ownerIdStr === userIdStr;
  }

  if (user.role === 'Manager') {
    if (ownerIdStr === userIdStr) return true;
    const subordinates = await User.find({ managerId: user._id }).select('_id');
    const subIds = subordinates.map(s => s._id.toString());
    return subIds.includes(ownerIdStr);
  }

  return false;
}

module.exports = {
  getRecordScopeFilter,
  canUserAccessRecord
};

