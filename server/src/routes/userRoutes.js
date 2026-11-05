const express = require('express');
const router = express.Router();
const {
  getUsers,
  createUser,
  updateUser,
  toggleUserStatus
} = require('../controllers/userController');
const { protect } = require('../middleware/auth');
const { authorize } = require('../middleware/rbac');

router.use(protect);
router.use(authorize('Admin'));

router.route('/')
  .get(getUsers)
  .post(createUser);

router.route('/:id')
  .put(updateUser);

router.patch('/:id/toggle-status', toggleUserStatus);

module.exports = router;

