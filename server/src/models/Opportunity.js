const mongoose = require('mongoose');

const opportunitySchema = new mongoose.Schema({
  opportunityCode: {
    type: String,
    required: true,
    unique: true,
    trim: true
  },
  title: {
    type: String,
    required: [true, 'Opportunity title is required'],
    trim: true,
    maxlength: 150
  },
  customerId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Customer',
    required: [true, 'Customer is required']
  },
  leadId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Lead',
    default: null
  },
  amount: {
    type: Number,
    required: [true, 'Deal amount is required'],
    min: [0, 'Amount cannot be negative']
  },
  stage: {
    type: String,
    enum: ['Prospecting', 'Qualification', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost'],
    default: 'Prospecting'
  },
  probability: {
    type: Number,
    default: 10,
    min: 0,
    max: 100
  },
  closeDate: {
    type: Date,
    required: [true, 'Expected close date is required']
  },
  assignedTo: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: [true, 'Assignee is required']
  },
  lostReason: {
    type: String,
    default: ''
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
  timestamps: true,
  toJSON: { virtuals: true },
  toObject: { virtuals: true }
});

opportunitySchema.virtual('weightedAmount').get(function () {
  return (this.amount * this.probability) / 100;
});

opportunitySchema.index({ customerId: 1 });
opportunitySchema.index({ assignedTo: 1 });
opportunitySchema.index({ stage: 1 });

module.exports = mongoose.model('Opportunity', opportunitySchema);
