const express = require('express');
const router = express.Router();
const {
  getCustomers,
  getCustomerById,
  createCustomer,
  updateCustomer,
  deleteCustomer
} = require('../controllers/customerController');
const { protect } = require('../middleware/auth');
const { authorize } = require('../middleware/rbac');

router.use(protect);

router.route('/')
  .get(getCustomers)
  .post(createCustomer);

router.route('/:id')
  .get(getCustomerById)
  .put(updateCustomer)
  .delete(authorize('Admin'), deleteCustomer);

module.exports = router;

