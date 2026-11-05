const express = require('express');
const router = express.Router();
const {
  getOpportunities,
  getOpportunityById,
  createOpportunity,
  updateOpportunity
} = require('../controllers/opportunityController');
const { protect } = require('../middleware/auth');

router.use(protect);

router.route('/')
  .get(getOpportunities)
  .post(createOpportunity);

router.route('/:id')
  .get(getOpportunityById)
  .put(updateOpportunity);

module.exports = router;

