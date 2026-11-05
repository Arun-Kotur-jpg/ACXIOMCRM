const express = require('express');
const router = express.Router();
const {
  getLeads,
  getLeadById,
  createLead,
  updateLead,
  convertLead
} = require('../controllers/leadController');
const { protect } = require('../middleware/auth');

router.use(protect);

router.route('/')
  .get(getLeads)
  .post(createLead);

router.route('/:id')
  .get(getLeadById)
  .put(updateLead);

router.post('/:id/convert', convertLead);

module.exports = router;

