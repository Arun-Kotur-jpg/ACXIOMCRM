const Customer = require('../models/Customer');
const { getRecordScopeFilter, canUserAccessRecord } = require('../middleware/recordScope');
const { logAudit } = require('../middleware/audit');

// Helper to generate customer code
async function generateCustomerCode() {
  const count = await Customer.countDocuments();
  const nextNum = (count + 1).toString().padStart(4, '0');
  return `CUST-${nextNum}`;
}

// @desc    Get all customers (scoped by role)
// @route   GET /api/customers
// @access  Private
exports.getCustomers = async (req, res, next) => {
  try {
    const { search, status, page = 1, limit = 10, sort = '-createdAt' } = req.query;

    const scopeFilter = await getRecordScopeFilter(req.user, 'ownerId');
    const query = { ...scopeFilter };

    if (status) {
      query.status = status;
    }

    if (search) {
      query.$or = [
        { customerName: { $regex: search, $options: 'i' } },
        { customerCode: { $regex: search, $options: 'i' } },
        { companyName: { $regex: search, $options: 'i' } },
        { email: { $regex: search, $options: 'i' } },
        { city: { $regex: search, $options: 'i' } }
      ];
    }

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await Customer.countDocuments(query);
    const customers = await Customer.find(query)
      .populate('ownerId', 'fullName email role')
      .sort(sort)
      .skip(skip)
      .limit(limitNum);

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: customers.length,
      data: customers
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Get single customer by ID
// @route   GET /api/customers/:id
// @access  Private
exports.getCustomerById = async (req, res, next) => {
  try {
    const customer = await Customer.findById(req.params.id).populate('ownerId', 'fullName email role');

    if (!customer) {
      return res.status(404).json({ success: false, message: 'Customer not found.' });
    }

    // Role-based IDOR check
    const allowed = await canUserAccessRecord(req.user, customer.ownerId?._id || customer.ownerId);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to view this customer record.'
      });
    }

    res.status(200).json({ success: true, data: customer });
  } catch (err) {
    next(err);
  }
};

// @desc    Create new customer
// @route   POST /api/customers
// @access  Private
exports.createCustomer = async (req, res, next) => {
  try {
    const { customerName, email, phone, companyName, address, city, state, status, ownerId } = req.body;

    const customerCode = await generateCustomerCode();
    // Default owner to self if not provided or if sales rep
    const assignedOwner = (req.user.role === 'Admin' || req.user.role === 'Manager') && ownerId
      ? ownerId
      : req.user._id;

    const customer = await Customer.create({
      customerCode,
      customerName,
      email,
      phone,
      companyName,
      address,
      city,
      state,
      status: status || 'Active',
      ownerId: assignedOwner,
      createdBy: req.user.fullName
    });

    await logAudit({
      req,
      action: 'Create Customer',
      entityName: 'Customer',
      recordId: customer._id,
      newValue: customer.customerName,
      details: `Created customer ${customer.customerCode}: ${customer.customerName}`
    });

    res.status(201).json({ success: true, data: customer });
  } catch (err) {
    next(err);
  }
};

// @desc    Update customer
// @route   PUT /api/customers/:id
// @access  Private
exports.updateCustomer = async (req, res, next) => {
  try {
    let customer = await Customer.findById(req.params.id);
    if (!customer) {
      return res.status(404).json({ success: false, message: 'Customer not found.' });
    }

    const allowed = await canUserAccessRecord(req.user, customer.ownerId);
    if (!allowed) {
      return res.status(403).json({
        success: false,
        message: 'Access denied: You do not have permission to update this customer record.'
      });
    }

    const oldValue = customer.toObject();
    const updatableFields = ['customerName', 'email', 'phone', 'companyName', 'address', 'city', 'state', 'status'];

    if (req.user.role === 'Admin' || req.user.role === 'Manager') {
      updatableFields.push('ownerId');
    }

    updatableFields.forEach(field => {
      if (req.body[field] !== undefined) {
        customer[field] = req.body[field];
      }
    });

    await customer.save();

    await logAudit({
      req,
      action: 'Update Customer',
      entityName: 'Customer',
      recordId: customer._id,
      oldValue: oldValue.customerName,
      newValue: customer.customerName,
      details: `Updated customer ${customer.customerCode}`
    });

    res.status(200).json({ success: true, data: customer });
  } catch (err) {
    next(err);
  }
};

// @desc    Delete customer
// @route   DELETE /api/customers/:id
// @access  Private (Admin only or manager)
exports.deleteCustomer = async (req, res, next) => {
  try {
    const customer = await Customer.findById(req.params.id);
    if (!customer) {
      return res.status(404).json({ success: false, message: 'Customer not found.' });
    }

    if (req.user.role !== 'Admin') {
      return res.status(403).json({
        success: false,
        message: 'Forbidden: Only administrators can permanently delete customer records.'
      });
    }

    await customer.deleteOne();

    await logAudit({
      req,
      action: 'Delete Customer',
      entityName: 'Customer',
      recordId: customer._id,
      oldValue: customer.customerName,
      details: `Deleted customer ${customer.customerCode}`
    });

    res.status(200).json({ success: true, message: 'Customer deleted successfully.' });
  } catch (err) {
    next(err);
  }
};

