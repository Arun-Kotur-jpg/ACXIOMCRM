const Opportunity = require('../models/Opportunity');
const { getRecordScopeFilter, canUserAccessRecord } = require('../middleware/recordScope');
const { logAudit } = require('../middleware/audit');

const STAGE_PROBABILITIES = {
  Prospecting: 10,
  Qualification: 20,
  Proposal: 60,
  Negotiation: 80,
  ClosedWon: 100,
  ClosedLost: 0
};

async function generateOpportunityCode() {
  const count = await Opportunity.countDocuments();
  const nextNum = (count + 1).toString().padStart(4, '0');
  return `OPP-${nextNum}`;
}

// @desc    Get all opportunities (scoped by role)
// @route   GET /api/opportunities
// @access  Private
exports.getOpportunities = async (req, res, next) => {
  try {
    const { search, stage, page = 1, limit = 10, sort = '-createdAt' } = req.query;

    const scopeFilter = await getRecordScopeFilter(req.user, 'assignedTo');
    const query = { ...scopeFilter };

    if (stage) query.stage = stage;

    if (search) {
      query.$or = [
        { title: { $regex: search, $options: 'i' } },
        { opportunityCode: { $regex: search, $options: 'i' } }
      ];
    }

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await Opportunity.countDocuments(query);
    const opportunities = await Opportunity.find(query)
      .populate('customerId', 'customerCode customerName')
      .populate('assignedTo', 'fullName email role')
      .sort(sort)
      .skip(skip)
      .limit(limitNum);

    // Calculate total pipeline value
    const allMatching = await Opportunity.find(query);
    const totalPipelineValue = allMatching.reduce((acc, curr) => acc + (curr.amount || 0), 0);
    const weightedPipelineValue = allMatching.reduce((acc, curr) => acc + ((curr.amount * curr.probability) / 100), 0);

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: opportunities.length,
      totalPipelineValue,
      weightedPipelineValue,
      data: opportunities
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Get single opportunity by ID
// @route   GET /api/opportunities/:id
// @access  Private
exports.getOpportunityById = async (req, res, next) => {
  try {
    const opportunity = await Opportunity.findById(req.params.id)
      .populate('customerId', 'customerCode customerName email phone')
      .populate('leadId', 'leadCode leadName')
      .populate('assignedTo', 'fullName email role');

    if (!opportunity) {
      return res.status(404).json({ success: false, message: 'Opportunity not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, opportunity.assignedTo?._id || opportunity.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to view this opportunity.'
      });
    }

    res.status(200).json({ success: true, data: opportunity });
  } catch (err) {
    next(err);
  }
};

// @desc    Create opportunity
// @route   POST /api/opportunities
// @access  Private
exports.createOpportunity = async (req, res, next) => {
  try {
    const { title, customerId, leadId, amount, stage = 'Prospecting', probability, closeDate, notes, assignedTo } = req.body;

    const opportunityCode = await generateOpportunityCode();
    const assignee = (req.user.role === 'Admin' || req.user.role === 'Manager') && assignedTo
      ? assignedTo
      : req.user._id;

    const prob = probability !== undefined ? probability : (STAGE_PROBABILITIES[stage] || 10);

    const opportunity = await Opportunity.create({
      opportunityCode,
      title,
      customerId,
      leadId: leadId || null,
      amount,
      stage,
      probability: prob,
      closeDate: closeDate || new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      notes: notes || '',
      assignedTo: assignee,
      createdBy: req.user.fullName
    });

    await logAudit({
      req,
      action: 'Create Opportunity',
      entityName: 'Opportunity',
      recordId: opportunity._id,
      newValue: opportunity.title,
      details: `Created opportunity ${opportunity.opportunityCode} for amount ₹${opportunity.amount}`
    });

    res.status(201).json({ success: true, data: opportunity });
  } catch (err) {
    next(err);
  }
};

// @desc    Update opportunity stage or details
// @route   PUT /api/opportunities/:id
// @access  Private
exports.updateOpportunity = async (req, res, next) => {
  try {
    let opportunity = await Opportunity.findById(req.params.id);
    if (!opportunity) {
      return res.status(404).json({ success: false, message: 'Opportunity not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, opportunity.assignedTo);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to update this opportunity.'
      });
    }

    const oldStage = opportunity.stage;

    // Stage updates
    if (req.body.stage && req.body.stage !== oldStage) {
      opportunity.stage = req.body.stage;
      opportunity.probability = req.body.probability !== undefined
        ? req.body.probability
        : (STAGE_PROBABILITIES[req.body.stage] || opportunity.probability);

      if (req.body.stage === 'ClosedLost') {
        opportunity.lostReason = req.body.lostReason || 'Not specified';
      }
    }

    const updatableFields = ['title', 'customerId', 'amount', 'closeDate', 'notes', 'lostReason', 'probability'];
    if (req.user.role === 'Admin' || req.user.role === 'Manager') {
      updatableFields.push('assignedTo');
    }

    updatableFields.forEach(field => {
      if (req.body[field] !== undefined) {
        opportunity[field] = req.body[field];
      }
    });

    await opportunity.save();

    await logAudit({
      req,
      action: 'Update Opportunity',
      entityName: 'Opportunity',
      recordId: opportunity._id,
      oldValue: oldStage,
      newValue: opportunity.stage,
      details: `Updated opportunity ${opportunity.opportunityCode}`
    });

    res.status(200).json({ success: true, data: opportunity });
  } catch (err) {
    next(err);
  }
};

