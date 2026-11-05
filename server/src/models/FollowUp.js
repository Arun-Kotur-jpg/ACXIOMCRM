const mongoose = require('mongoose');

const followUpSchema = new mongoose.Schema({
  subject: {
    type: String,
    required: [true, 'Subject is required'],
    trim: true,
    maxlength: 150
  },
  scheduledDate: {
    type: Date,
    required: [true, 'Scheduled date is required']
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
  assignedTo: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: [true, 'Assigned user is required']
  },
  priority: {
    type: String,
    enum: ['Low', 'Medium', 'High', 'Urgent'],
    default: 'Medium'
  },
  status: {
    type: String,
    enum: ['Planned', 'Completed', 'Cancelled'],
    default: 'Planned'
  },
  completionDate: {
    type: Date,
    default: null
  },
  outcomeNotes: {
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

followUpSchema.index({ assignedTo: 1 });
followUpSchema.index({ scheduledDate: 1 });
followUpSchema.index({ status: 1 });

module.exports = mongoose.model('FollowUp', followUpSchema);

