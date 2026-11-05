const jwt = require('jsonwebtoken');
const User = require('../models/User');
const { logAudit } = require('../middleware/audit');

const signToken = (id) => {
  return jwt.sign(
    { id },
    process.env.JWT_SECRET || 'AcxiomCRM_Super_Secret_Key_MCA_2026_Secure_Token!',
    { expiresIn: process.env.JWT_EXPIRE || '24h' }
  );
};

// @desc    Login user & get token
// @route   POST /api/auth/login
// @access  Public
exports.login = async (req, res, next) => {
  try {
    const { emailOrUsername, password } = req.body;

    if (!emailOrUsername || !password) {
      return res.status(400).json({
        success: false,
        message: 'Please provide an email/username and password.'
      });
    }

    const user = await User.findOne({
      $or: [
        { email: emailOrUsername.toLowerCase().trim() },
        { email: emailOrUsername.trim() }
      ]
    }).select('+password');

    if (!user || !(await user.comparePassword(password))) {
      await logAudit({
        req,
        action: 'Failed Login',
        entityName: 'Authentication',
        result: 'Failed',
        details: `Invalid login attempt for: ${emailOrUsername}`
      });

      return res.status(401).json({
        success: false,
        message: 'Invalid email or password.'
      });
    }

    if (!user.isActive) {
      await logAudit({
        req,
        action: 'Failed Login - Inactive',
        entityName: 'Authentication',
        result: 'Failed',
        details: `Login attempt on deactivated account: ${user.email}`
      });

      return res.status(403).json({
        success: false,
        message: 'Your account has been deactivated. Please contact an administrator.'
      });
    }

    user.lastLoginDate = new Date();
    await user.save({ validateBeforeSave: false });

    await logAudit({
      req: { ...req, user },
      action: 'User Login',
      entityName: 'Authentication',
      recordId: user._id,
      result: 'Success',
      details: `Successful login as ${user.role}`
    });

    const token = signToken(user._id);

    res.status(200).json({
      success: true,
      token,
      user: {
        id: user._id,
        fullName: user.fullName,
        email: user.email,
        role: user.role,
        managerId: user.managerId
      }
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Register new sales executive
// @route   POST /api/auth/register
// @access  Public
exports.register = async (req, res, next) => {
  try {
    const { fullName, email, password } = req.body;

    if (!fullName || !email || !password) {
      return res.status(400).json({
        success: false,
        message: 'Please provide full name, email, and password.'
      });
    }

    const existingUser = await User.findOne({ email: email.toLowerCase().trim() });
    if (existingUser) {
      return res.status(400).json({
        success: false,
        message: 'An account with this email already exists.'
      });
    }

    // Assign default manager if available
    const manager = await User.findOne({ role: 'Manager', isActive: true });

    const user = await User.create({
      fullName: fullName.trim(),
      email: email.toLowerCase().trim(),
      password,
      role: 'Sales Executive',
      managerId: manager ? manager._id : null,
      isActive: true
    });

    await logAudit({
      req: { ...req, user },
      action: 'User Registered',
      entityName: 'User',
      recordId: user._id,
      result: 'Success',
      details: `New Sales Executive self-registered: ${user.email}`
    });

    const token = signToken(user._id);

    res.status(201).json({
      success: true,
      token,
      user: {
        id: user._id,
        fullName: user.fullName,
        email: user.email,
        role: user.role,
        managerId: user.managerId
      }
    });
  } catch (err) {
    next(err);
  }
};

// @desc    Get current logged in user
// @route   GET /api/auth/me
// @access  Private
exports.getMe = async (req, res, next) => {
  try {
    const user = await User.findById(req.user._id).populate('managerId', 'fullName email');
    res.status(200).json({
      success: true,
      user
    });
  } catch (err) {
    next(err);
  }
};

