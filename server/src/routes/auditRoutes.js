const express = require('express');
const router = express.Router();
const { getAuditLogs } = require('../controllers/auditController');
const { protect } = require('../middleware/auth');
const { authorize } = require('../middleware/rbac');

router.use(protect);
router.use(authorize('Admin'));

router.get('/', getAuditLogs);

module.exports = router;

