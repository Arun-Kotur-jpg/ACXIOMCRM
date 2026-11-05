const mongoose = require('mongoose');

const customerSchema = new mongoose.Schema({
  customerCode: {
    type: String,
    required: true,
    unique: true,
    trim: true
  },
  customerName: {
    type: String,
    required: [true, 'Customer name is required'],
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
  address: {
    type: String,
    trim: true
  },
  city: {
    type: String,
    trim: true,
    maxlength: 50
  },
  state: {
    type: String,
    trim: true,
    maxlength: 50
  },
  status: {
    type: String,
    enum: ['Active', 'Inactive', 'Archived'],
    default: 'Active'
  },
  ownerId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: [true, 'Owner is required']
  },
  createdBy: {
    type: String,
    default: 'System'
  }
}, {
  timestamps: true
});

customerSchema.index({ ownerId: 1 });
customerSchema.index({ status: 1 });

module.exports = mongoose.model('Customer', customerSchema);
