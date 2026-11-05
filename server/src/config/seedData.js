const bcrypt = require('bcryptjs');
const User = require('../models/User');
const Customer = require('../models/Customer');
const Lead = require('../models/Lead');
const Opportunity = require('../models/Opportunity');
const FollowUp = require('../models/FollowUp');
const Activity = require('../models/Activity');
const AuditLog = require('../models/AuditLog');

async function seedDatabase() {
  try {
    const adminCount = await User.countDocuments();
    if (adminCount > 0) {
      // Ensure passwords and accounts are in sync
      const admin = await User.findOne({ email: 'admin@acxiomcrm.local' });
      if (admin) {
        admin.password = 'Admin@123456!';
        admin.isActive = true;
        await admin.save();
      }
      return;
    }

    console.log('🌱 Seeding AcxiomCRM demonstration data...');

    // 1. Seed Users
    const admin = await User.create({
      fullName: 'System Administrator',
      email: 'admin@acxiomcrm.local',
      password: 'Admin@123456!',
      role: 'Admin',
      isActive: true
    });

    const manager = await User.create({
      fullName: 'Rajesh Sharma (Sales Manager)',
      email: 'manager@acxiomcrm.local',
      password: 'Demo@123456!',
      role: 'Manager',
      isActive: true
    });

    const sales1 = await User.create({
      fullName: 'Vikram Malhotra',
      email: 'sales1@acxiomcrm.local',
      password: 'Demo@123456!',
      role: 'Sales Executive',
      managerId: manager._id,
      isActive: true
    });

    const sales2 = await User.create({
      fullName: 'Priya Nair',
      email: 'sales2@acxiomcrm.local',
      password: 'Demo@123456!',
      role: 'Sales Executive',
      managerId: manager._id,
      isActive: true
    });

    // 2. Seed Customers
    const cust1 = await Customer.create({
      customerCode: 'CUST-0001',
      customerName: 'Tata Consultancy Enterprise',
      email: 'procurement@tataenterprise.com',
      phone: '9820012345',
      companyName: 'Tata Enterprises Ltd',
      address: 'Nariman Point, Marine Drive',
      city: 'Mumbai',
      state: 'Maharashtra',
      status: 'Active',
      ownerId: sales1._id,
      createdBy: 'System'
    });

    const cust2 = await Customer.create({
      customerCode: 'CUST-0002',
      customerName: 'Infosys Digital Solutions',
      email: 'connect@infosysdigital.com',
      phone: '9845012346',
      companyName: 'Infosys Tech Hub',
      address: 'Electronic City Phase 1',
      city: 'Bengaluru',
      state: 'Karnataka',
      status: 'Active',
      ownerId: sales2._id,
      createdBy: 'System'
    });

    const cust3 = await Customer.create({
      customerCode: 'CUST-0003',
      customerName: 'Reliance Logistics Corp',
      email: 'ops@reliancelogistics.com',
      phone: '9811012347',
      companyName: 'Reliance Logistics Ltd',
      address: 'BKC Complex',
      city: 'Mumbai',
      state: 'Maharashtra',
      status: 'Active',
      ownerId: sales1._id,
      createdBy: 'System'
    });

    const cust4 = await Customer.create({
      customerCode: 'CUST-0004',
      customerName: 'Wipro Cloud Infrastructure',
      email: 'it@wiprocloud.com',
      phone: '9880012348',
      companyName: 'Wipro Solutions',
      address: 'Sarjapur Road',
      city: 'Bengaluru',
      state: 'Karnataka',
      status: 'Active',
      ownerId: manager._id,
      createdBy: 'System'
    });

    // 3. Seed Leads
    const lead1 = await Lead.create({
      leadCode: 'LEAD-0001',
      leadName: 'Adani Ports Automation',
      email: 'contact@adaniports.com',
      phone: '9879012349',
      companyName: 'Adani Port Services',
      source: 'Referral',
      status: 'Qualified',
      priority: 'High',
      expectedValue: 1200000,
      notes: 'Interested in warehouse tracking and automated quotes.',
      assignedTo: sales1._id,
      createdBy: 'System'
    });

    const lead2 = await Lead.create({
      leadCode: 'LEAD-0002',
      leadName: 'HDFC FinTech Modernization',
      email: 'leads@hdfcfintech.com',
      phone: '9822012350',
      companyName: 'HDFC Banking Solutions',
      source: 'Website',
      status: 'Contacted',
      priority: 'Medium',
      expectedValue: 850000,
      notes: 'Demo scheduled for core sales automation.',
      assignedTo: sales2._id,
      createdBy: 'System'
    });

    const lead3 = await Lead.create({
      leadCode: 'LEAD-0003',
      leadName: 'Mahindra Auto Dealers CRM',
      email: 'sales@mahindradealers.com',
      phone: '9833012351',
      companyName: 'Mahindra Auto Network',
      source: 'ColdCall',
      status: 'New',
      priority: 'Low',
      expectedValue: 450000,
      notes: 'Requested informational brochure.',
      assignedTo: sales1._id,
      createdBy: 'System'
    });

    const lead4 = await Lead.create({
      leadCode: 'LEAD-0004',
      leadName: 'L&T Defense Procurement',
      email: 'contracts@ltdefense.com',
      phone: '9844012352',
      companyName: 'L&T Heavy Engineering',
      source: 'TradeShow',
      status: 'Converted',
      priority: 'Urgent',
      expectedValue: 2500000,
      notes: 'Lead successfully converted to customer Tata Consultancy.',
      assignedTo: sales1._id,
      convertedToCustomerId: cust1._id,
      convertedDate: new Date(),
      createdBy: 'System'
    });

    // 4. Seed Opportunities
    const opp1 = await Opportunity.create({
      opportunityCode: 'OPP-0001',
      title: 'Tata Enterprise Cloud Migration Phase 1',
      customerId: cust1._id,
      leadId: lead4._id,
      amount: 2500000,
      stage: 'Proposal',
      probability: 60,
      closeDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      assignedTo: sales1._id,
      notes: 'Technical proposal delivered and awaiting procurement review.',
      createdBy: 'System'
    });

    const opp2 = await Opportunity.create({
      opportunityCode: 'OPP-0002',
      title: 'Infosys AI Analytics Licensing',
      customerId: cust2._id,
      amount: 1800000,
      stage: 'Negotiation',
      probability: 80,
      closeDate: new Date(Date.now() + 15 * 24 * 60 * 60 * 1000),
      assignedTo: sales2._id,
      notes: 'Contract terms in legal review.',
      createdBy: 'System'
    });

    const opp3 = await Opportunity.create({
      opportunityCode: 'OPP-0003',
      title: 'Reliance Fleet Tracking Integration',
      customerId: cust3._id,
      amount: 3200000,
      stage: 'ClosedWon',
      probability: 100,
      closeDate: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000),
      assignedTo: sales1._id,
      notes: 'Deal closed and kickoff meeting planned.',
      createdBy: 'System'
    });

    const opp4 = await Opportunity.create({
      opportunityCode: 'OPP-0004',
      title: 'Wipro Hybrid Cloud Integration',
      customerId: cust4._id,
      amount: 1200000,
      stage: 'ClosedLost',
      probability: 0,
      closeDate: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000),
      assignedTo: manager._id,
      lostReason: 'Budget postponed to next fiscal year.',
      createdBy: 'System'
    });

    // 5. Seed Follow-ups
    await FollowUp.create({
      subject: 'Review Cloud Migration Security Proposal',
      scheduledDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000),
      customerId: cust1._id,
      opportunityId: opp1._id,
      assignedTo: sales1._id,
      priority: 'High',
      status: 'Planned',
      createdBy: 'System'
    });

    await FollowUp.create({
      subject: 'Urgent: Follow up on legal contract draft',
      scheduledDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // Overdue!
      customerId: cust2._id,
      opportunityId: opp2._id,
      assignedTo: sales2._id,
      priority: 'Urgent',
      status: 'Planned',
      createdBy: 'System'
    });

    await FollowUp.create({
      subject: 'Initial discovery call with Adani procurement',
      scheduledDate: new Date(Date.now() + 1 * 24 * 60 * 60 * 1000),
      leadId: lead1._id,
      assignedTo: sales1._id,
      priority: 'Medium',
      status: 'Planned',
      createdBy: 'System'
    });

    // 6. Seed Activities
    await Activity.create({
      type: 'Meeting',
      subject: 'Discovery & Requirements Discussion',
      activityDate: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000),
      customerId: cust1._id,
      opportunityId: opp1._id,
      performedBy: sales1._id,
      durationMinutes: 45,
      notes: 'Met with IT director to finalize system architecture.',
      createdBy: 'System'
    });

    await Activity.create({
      type: 'Call',
      subject: 'Pricing & Discount Negotiation Call',
      activityDate: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
      customerId: cust2._id,
      opportunityId: opp2._id,
      performedBy: sales2._id,
      durationMinutes: 30,
      notes: 'Agreed on 8% multi-year discount.',
      createdBy: 'System'
    });

    // 7. Seed Audit Logs
    await AuditLog.create({
      userId: admin._id,
      userName: admin.fullName,
      action: 'System Initialized',
      entityName: 'System',
      recordId: '0',
      result: 'Success',
      ipAddress: '127.0.0.1',
      details: 'AcxiomCRM initial setup and MongoDB database seeding completed.'
    });

    await AuditLog.create({
      userId: sales1._id,
      userName: sales1.fullName,
      action: 'Lead Converted',
      entityName: 'Lead',
      recordId: lead4._id.toString(),
      oldValue: 'Status: Qualified',
      newValue: `CustomerId: ${cust1._id}`,
      result: 'Success',
      ipAddress: '127.0.0.1',
      details: 'Lead converted to Customer Tata Consultancy Enterprise.'
    });

    console.log('✅ AcxiomCRM demonstration data seeded successfully.');
  } catch (error) {
    console.error('❌ Error seeding database:', error);
  }
}

module.exports = { seedDatabase };

