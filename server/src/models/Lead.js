const mongoose = require('mongoose');

const leadSchema = new mongoose.Schema({
  leadCode: {
    type: String,
    required: true,
    unique: true,
    trim: true
  },
  leadName: {
    type: String,
    required: [true, 'Lead name is required'],
    trim: true,
    maxlength: 150
  },
  email: {
    type: String,
    required: [true, 'Email is required'],
    trim: true,
    lowercase: true
  },
  phone: {
    type: String,
    required: [true, 'Phone number is required'],
    trim: true
  },
  companyName: {
    type: String,
    trim: true,
    maxlength: 150
  },
  source: {
    type: String,
    enum: ['Website', 'Referral', 'ColdCall', 'TradeShow', 'Advertisement', 'EmailCampaign', 'Other'],
    default: 'Website'
  },
  status: {
    type: String,
    enum: ['New', 'Contacted', 'Qualified', 'Unqualified', 'Converted'],
    default: 'New'
  },
  priority: {
    type: String,
    enum: ['Low', 'Medium', 'High', 'Urgent'],
    default: 'Medium'
  },
  expectedValue: {
    type: Number,
    default: 0,
    min: 0
  },
  notes: {
    type: String,
    default: ''
  },
  assignedTo: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    default: null
  },
  convertedToCustomerId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Customer',
    default: null
  },
  convertedOpportunityId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Opportunity',
    default: null
  },
  convertedDate: {
    type: Date,
    default: null
  },
  createdBy: {
    type: String,
    default: 'System'
  }
}, {
  timestamps: true
});

leadSchema.index({ assignedTo: 1 });
leadSchema.index({ status: 1 });

module.exports = mongoose.model('Lead', leadSchema);
