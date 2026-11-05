const User = require('../models/User');
const { logAudit } = require('../middleware/audit');

// @desc    Get all users
// @route   GET /api/users
// @access  Private (Admin only)
exports.getUsers = async (req, res, next) => {
  try {
    const { role, search, page = 1, limit = 10 } = req.query;
    const query = {};

    if (role) query.role = role;
    if (search) {
      query.$or = [
        { fullName: { $regex: search, $options: 'i' } },
        { email: { $regex: search, $options: 'i' } }
      ];
    }

    const pageNum = parseInt(page, 10);
    const limitNum = parseInt(limit, 10);
    const skip = (pageNum - 1) * limitNum;

    const total = await User.countDocuments(query);
    const users = await User.find(query)
      .populate('managerId', 'fullName email')
      .sort('-createdAt')
      .skip(skip)
      .limit(limitNum);

    // List of managers for assignment dropdown
    const managers = await User.find({ role: 'Manager', isActive: true }).select('fullName email');

    res.status(200).json({
      success: true,
      total,
      page: pageNum,
      pages: Math.ceil(total / limitNum) || 1,
      count: users.length,
      managers,
      data: users
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Create a user
// @route   POST /api/users
// @access  Private (Admin only)
exports.createUser = async (req, res, next) => {
  try {
    const { fullName, email, password, role, managerId, isActive = true } = req.body;

    const existingUser = await User.findOne({ email: email.toLowerCase().trim() });
    if (existingUser) {
      return res.status(400).json({ success: false, message: 'User with this email already exists.' });
    }

    const user = await User.create({
      fullName,
      email: email.toLowerCase().trim(),
      password,
      role: role || 'Sales Executive',
      managerId: managerId || null,
      isActive
    });

    await logAudit({
      req,
      action: 'Create User',
      entityName: 'User',
      recordId: user._id,
      newValue: `${user.email} (${user.role})`,
      details: `Admin created user ${user.email} with role ${user.role}`
    });

    res.status(201).json({ success: true, data: user });
  } catch (err) {
    next(err);
  }
};

// @desc    Update user
// @route   PUT /api/users/:id
// @access  Private (Admin only)
exports.updateUser = async (req, res, next) => {
  try {
    const { fullName, role, managerId, isActive, password } = req.body;

    const user = await User.findById(req.params.id);
    if (!user) {
      return res.status(404).json({ success: false, message: 'User not found.' });
    }

    if (fullName) user.fullName = fullName;
    if (role) user.role = role;
    if (managerId !== undefined) user.managerId = managerId || null;
    if (isActive !== undefined) user.isActive = isActive;
    if (password) user.password = password;

    await user.save();

    await logAudit({
      req,
      action: 'Update User',
      entityName: 'User',
      recordId: user._id,
      newValue: `${user.email} (${user.role})`,
      details: `Admin updated user ${user.email}`
    });

    res.status(200).json({ success: true, data: user });
  } catch (err) {
    next(err);
  }
};

// @desc    Toggle user status (activate/deactivate)
// @route   PATCH /api/users/:id/toggle-status
// @access  Private (Admin only)
exports.toggleUserStatus = async (req, res, next) => {
  try {
    const user = await User.findById(req.params.id);
    if (!user) {
      return res.status(404).json({ success: false, message: 'User not found.' });
    }

    // Protect self-deactivation
    if (user._id.toString() === req.user._id.toString()) {
      return res.status(400).json({ success: false, message: 'You cannot deactivate your own account.' });
    }

    user.isActive = !user.isActive;
    await user.save();

    await logAudit({
      req,
      action: user.isActive ? 'Activate User' : 'Deactivate User',
      entityName: 'User',
      recordId: user._id,
      newValue: user.isActive ? 'Active' : 'Inactive',
      details: `${user.isActive ? 'Activated' : 'Deactivated'} account for ${user.email}`
    });

    res.status(200).json({
      success: true,
      message: `User ${user.isActive ? 'activated' : 'deactivated'} successfully.`,
      data: user
    });
  } catch (err) {
    next(err);
  }
};

