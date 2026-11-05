const mongoose = require('mongoose');

const activitySchema = new mongoose.Schema({
  type: {
    type: String,
    enum: ['Call', 'Meeting', 'Email', 'Task', 'Note'],
    default: 'Task'
  },
  subject: {
    type: String,
    required: [true, 'Subject is required'],
    trim: true,
    maxlength: 150
  },
  activityDate: {
    type: Date,
    default: Date.now
  },
  leadId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Lead',
    default: null
  },
  customerId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Customer',
    default: null
  },
  opportunityId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Opportunity',
    default: null
  },
  performedBy: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: [true, 'Performed by user is required']
  },
  durationMinutes: {
    type: Number,
    default: 0,
    min: 0
  },
  notes: {
    type: String,
    default: ''
  },
  createdBy: {
    type: String,
    default: 'System'
  }
}, {
  timestamps: true
});

activitySchema.index({ performedBy: 1 });
activitySchema.index({ activityDate: -1 });

module.exports = mongoose.model('Activity', activitySchema);

