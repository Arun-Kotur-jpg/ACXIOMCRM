const mongoose = require('mongoose');

const auditLogSchema = new mongoose.Schema({
  userId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    default: null
  },
  userName: {
    type: String,
    default: 'System'
  },
  action: {
    type: String,
    required: true
  },
  entityName: {
    type: String,
    required: true
  },
  recordId: {
    type: String,
    default: ''
  },
  oldValue: {
    type: String,
    default: null
  },
  newValue: {
    type: String,
    default: null
  },
  result: {
    type: String,
    enum: ['Success', 'Failed'],
    default: 'Success'
  },
  ipAddress: {
    type: String,
    default: '127.0.0.1'
  },
  details: {
    type: String,
    default: ''
  },
  timestamp: {
    type: Date,
    default: Date.now
  }
}, {
  timestamps: false
});

auditLogSchema.index({ timestamp: -1 });
auditLogSchema.index({ entityName: 1 });
auditLogSchema.index({ action: 1 });

module.exports = mongoose.model('AuditLog', auditLogSchema);

