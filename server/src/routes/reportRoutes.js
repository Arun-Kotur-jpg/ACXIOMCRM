const express = require('express');
const router = express.Router();
const {
  getDashboardMetrics,
  getAnalytics
} = require('../controllers/reportController');
const { protect } = require('../middleware/auth');

router.use(protect);

router.get('/dashboard', getDashboardMetrics);
router.get('/analytics', getAnalytics);

module.exports = router;

