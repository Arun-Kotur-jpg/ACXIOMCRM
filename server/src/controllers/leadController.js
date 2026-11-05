const Lead = require('../models/Lead');
const Customer = require('../models/Customer');
const Opportunity = require('../models/Opportunity');
const { getRecordScopeFilter, canUserAccessRecord } = require('../middleware/recordScope');
const { logAudit } = require('../middleware/audit');

// Helper to generate lead code
async function generateLeadCode() {
  const count = await Lead.countDocuments();
  const nextNum = (count + 1).toString().padStart(4, '0');
  return `LEAD-${nextNum}`;
}

// @desc    Get all leads (scoped by role)
// @route   GET /api/leads
// @access  Private
exports.getLeads = async (req, res, next) => {
  try {
    const { search, status, priority, source, page = 1, limit = 10, sort = '-createdAt' } = req.query;

    const scopeFilter = await getRecordScopeFilter(req.user, 'assignedTo');
    const query = { ...scopeFilter };

    if (status) query.status = status;
    if (priority) query.priority = priority;
    if (source) query.source = source;

    if (search) {
      query.$or = [
        { leadName: { $regex: search, $options: 'i' } },
        { leadCode: { $regex: search, $options: 'i' } },
        { companyName: { $regex: search, $options: 'i' } },
        { email: { $regex: search, $options: 'i' } }
      ];
    }

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await Lead.countDocuments(query);
    const leads = await Lead.find(query)
      .populate('assignedTo', 'fullName email role')
      .populate('convertedToCustomerId', 'customerCode customerName')
      .sort(sort)
      .skip(skip)
      .limit(limitNum);

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: leads.length,
      data: leads
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Get single lead by ID
// @route   GET /api/leads/:id
// @access  Private
exports.getLeadById = async (req, res, next) => {
  try {
    const lead = await Lead.findById(req.params.id)
      .populate('assignedTo', 'fullName email role')
      .populate('convertedToCustomerId', 'customerCode customerName')
      .populate('convertedOpportunityId', 'opportunityCode title amount stage');

    if (!lead) {
      return res.status(404).json({ success: false, message: 'Lead not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, lead.assignedTo?._id || lead.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to view this lead.'
      });
    }

    res.status(200).json({ success: true, data: lead });
  } catch (err) {
    next(err);
  }
};

// @desc    Create lead
// @route   POST /api/leads
// @access  Private
exports.createLead = async (req, res, next) => {
  try {
    const { leadName, email, phone, companyName, source, status, priority, expectedValue, notes, assignedTo } = req.body;

    const leadCode = await generateLeadCode();
    const assignee = (req.user.role === 'Admin' || req.user.role === 'Manager') && assignedTo
      ? assignedTo
      : req.user._id;

    const lead = await Lead.create({
      leadCode,
      leadName,
      email,
      phone,
      companyName,
      source: source || 'Website',
      status: status || 'New',
      priority: priority || 'Medium',
      expectedValue: expectedValue || 0,
      notes: notes || '',
      assignedTo: assignee,
      createdBy: req.user.fullName
    });

    await logAudit({
      req,
      action: 'Create Lead',
      entityName: 'Lead',
      recordId: lead._id,
      newValue: lead.leadName,
      details: `Created lead ${lead.leadCode}: ${lead.leadName}`
    });

    res.status(201).json({ success: true, data: lead });
  } catch (err) {
    next(err);
  }
};

// @desc    Update lead
// @route   PUT /api/leads/:id
// @access  Private
exports.updateLead = async (req, res, next) => {
  try {
    let lead = await Lead.findById(req.params.id);
    if (!lead) {
      return res.status(404).json({ success: false, message: 'Lead not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, lead.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to update this lead.'
      });
    }

    // Business rule: Once converted, a lead cannot be changed back to another status
    if (lead.status === 'Converted' && req.body.status && req.body.status !== 'Converted') {
      return res.status(400).json({
        success: false,
        message: 'A converted lead cannot change status.'
      });
    }

    const updatableFields = ['leadName', 'email', 'phone', 'companyName', 'source', 'status', 'priority', 'expectedValue', 'notes'];
    if (req.user.role === 'Admin' || req.user.role === 'Manager') {
      updatableFields.push('assignedTo');
    }

    updatableFields.forEach(field => {
      if (req.body[field] !== undefined) {
        lead[field] = req.body[field];
      }
    });

    await lead.save();

    await logAudit({
      req,
      action: 'Update Lead',
      entityName: 'Lead',
      recordId: lead._id,
      newValue: lead.status,
      details: `Updated lead ${lead.leadCode}`
    });

    res.status(200).json({ success: true, data: lead });
  } catch (err) {
    next(err);
  }
};

// @desc    Convert lead into Customer and optionally Opportunity
// @route   POST /api/leads/:id/convert
// @access  Private
exports.convertLead = async (req, res, next) => {
  try {
    const lead = await Lead.findById(req.params.id);
    if (!lead) {
      return res.status(404).json({ success: false, message: 'Lead not found.' });
    }

    if (lead.status === 'Converted') {
      return res.status(400).json({
        success: false,
        message: 'This lead has already been converted.'
      });
    }

    const allowed = await canUserAccessRecord(req.user, lead.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to convert this lead.'
      });
    }

    const { createOpportunity = true, opportunityTitle, dealAmount, closeDate } = req.body;

    // 1. Create Customer
    const countCust = await Customer.countDocuments();
    const customerCode = `CUST-${(countCust + 1).toString().padStart(4, '0')}`;

    const customer = await Customer.create({
      customerCode,
      customerName: lead.leadName,
      email: lead.email,
      phone: lead.phone,
      companyName: lead.companyName || lead.leadName,
      status: 'Active',
      ownerId: lead.assignedTo || req.user._id,
      createdBy: req.user.fullName
    });

    // 2. Optionally create Opportunity
    let opportunity = null;
    if (createOpportunity) {
      const countOpp = await Opportunity.countDocuments();
      const oppCode = `OPP-${(countOpp + 1).toString().padStart(4, '0')}`;

      opportunity = await Opportunity.create({
        opportunityCode: oppCode,
        title: opportunityTitle || `${lead.leadName} - Initial Deal`,
        customerId: customer._id,
        leadId: lead._id,
        amount: dealAmount !== undefined ? dealAmount : (lead.expectedValue || 100000),
        stage: 'Qualification',
        probability: 20,
        closeDate: closeDate ? new Date(closeDate) : new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
        assignedTo: lead.assignedTo || req.user._id,
        createdBy: req.user.fullName
      });
    }

    // 3. Mark Lead as Converted
    lead.status = 'Converted';
    lead.convertedToCustomerId = customer._id;
    lead.convertedOpportunityId = opportunity ? opportunity._id : null;
    lead.convertedDate = new Date();
    await lead.save();

    await logAudit({
      req,
      action: 'Lead Converted',
      entityName: 'Lead',
      recordId: lead._id,
      newValue: `Customer: ${customer.customerCode}${opportunity ? `, Opp: ${opportunity.opportunityCode}` : ''}`,
      details: `Lead ${lead.leadCode} converted to Customer ${customer.customerCode}`
    });

    res.status(200).json({
      success: true,
      message: 'Lead converted successfully.',
      customer,
      opportunity
    });
  } catch (err) {
    next(err);
  }
};

